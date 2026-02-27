using Backend.Agents;
using Backend.Models;
using Backend.Pipeline;

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
            new { id =  1, name = "Ingestion Agent",            icon = "📥", description = "Simulates email inbox monitoring by reading submissions from the file-system watch folder." },
            new { id =  2, name = "Pre-process Agent",          icon = "🔄", description = "Validates file type, runs OCR on scanned documents, and detects the primary language." },
            new { id =  3, name = "Translation Agent",          icon = "🌍", description = "Translates non-English submissions to English; stores original and translated text." },
            new { id =  4, name = "Extraction Agent",           icon = "🧠", description = "Extracts title, authors, affiliations, abstract, keywords and figures." },
            new { id =  5, name = "Validation Agent",           icon = "✔️",  description = "Enforces business rules: page count 8-25, required sections present (title, abstract, keywords, authors, references)." },
            new { id =  6, name = "Content Safety Agent",       icon = "🛡️", description = "Scans for toxicity, hate speech and illicit content; flags for human review when violations detected." },
            new { id =  7, name = "Plagiarism Detection Agent", icon = "🔍", description = "Cross-references against academic databases for similarity; flags if > 25 %." },
            new { id =  8, name = "RAG Agent",                  icon = "📚", description = "Generates embeddings and maintains the vector store for retrieval, augmentation and generation." },
            new { id =  9, name = "Summary Agent",              icon = "✨",  description = "Produces a ≤250-word summary highlighting key findings, validation issues and missing sections." },
            new { id = 10, name = "Q&A Agent",                  icon = "💬", description = "Enables multilingual conversational Q&A on the document with full chat history." },
            new { id = 11, name = "Human Feedback Agent",       icon = "👤", description = "Presents flagged items to admin for HITL review; accepts corrections when confidence < 25 %." },
        }))
        .WithName("GetPipelineSteps")
        .WithSummary("List all pipeline steps with metadata");

        // ── POST /api/documents/{documentId}/ask ──────────────────────────────
        // Ask a question against an already-processed document.
        group.MapPost("/{documentId}/ask", async (
            string documentId,
            QnARequest body,
            IQnAAgent qnaAgent,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Question))
                return Results.BadRequest(new { error = "Question cannot be empty." });

            var response = await qnaAgent.AskAsync(
                new QnARequest(documentId, body.Question, body.SessionId), ct);

            return Results.Ok(response);
        })
        .WithName("AskQuestion")
        .WithSummary("Ask a natural language question against a processed document");

        // ── POST /api/documents/{documentId}/correct ──────────────────────────
        // Admin submits a correction for a flagged HITL item.
        group.MapPost("/{documentId}/correct", async (
            string documentId,
            HitlCorrectionRequest body,
            IHumanFeedbackAgent humanFeedbackAgent,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Field) || string.IsNullOrWhiteSpace(body.Correction))
                return Results.BadRequest(new { error = "Field and Correction are required." });

            await humanFeedbackAgent.ApplyCorrectionAsync(documentId, body.Field, body.Correction, ct);

            return Results.Ok(new { documentId, body.Field, body.Correction, appliedAt = DateTime.UtcNow });
        })
        .WithName("SubmitHitlCorrection")
        .WithTags("Documents")
        .WithSummary("Admin submits a Human-In-The-Loop correction for a flagged item");

        return app;
    }
}
