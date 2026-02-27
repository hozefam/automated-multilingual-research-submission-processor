using System.Diagnostics;
using Microsoft.SemanticKernel;

namespace Backend.Services.MetadataExtraction;

/// <summary>
/// SK Filter – intercepts every function invocation and prompt render that passes
/// through the Kernel used by the metadata pipeline.
///
/// ┌───────────────────────────────────────────────────────────────────┐
/// │  SK Concepts demonstrated:                                        │
/// │  • IFunctionInvocationFilter – wraps every Native + Semantic call │
/// │  • IPromptRenderFilter       – logs the rendered prompt text      │
/// └───────────────────────────────────────────────────────────────────┘
/// </summary>
public sealed class MetadataLoggingFilter : IFunctionInvocationFilter, IPromptRenderFilter
{
    private readonly ILogger<MetadataLoggingFilter> _logger;

    public MetadataLoggingFilter(ILogger<MetadataLoggingFilter> logger) => _logger = logger;

    // ── IFunctionInvocationFilter ─────────────────────────────────────────────
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[SK Filter] → Invoking {Plugin}.{Function}",
            context.Function.PluginName ?? "(kernel)",
            context.Function.Name);

        try
        {
            await next(context);
            sw.Stop();

            _logger.LogInformation(
                "[SK Filter] ✓ Completed {Plugin}.{Function} in {Ms}ms",
                context.Function.PluginName ?? "(kernel)",
                context.Function.Name,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                "[SK Filter] ✗ Failed {Plugin}.{Function} in {Ms}ms – {Error}",
                context.Function.PluginName ?? "(kernel)",
                context.Function.Name,
                sw.ElapsedMilliseconds,
                ex.Message);
            throw;
        }
    }

    // ── IPromptRenderFilter ───────────────────────────────────────────────────
    public async Task OnPromptRenderAsync(
        PromptRenderContext context,
        Func<PromptRenderContext, Task> next)
    {
        _logger.LogInformation(
            "[SK Filter] Rendering prompt for {Plugin}.{Function}",
            context.Function.PluginName ?? "(kernel)",
            context.Function.Name);

        await next(context);

        // Log a safe preview of the rendered prompt (first 200 chars)
        var preview = context.RenderedPrompt ?? string.Empty;
        if (preview.Length > 200) preview = preview[..200] + "…";

        _logger.LogDebug("[SK Filter] Rendered prompt preview: {Preview}", preview);
    }
}
