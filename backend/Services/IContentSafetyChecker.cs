using Backend.Models;

namespace Backend.Services;

public interface IContentSafetyChecker
{
    Task<StepResult<ContentSafetyResult>> CheckAsync(string text, CancellationToken ct = default);
}
