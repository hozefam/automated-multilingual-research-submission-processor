using System.Diagnostics;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Stub implementation of language detection.
/// Replace with Azure AI Language / CLD3 / FastText.
/// </summary>
public class LanguageDetector : ILanguageDetector
{
    private readonly ILogger<LanguageDetector> _logger;

    public LanguageDetector(ILogger<LanguageDetector> logger) => _logger = logger;

    public async Task<StepResult<LanguageDetectionResult>> DetectAsync(
        string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Detecting language on {CharCount} characters", text.Length);

        await Task.Delay(300, ct); // TODO: replace with real detection

        var result = new LanguageDetectionResult(
            PrimaryLanguage: "English",
            LanguageCode: "en",
            Confidence: 0.98,
            AdditionalLanguages: []
        );

        sw.Stop();
        _logger.LogInformation("Language detected: {Lang} ({Conf:P0})", result.PrimaryLanguage, result.Confidence);
        return new StepResult<LanguageDetectionResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
