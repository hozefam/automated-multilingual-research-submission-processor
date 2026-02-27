using System.Diagnostics;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Stub implementation of content safety checking.
/// Replace with Azure AI Content Safety or OpenAI Moderation API.
/// </summary>
public class ContentSafetyChecker : IContentSafetyChecker
{
    private readonly ILogger<ContentSafetyChecker> _logger;

    public ContentSafetyChecker(ILogger<ContentSafetyChecker> logger) => _logger = logger;

    public async Task<StepResult<ContentSafetyResult>> CheckAsync(
        string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Running content safety check on {CharCount} characters", text.Length);

        await Task.Delay(600, ct); // TODO: replace with Azure Content Safety SDK call

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
