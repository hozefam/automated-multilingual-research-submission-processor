using System.Diagnostics;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Stub implementation of plagiarism detection.
/// Replace with Turnitin API, Copyleaks, or custom embedding cosine similarity.
/// </summary>
public class PlagiarismDetector : IPlagiarismDetector
{
    private readonly ILogger<PlagiarismDetector> _logger;

    public PlagiarismDetector(ILogger<PlagiarismDetector> logger) => _logger = logger;

    public async Task<StepResult<PlagiarismResult>> DetectAsync(
        string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Running plagiarism detection on {CharCount} characters", text.Length);

        await Task.Delay(800, ct); // TODO: replace with real plagiarism API

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
