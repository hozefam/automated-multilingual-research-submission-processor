using Backend.Models;

namespace Backend.Services;

public interface ILanguageDetector
{
    Task<StepResult<LanguageDetectionResult>> DetectAsync(string text, CancellationToken ct = default);
}
