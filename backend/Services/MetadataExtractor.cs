#pragma warning disable SKEXP0080   // SK Process Framework is experimental
#pragma warning disable SKEXP0001   // SK Memory APIs are experimental

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Backend.Models;
using Backend.Services.MetadataExtraction;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Process;

namespace Backend.Services;

/// <summary>
/// Metadata extraction step of the AMRSP document pipeline.
///
/// Demonstrates all seven Semantic Kernel capabilities:
///
///  ① Native Functions   – MetadataExtractionPlugin.extract_document_info
///                          (calls Azure Document Intelligence AI)
///  ② Semantic Functions – MetadataPrompts.ExtractMetadataFromText
///                          (prompt template → GPT-4o)
///  ③ Plugins            – "MetadataExtraction" + "MetadataPrompts" plugins
///                          registered on the Kernel
///  ④ Filters / Logging  – MetadataLoggingFilter on every function invocation
///  ⑤ Memory             – VolatileMemoryStore: skip re-extraction for seen docs
///  ⑥ Agent Framework    – ChatCompletionAgent in AgentValidationStep
///                          (validates + self-corrects with tool calling)
///  ⑦ Process Framework  – KernelProcess drives AgentValidation → Finalize
/// </summary>
public class MetadataExtractor : IMetadataExtractor
{
    private readonly Kernel _kernel;
    private readonly IMemoryStore _memoryStore;
    private readonly ILogger<MetadataExtractor> _logger;

    private const string CacheCollection = "metadata-cache";

    public MetadataExtractor(
        Kernel kernel,
        IMemoryStore memoryStore,
        ILogger<MetadataExtractor> logger)
    {
        _kernel = kernel;
        _memoryStore = memoryStore;
        _logger = logger;
    }

    public async Task<StepResult<DocumentMetadata>> ExtractAsync(
        Stream pdfStream,
        string fileName,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[MetadataExtractor] Starting SK-powered extraction for '{FileName}'", fileName);

        // ── Read PDF bytes ────────────────────────────────────────────────────
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, ct);
        var pdfBytes = ms.ToArray();
        var base64Pdf = Convert.ToBase64String(pdfBytes);
        var cacheKey = ComputeSha256Key(pdfBytes);

        // ── ⑤ MEMORY – Check VolatileMemoryStore cache ────────────────────────
        var cached = await TryGetFromCacheAsync(cacheKey, ct);
        if (cached is not null)
        {
            sw.Stop();
            _logger.LogInformation(
                "[MetadataExtractor] ⑤ SK Memory cache HIT for '{FileName}' ({Ms}ms)", fileName, sw.ElapsedMilliseconds);
            return new StepResult<DocumentMetadata>(true, cached, ElapsedMs: sw.ElapsedMilliseconds);
        }

        _logger.LogInformation("[MetadataExtractor] ⑤ SK Memory cache MISS – running full AI pipeline");

        // ── ① NATIVE FUNCTION – extract_document_info ─────────────────────────
        // Plugin: "MetadataExtraction" | Function: "extract_document_info"
        // Calls Azure Document Intelligence to convert PDF binary → text + pageCount.
        // The MetadataLoggingFilter (④) intercepts this invocation automatically.
        _logger.LogInformation("[MetadataExtractor] ①③ Invoking Native Function via Plugin: MetadataExtraction.extract_document_info");
        var docInfoResult = await _kernel.InvokeAsync(
            pluginName: "MetadataExtraction",
            functionName: "extract_document_info",
            arguments: new KernelArguments { ["base64PdfBytes"] = base64Pdf },
            cancellationToken: ct);

        var docInfoJson = docInfoResult.GetValue<string>()!;
        using var docDoc = JsonDocument.Parse(docInfoJson);
        var documentText = docDoc.RootElement.GetProperty("text").GetString() ?? string.Empty;
        var pageCount = docDoc.RootElement.GetProperty("pageCount").GetInt32();

        // Limit prompt input to 4 000 chars (LLM context management)
        var promptText = documentText.Length > 4_000 ? documentText[..4_000] : documentText;

        // ── ② SEMANTIC FUNCTION – ExtractMetadataFromText ─────────────────────
        // Plugin: "MetadataPrompts" | Function: "ExtractMetadataFromText"
        // Sends document text to GPT-4o via a prompt template; returns initial JSON.
        // The MetadataLoggingFilter (④) intercepts both the function invocation
        // and the prompt render.
        _logger.LogInformation("[MetadataExtractor] ②③ Invoking Semantic Function via Plugin: MetadataPrompts.ExtractMetadataFromText");
        var semanticResult = await _kernel.InvokeAsync(
            pluginName: "MetadataPrompts",
            functionName: "ExtractMetadataFromText",
            arguments: new KernelArguments { ["documentText"] = promptText },
            cancellationToken: ct);

        var initialJson = MetadataExtractionPlugin.StripMarkdownFences(
            semanticResult.GetValue<string>() ?? "{}");

        _logger.LogInformation("[MetadataExtractor] ② Semantic Function produced initial metadata JSON");

        // ── ⑦ PROCESS FRAMEWORK + ⑥ AGENT FRAMEWORK ─────────────────────────
        // KernelProcess with 2 steps:
        //   AgentValidationStep (⑥) – ChatCompletionAgent calls validate_metadata_json
        //                              as a tool (① native fn) to self-correct output
        //   FinalizeStep         (⑦) – stores result in ProcessResultStore TCS
        _logger.LogInformation("[MetadataExtractor] ⑦⑥ Starting Process Framework (Agent self-correction)");

        var correlationId = Guid.NewGuid().ToString("N");
        var tcs = ProcessResultStore.Register(correlationId);

        var process = MetadataProcessBuilder.Build();
        await process.StartAsync(
            _kernel,
            new KernelProcessEvent
            {
                Id = MetadataProcessEvents.StartValidation,
                Data = new AgentStepInput(correlationId, initialJson, pageCount, _kernel)
            });

        // Await result delivered by FinalizeStep via ProcessResultStore
        var finalJson = await tcs.Task.WaitAsync(TimeSpan.FromMinutes(2), ct);

        _logger.LogInformation("[MetadataExtractor] ⑦ Process Framework complete – deserializing result");

        // ── Deserialize final metadata ──────────────────────────────────────
        var metadata = DeserializeMetadata(finalJson, fileName, pageCount);

        // ── ⑤ MEMORY – Save to VolatileMemoryStore cache ──────────────────────
        await SaveToCacheAsync(cacheKey, finalJson, fileName, ct);

        sw.Stop();
        _logger.LogInformation(
            "[MetadataExtractor] Extraction complete for '{FileName}' in {Ms}ms", fileName, sw.ElapsedMilliseconds);

        return new StepResult<DocumentMetadata>(true, metadata, ElapsedMs: sw.ElapsedMilliseconds);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string ComputeSha256Key(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private async Task<DocumentMetadata?> TryGetFromCacheAsync(string key, CancellationToken ct)
    {
        try
        {
            if (!await _memoryStore.DoesCollectionExistAsync(CacheCollection, ct))
                await _memoryStore.CreateCollectionAsync(CacheCollection, ct);

            var record = await _memoryStore.GetAsync(CacheCollection, key, withEmbedding: false, ct);
            if (record is null) return null;

            return JsonSerializer.Deserialize<DocumentMetadata>(
                record.Metadata.Text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[MetadataExtractor] Memory read skipped: {Error}", ex.Message);
            return null;
        }
    }

    private async Task SaveToCacheAsync(string key, string metadataJson, string fileName, CancellationToken ct)
    {
        try
        {
            if (!await _memoryStore.DoesCollectionExistAsync(CacheCollection, ct))
                await _memoryStore.CreateCollectionAsync(CacheCollection, ct);

            // Empty embedding – we use exact-key lookup (GetAsync), not semantic search
            var record = MemoryRecord.LocalRecord(
                id: key,
                text: metadataJson,
                description: $"Metadata for '{fileName}'",
                additionalMetadata: fileName,
                embedding: null);

            await _memoryStore.UpsertAsync(CacheCollection, record, ct);
            _logger.LogInformation("[MetadataExtractor] ⑤ Result saved to SK Memory cache");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[MetadataExtractor] Memory write skipped: {Error}", ex.Message);
        }
    }

    private static DocumentMetadata DeserializeMetadata(string json, string fileName, int actualPageCount)
    {
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var m = JsonSerializer.Deserialize<DocumentMetadata>(json, opts);
            if (m is not null)
                return m with { PageCount = actualPageCount }; // use deterministic page count
        }
        catch { /* fall through */ }

        // Fallback if JSON could not be parsed
        return new DocumentMetadata(
            Title: Path.GetFileNameWithoutExtension(fileName),
            Authors: [],
            Abstract: "Extraction completed but result could not be parsed.",
            Keywords: [],
            PageCount: actualPageCount,
            Format: "PDF");
    }
}

#pragma warning restore SKEXP0080
#pragma warning restore SKEXP0001
