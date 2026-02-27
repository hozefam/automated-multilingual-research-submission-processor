#pragma warning disable SKEXP0080   // SK Process Framework is experimental
#pragma warning disable SKEXP0070   // SK Agent invocation is experimental

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Process;

namespace Backend.Services.MetadataExtraction;

// ── Process event IDs ─────────────────────────────────────────────────────────

internal static class MetadataProcessEvents
{
    /// <summary>External trigger: start the validation process.</summary>
    internal const string StartValidation = nameof(StartValidation);

    /// <summary>Agent has validated/refined the metadata JSON.</summary>
    internal const string ValidationDone = nameof(ValidationDone);

    /// <summary>Process has finished all steps.</summary>
    internal const string ProcessComplete = nameof(ProcessComplete);
}

// ── Data types passed between steps ──────────────────────────────────────────

/// <summary>Initial input sent when the process is triggered.</summary>
public sealed record AgentStepInput(
    string CorrelationId,
    string InitialMetadataJson,
    int PageCount,
    Kernel Kernel);

/// <summary>Output from the agent step, forwarded to the finalize step.</summary>
public sealed record FinalizeStepInput(
    string CorrelationId,
    string RefinedMetadataJson);

// ── ProcessResultStore – thread-safe TCS registry ─────────────────────────────
/// <summary>
/// Bridges the asynchronous SK Process world back to the awaiting
/// <see cref="MetadataExtractor.ExtractAsync"/> call via a TaskCompletionSource.
/// </summary>
internal static class ProcessResultStore
{
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new();

    internal static TaskCompletionSource<string> Register(string correlationId)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[correlationId] = tcs;
        return tcs;
    }

    internal static void Complete(string correlationId, string metadataJson)
    {
        if (_pending.TryRemove(correlationId, out var tcs))
            tcs.TrySetResult(metadataJson);
    }

    internal static void Fail(string correlationId, string error)
    {
        if (_pending.TryRemove(correlationId, out var tcs))
            tcs.TrySetException(new InvalidOperationException(error));
    }
}

// ── Step 1: AgentValidationStep ───────────────────────────────────────────────
/// <summary>
/// SK Process step that runs a <see cref="ChatCompletionAgent"/> (Agent Framework)
/// to validate and refine the initial metadata JSON produced by the Semantic Function.
///
/// ┌─────────────────────────────────────────────────────────────────────────┐
/// │  SK Concepts demonstrated inside this step:                            │
/// │  • Agent Framework – ChatCompletionAgent with FunctionChoiceBehavior   │
/// │  • Native Function – validate_metadata_json called by the agent as tool│
/// │  • Process Framework – this class IS a KernelProcessStep               │
/// └─────────────────────────────────────────────────────────────────────────┘
/// </summary>
[Experimental("SKEXP0080")]
public sealed class AgentValidationStep : KernelProcessStep
{
    [KernelFunction]
    public async Task RunAsync(KernelProcessStepContext context, AgentStepInput input)
    {
        // ── ChatCompletionAgent (Agent Framework) ─────────────────────────────
        // The agent has the MetadataExtraction plugin available as tools.
        // It calls validate_metadata_json (Native Function) autonomously to
        // verify its own output and self-correct if needed.
        var agentKernel = input.Kernel.Clone();

        var agent = new ChatCompletionAgent
        {
            Name = "MetadataValidationAgent",
            Instructions = $$"""
                You are a research paper metadata validator and corrector.
                
                You will receive a JSON object with extracted metadata from a research paper.
                
                Your tasks in order:
                1. Review the metadata for correctness and completeness.
                2. ALWAYS call the validate_metadata_json tool on the JSON – fix any reported issues.
                3. Ensure pageCount is exactly {{input.PageCount}}.
                4. Return ONLY the final validated JSON. No markdown fences, no explanation.
                
                Required JSON schema:
                {
                  "title":    "string",
                  "authors":  ["string"],
                  "abstract": "string (full abstract text)",
                  "keywords": ["string"],
                  "pageCount": {{input.PageCount}},
                  "format":   "PDF"
                }
                """,
            Kernel = agentKernel,
            Arguments = new KernelArguments(
                new AzureOpenAIPromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };

        // Collect all streamed response parts
        var responseParts = new List<string>();
        var userMessages = new List<ChatMessageContent>
        {
            new(AuthorRole.User, $"Validate and correct this metadata JSON:\n{input.InitialMetadataJson}")
        };
        await foreach (var response in agent.InvokeAsync(userMessages))
        {
            if (!string.IsNullOrEmpty(response.Message.Content))
                responseParts.Add(response.Message.Content);
        }

        var refinedJson = responseParts.Count > 0
            ? MetadataExtractionPlugin.StripMarkdownFences(string.Join("", responseParts))
            : input.InitialMetadataJson; // fallback to initial if agent returned nothing

        await context.EmitEventAsync(new KernelProcessEvent
        {
            Id = MetadataProcessEvents.ValidationDone,
            Data = new FinalizeStepInput(input.CorrelationId, refinedJson)
        });
    }
}

// ── Step 2: FinalizeStep ─────────────────────────────────────────────────────
/// <summary>
/// Final process step – delivers the refined metadata JSON back to the
/// awaiting <see cref="MetadataExtractor"/> via <see cref="ProcessResultStore"/>.
/// </summary>
[Experimental("SKEXP0080")]
public sealed class FinalizeStep : KernelProcessStep
{
    [KernelFunction]
    public async Task RunAsync(KernelProcessStepContext context, FinalizeStepInput input)
    {
        ProcessResultStore.Complete(input.CorrelationId, input.RefinedMetadataJson);

        await context.EmitEventAsync(new KernelProcessEvent
        {
            Id = MetadataProcessEvents.ProcessComplete
        });
    }
}

// ── Process Builder ───────────────────────────────────────────────────────────
/// <summary>
/// Builds the <c>MetadataExtractionProcess</c> KernelProcess definition.
///
/// Execution graph:
///   [StartValidation] → AgentValidationStep
///                                ↓ ValidationDone
///                         FinalizeStep
///                                ↓ ProcessComplete  (end)
/// </summary>
[Experimental("SKEXP0080")]
public static class MetadataProcessBuilder
{
    public static KernelProcess Build()
    {
        var builder = new ProcessBuilder("MetadataExtractionProcess");

        var agentStep = builder.AddStepFromType<AgentValidationStep>("AgentValidation");
        var finalizeStep = builder.AddStepFromType<FinalizeStep>("Finalize");

        // Wire: external start → agent validation
        builder
            .OnInputEvent(MetadataProcessEvents.StartValidation)
            .SendEventTo(new ProcessFunctionTargetBuilder(agentStep));

        // Wire: agent done → finalize
        agentStep
            .OnEvent(MetadataProcessEvents.ValidationDone)
            .SendEventTo(new ProcessFunctionTargetBuilder(finalizeStep));

        return builder.Build();
    }
}

#pragma warning restore SKEXP0080
#pragma warning restore SKEXP0070
