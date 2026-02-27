namespace Backend.Models;

// ── Upload / Pipeline request ─────────────────────────────────────────────

public record DocumentUploadResponse(
    string DocumentId,
    string FileName,
    long FileSizeBytes,
    string Message
);

// ── Shared pipeline result wrapper ───────────────────────────────────────

public record StepResult<T>(
    bool Success,
    T? Data,
    string? Error = null,
    long ElapsedMs = 0
);

// ── 1. Metadata Extraction ────────────────────────────────────────────────

public record DocumentMetadata(
    string Title,
    List<string> Authors,
    string Abstract,
    List<string> Keywords,
    int PageCount,
    string Format
);

// ── 2. Language Detection ─────────────────────────────────────────────────

public record LanguageDetectionResult(
    string PrimaryLanguage,
    string LanguageCode,
    double Confidence,
    List<string> AdditionalLanguages
);

// ── 3. Content Safety ─────────────────────────────────────────────────────

public enum SafetyCategory { None, HateSpeech, Violence, SexualContent, SelfHarm }

public record ContentSafetyResult(
    bool IsSafe,
    List<SafetyFlag> Flags,
    string OverallRating
);

public record SafetyFlag(SafetyCategory Category, double Severity, string Detail);

// ── 4. Plagiarism Detection ───────────────────────────────────────────────

public record PlagiarismResult(
    double SimilarityPercent,
    bool PlagiarismDetected,
    List<PlagiarismMatch> Matches
);

public record PlagiarismMatch(
    string Source,
    double Similarity,
    string MatchedText
);

// ── 5. RAG Indexing ───────────────────────────────────────────────────────

public record RagIndexResult(
    string IndexId,
    int ChunksIndexed,
    int TotalTokens,
    string VectorStore
);

// ── 6. AI Summarization ───────────────────────────────────────────────────

public record SummarizationResult(
    string Summary,
    List<string> KeyFindings,
    List<string> Topics,
    string Methodology
);

// ── 7. Q&A System ────────────────────────────────────────────────────────

public record QnAReadyResult(
    bool IsReady,
    string IndexId,
    string Endpoint
);

public record QnARequest(string DocumentId, string Question);

public record QnAResponse(
    string Question,
    string Answer,
    List<string> Sources,
    double Confidence
);

// ── Full Pipeline Result ──────────────────────────────────────────────────

public record PipelineResult(
    string DocumentId,
    string FileName,
    bool OverallSuccess,
    StepResult<DocumentMetadata> Metadata,
    StepResult<LanguageDetectionResult> Language,
    StepResult<ContentSafetyResult> ContentSafety,
    StepResult<PlagiarismResult> Plagiarism,
    StepResult<RagIndexResult> RagIndex,
    StepResult<SummarizationResult> Summarization,
    StepResult<QnAReadyResult> QnA,
    long TotalElapsedMs
);
