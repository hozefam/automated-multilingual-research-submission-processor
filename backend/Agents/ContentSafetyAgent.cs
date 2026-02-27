using System.Diagnostics;
using Backend.Models;

namespace Backend.Agents;

/// <summary>
/// Content Safety Agent – stub implementation.
/// TODO: Replace with Azure AI Content Safety SDK to detect toxicity,
/// hate speech, violence and illicit content. Flag for HITL when flagged.
/// Decoupled from business-rule Validation Agent.
/// </summary>
public class ContentSafetyAgent : IContentSafetyAgent
{
    private readonly ILogger<ContentSafetyAgent> _logger;

    public ContentSafetyAgent(ILogger<ContentSafetyAgent> logger) => _logger = logger;

    public async Task<StepResult<ContentSafetyResult>> CheckAsync(
        string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[ContentSafetyAgent] Scanning {CharCount} characters for toxicity and illicit content", text.Length);

        await Task.Delay(600, ct); // TODO: Azure AI Content Safety SDK

        var result = new ContentSafetyResult(
            IsSafe: true,
            Flags: [],
            OverallRating: "Safe"
        );

        sw.Stop();
        _logger.LogInformation("Content safety result: {Rating}", result.OverallRating);
        return new StepResult<ContentSafetyResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
