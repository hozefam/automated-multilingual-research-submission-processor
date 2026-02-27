using System.Diagnostics;
using Backend.Models;

namespace Backend.Agents;

/// <summary>
/// RAG Agent – stub implementation.
/// TODO: Chunk document text, generate embeddings via Azure OpenAI,
/// and upsert into Azure AI Search vector store using SK Memory.
/// </summary>
public class RagAgent : IRagAgent
{
    private readonly ILogger<RagAgent> _logger;

    public RagAgent(ILogger<RagAgent> logger) => _logger = logger;

    public async Task<StepResult<RagIndexResult>> IndexAsync(
        string documentId, string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[RagAgent] Indexing document '{DocumentId}' into vector store", documentId);

        await Task.Delay(900, ct); // TODO: chunk → embed → upsert into Azure AI Search

        // Simulate chunking: ~500 chars per chunk
        var estimatedChunks = Math.Max(1, text.Length / 500);
        var estimatedTokens = estimatedChunks * 380;

        var result = new RagIndexResult(
            IndexId: $"idx-{documentId}",
            ChunksIndexed: estimatedChunks,
            TotalTokens: estimatedTokens,
            VectorStore: "Azure AI Search"
        );

        sw.Stop();
        _logger.LogInformation(
            "RAG indexing complete: {Chunks} chunks, {Tokens} tokens in {Ms}ms",
            result.ChunksIndexed, result.TotalTokens, sw.ElapsedMilliseconds);

        return new StepResult<RagIndexResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
