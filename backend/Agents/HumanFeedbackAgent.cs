using System.Diagnostics;
using Backend.Models;

namespace Backend.Agents;

/// <summary>
/// Human Feedback Agent – stub implementation.
/// Evaluates overall pipeline confidence. If any step confidence is below 25 %,
/// or if content safety / plagiarism / validation flags are raised, the document
/// is flagged for admin review. Admins can then supply corrections which are
/// stored and used to improve future extractions.
/// TODO: Persist corrections to a durable store (e.g. Azure Table Storage / Cosmos DB)
/// and feed them back into SK Memory for continuous learning.
/// </summary>
public class HumanFeedbackAgent : IHumanFeedbackAgent
{
    private const double HitlConfidenceThreshold = 0.25;

    private readonly ILogger<HumanFeedbackAgent> _logger;

    // In-memory correction store (replace with durable storage)
    private static readonly Dictionary<string, List<FlaggedItem>> _corrections = [];

    public HumanFeedbackAgent(ILogger<HumanFeedbackAgent> logger) => _logger = logger;

    public async Task<StepResult<HumanFeedbackResult>> EvaluateAsync(
        string documentId, PipelineStepSummary summary, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "[HumanFeedbackAgent] Evaluating confidence for document '{DocumentId}'", documentId);

        await Task.Delay(100, ct); // TODO: SK filter / reflection step

        var flagged = new List<FlaggedItem>();

        // Flag content safety violations
        if (!summary.ContentSafetyPassed)
            flagged.Add(new FlaggedItem("ContentSafety", "Potential safety violation detected", 0.5, null));

        // Flag plagiarism above threshold
        if (summary.PlagiarismSimilarityPercent > 25.0)
            flagged.Add(new FlaggedItem(
                "Plagiarism",
                $"Similarity: {summary.PlagiarismSimilarityPercent:F1}% – exceeds 25% threshold",
                1.0 - summary.PlagiarismSimilarityPercent / 100.0,
                null));

        // Flag validation failures
        foreach (var issue in summary.ValidationIssues)
            flagged.Add(new FlaggedItem("Validation", issue, 0.0, null));

        // Flag low-confidence extraction fields (< 25%)
        if (summary.ExtractionConfidence < HitlConfidenceThreshold)
            flagged.Add(new FlaggedItem(
                "Extraction",
                $"Extraction confidence {summary.ExtractionConfidence:P0} is below HITL threshold",
                summary.ExtractionConfidence,
                null));

        var overallConfidence = flagged.Count == 0 ? 1.0
            : flagged.Average(f => f.Confidence);

        var requiresReview = flagged.Count > 0;

        var result = new HumanFeedbackResult(
            RequiresHumanReview: requiresReview,
            OverallConfidence: overallConfidence,
            FlaggedItems: flagged,
            IsResolved: false
        );

        sw.Stop();
        _logger.LogInformation(
            "[HumanFeedbackAgent] RequiresReview={Review}, Flags={Count}, Confidence={Conf:P0}, {Ms}ms",
            requiresReview, flagged.Count, overallConfidence, sw.ElapsedMilliseconds);

        return new StepResult<HumanFeedbackResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }

    public async Task ApplyCorrectionAsync(
        string documentId, string field, string correction, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[HumanFeedbackAgent] Admin correction for '{DocumentId}', field '{Field}': {Correction}",
            documentId, field, correction);

        await Task.Delay(50, ct); // TODO: persist to Azure Table Storage + update SK Memory

        var correctedItem = new FlaggedItem(field, "Admin-corrected", 1.0, correction);

        if (!_corrections.TryGetValue(documentId, out var list))
        {
            list = [];
            _corrections[documentId] = list;
        }
        list.Add(correctedItem);
    }
}
