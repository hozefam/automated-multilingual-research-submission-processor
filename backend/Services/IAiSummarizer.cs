using Backend.Models;

namespace Backend.Services;

public interface IAiSummarizer
{
    Task<StepResult<SummarizationResult>> SummarizeAsync(string text, CancellationToken ct = default);
}
