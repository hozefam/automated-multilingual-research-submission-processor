using System.Diagnostics;
using System.Text;
using Backend.Models;
using Backend.Services;

namespace Backend.Pipeline;

/// <summary>
/// Orchestrates the full 7-step document processing pipeline.
/// Each step receives the output of previous steps where relevant.
/// The pipeline is fail-forward: a failing step is recorded but processing continues.
/// </summary>
public class DocumentPipelineOrchestrator
{
    private readonly IMetadataExtractor _metadataExtractor;
    private readonly ILanguageDetector _languageDetector;
    private readonly IContentSafetyChecker _contentSafetyChecker;
    private readonly IPlagiarismDetector _plagiarismDetector;
    private readonly IRagIndexer _ragIndexer;
    private readonly IAiSummarizer _aiSummarizer;
    private readonly IQnAService _qnAService;
    private readonly ILogger<DocumentPipelineOrchestrator> _logger;

    public DocumentPipelineOrchestrator(
        IMetadataExtractor metadataExtractor,
        ILanguageDetector languageDetector,
        IContentSafetyChecker contentSafetyChecker,
        IPlagiarismDetector plagiarismDetector,
        IRagIndexer ragIndexer,
        IAiSummarizer aiSummarizer,
        IQnAService qnAService,
        ILogger<DocumentPipelineOrchestrator> logger)
    {
        _metadataExtractor = metadataExtractor;
        _languageDetector = languageDetector;
        _contentSafetyChecker = contentSafetyChecker;
        _plagiarismDetector = plagiarismDetector;
        _ragIndexer = ragIndexer;
        _aiSummarizer = aiSummarizer;
        _qnAService = qnAService;
        _logger = logger;
    }

    public async Task<PipelineResult> RunAsync(
        string documentId,
        string fileName,
        Stream pdfStream,
        CancellationToken ct = default)
    {
        var totalSw = Stopwatch.StartNew();
        _logger.LogInformation("=== Pipeline START for document '{DocumentId}' ===", documentId);

        // ── Step 1: Metadata Extraction ────────────────────────────────
        var metadataResult = await SafeRunAsync(
            "Metadata Extraction",
            () => _metadataExtractor.ExtractAsync(pdfStream, fileName, ct));

        // Build text corpus for subsequent steps (stub: use filename + abstract)
        var textCorpus = metadataResult.Data is not null
            ? $"{metadataResult.Data.Title} {metadataResult.Data.Abstract} {string.Join(' ', metadataResult.Data.Keywords)}"
            : fileName;

        // ── Step 2: Language Detection ─────────────────────────────────
        var languageResult = await SafeRunAsync(
            "Language Detection",
            () => _languageDetector.DetectAsync(textCorpus, ct));

        // ── Step 3: Content Safety Check ───────────────────────────────
        var safetyResult = await SafeRunAsync(
            "Content Safety Check",
            () => _contentSafetyChecker.CheckAsync(textCorpus, ct));

        // ── Step 4: Plagiarism Detection ───────────────────────────────
        var plagiarismResult = await SafeRunAsync(
            "Plagiarism Detection",
            () => _plagiarismDetector.DetectAsync(textCorpus, ct));

        // ── Step 5: RAG Indexing ───────────────────────────────────────
        var ragResult = await SafeRunAsync(
            "RAG Indexing",
            () => _ragIndexer.IndexAsync(documentId, textCorpus, ct));

        // ── Step 6: AI Summarization ───────────────────────────────────
        var summaryResult = await SafeRunAsync(
            "AI Summarization",
            () => _aiSummarizer.SummarizeAsync(textCorpus, ct));

        // ── Step 7: Q&A System ─────────────────────────────────────────
        var indexId = ragResult.Data?.IndexId ?? $"idx-{documentId}";
        var qnaResult = await SafeRunAsync(
            "Q&A System",
            () => _qnAService.PrepareAsync(documentId, indexId, ct));

        totalSw.Stop();

        var overallSuccess =
            metadataResult.Success &&
            languageResult.Success &&
            safetyResult.Success &&
            plagiarismResult.Success &&
            ragResult.Success &&
            summaryResult.Success &&
            qnaResult.Success;

        _logger.LogInformation(
            "=== Pipeline {Status} for '{DocumentId}' in {Ms}ms ===",
            overallSuccess ? "COMPLETE" : "COMPLETE WITH ERRORS",
            documentId, totalSw.ElapsedMilliseconds);

        return new PipelineResult(
            DocumentId: documentId,
            FileName: fileName,
            OverallSuccess: overallSuccess,
            Metadata: metadataResult,
            Language: languageResult,
            ContentSafety: safetyResult,
            Plagiarism: plagiarismResult,
            RagIndex: ragResult,
            Summarization: summaryResult,
            QnA: qnaResult,
            TotalElapsedMs: totalSw.ElapsedMilliseconds
        );
    }

    /// <summary>Wraps a pipeline step so exceptions are captured rather than thrown.</summary>
    private async Task<StepResult<T>> SafeRunAsync<T>(string stepName, Func<Task<StepResult<T>>> step)
    {
        try
        {
            _logger.LogInformation("[{Step}] Starting", stepName);
            var result = await step();
            _logger.LogInformation("[{Step}] {Status} in {Ms}ms",
                stepName, result.Success ? "OK" : "FAILED", result.ElapsedMs);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Step}] Exception: {Message}", stepName, ex.Message);
            return new StepResult<T>(false, default, Error: ex.Message);
        }
    }
}
