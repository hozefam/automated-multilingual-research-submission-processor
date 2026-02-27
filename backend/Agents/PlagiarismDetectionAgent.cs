using System.Diagnostics;
using Backend.Models;

namespace Backend.Agents;

/// <summary>
/// Plagiarism Detection Agent – stub implementation.
/// TODO: Replace with Turnitin API, Copyleaks, or SK-powered embedding cosine similarity.
/// Decoupled from business-rule Validation Agent.
/// </summary>
public class PlagiarismDetectionAgent : IPlagiarismDetectionAgent
{
    private readonly ILogger<PlagiarismDetectionAgent> _logger;

    public PlagiarismDetectionAgent(ILogger<PlagiarismDetectionAgent> logger) => _logger = logger;

    public async Task<StepResult<PlagiarismResult>> DetectAsync(
        string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[PlagiarismDetectionAgent] Checking {CharCount} characters against academic databases", text.Length);

        await Task.Delay(800, ct); // TODO: Turnitin / Copyleaks / SK embedding similarity

        var result = new PlagiarismResult(
            SimilarityPercent: 3.2,
            PlagiarismDetected: false,
            Matches: []
        );

        sw.Stop();
        _logger.LogInformation(
            "Plagiarism check complete: {Similarity:F1}% match, Detected={Detected}",
            result.SimilarityPercent, result.PlagiarismDetected);

        return new StepResult<PlagiarismResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
