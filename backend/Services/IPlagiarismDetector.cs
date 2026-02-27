using Backend.Models;

namespace Backend.Services;

public interface IPlagiarismDetector
{
    Task<StepResult<PlagiarismResult>> DetectAsync(string text, CancellationToken ct = default);
}
