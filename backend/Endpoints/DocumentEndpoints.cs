using Backend.Models;
using Backend.Pipeline;
using Backend.Services;

namespace Backend.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        // ── POST /api/documents/process ───────────────────────────────────────
        // Accepts a multipart/form-data upload with a single PDF file.
        // Runs the full 7-step pipeline and returns the aggregated result.
        group.MapPost("/process", async (
            IFormFile file,
            DocumentPipelineOrchestrator pipeline,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded." });

            if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "Only PDF files are supported." });

            var documentId = Guid.NewGuid().ToString("N")[..12];

            await using var stream = file.OpenReadStream();
            var result = await pipeline.RunAsync(documentId, file.FileName, stream, ct);

            return Results.Ok(result);
        })
        .DisableAntiforgery()
        .WithName("ProcessDocument")
        .WithSummary("Upload a PDF and run the full AI processing pipeline");

        // ── GET /api/documents/pipeline-steps ─────────────────────────────────
        // Returns metadata about each pipeline step — useful for frontend display.
        group.MapGet("/pipeline-steps", () => Results.Ok(new[]
        {
            new { id = 1, name = "Metadata Extraction",    icon = "🧠", description = "Extracts title, authors, abstract, keywords and page count." },
            new { id = 2, name = "Language Detection",     icon = "🌐", description = "Identifies primary language and detects multilingual sections." },
            new { id = 3, name = "Content Safety Check",  icon = "🛡️", description = "Scans for policy violations using AI content moderation." },
            new { id = 4, name = "Plagiarism Detection",  icon = "🔍", description = "Cross-references against academic databases for similarity." },
            new { id = 5, name = "RAG Indexing",          icon = "📚", description = "Chunks and embeds document into the vector knowledge base." },
            new { id = 6, name = "AI Summarization",      icon = "✨", description = "Generates structured summary and key findings via LLM." },
            new { id = 7, name = "Q&A System",            icon = "💬", description = "Enables interactive Q&A on the document via RAG pipeline." },
        }))
        .WithName("GetPipelineSteps")
        .WithSummary("List all pipeline steps with metadata");

        // ── POST /api/documents/{documentId}/ask ──────────────────────────────
        // Ask a question against an already-processed document.
        group.MapPost("/{documentId}/ask", async (
            string documentId,
            QnARequest body,
            IQnAService qnaService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Question))
                return Results.BadRequest(new { error = "Question cannot be empty." });

            var response = await qnaService.AskAsync(
                new QnARequest(documentId, body.Question), ct);

            return Results.Ok(response);
        })
        .WithName("AskQuestion")
        .WithSummary("Ask a natural language question against a processed document");

        return app;
    }
}
