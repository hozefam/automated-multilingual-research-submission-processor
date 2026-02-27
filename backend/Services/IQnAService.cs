using Backend.Models;

namespace Backend.Services;

public interface IQnAService
{
    Task<StepResult<QnAReadyResult>> PrepareAsync(string documentId, string indexId, CancellationToken ct = default);
    Task<QnAResponse> AskAsync(QnARequest request, CancellationToken ct = default);
}
