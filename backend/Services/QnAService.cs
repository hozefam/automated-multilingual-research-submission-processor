using System.Diagnostics;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Stub implementation of Q&A readiness and querying.
/// Replace with Azure AI Search + Azure OpenAI RAG pattern or Semantic Kernel.
/// </summary>
public class QnAService : IQnAService
{
    private readonly ILogger<QnAService> _logger;

    public QnAService(ILogger<QnAService> logger) => _logger = logger;

    public async Task<StepResult<QnAReadyResult>> PrepareAsync(
        string documentId, string indexId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Preparing Q&A system for document '{DocumentId}'", documentId);

        await Task.Delay(400, ct); // TODO: validate index and register document for Q&A

        var result = new QnAReadyResult(
            IsReady: true,
            IndexId: indexId,
            Endpoint: $"/api/qna/{documentId}/ask"
        );

        sw.Stop();
        _logger.LogInformation("Q&A system ready for '{DocumentId}' at {Endpoint}", documentId, result.Endpoint);
        return new StepResult<QnAReadyResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }

    public async Task<QnAResponse> AskAsync(QnARequest request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Q&A query on document '{DocumentId}': {Question}",
            request.DocumentId, request.Question);

        await Task.Delay(700, ct); // TODO: RAG retrieval → LLM completion

        return new QnAResponse(
            Question: request.Question,
            Answer: $"[Stub] Based on the indexed document, the answer to \"{request.Question}\" " +
                    "would be retrieved via RAG from the vector store.",
            Sources: [$"Document: {request.DocumentId}, Chunk 3", $"Document: {request.DocumentId}, Chunk 7"],
            Confidence: 0.87
        );
    }
}
