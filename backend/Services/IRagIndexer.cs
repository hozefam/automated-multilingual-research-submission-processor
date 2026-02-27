using Backend.Models;

namespace Backend.Services;

public interface IRagIndexer
{
    Task<StepResult<RagIndexResult>> IndexAsync(string documentId, string text, CancellationToken ct = default);
}
