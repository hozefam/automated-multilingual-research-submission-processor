using System.Diagnostics;
using Backend.Models;

namespace Backend.Agents;

/// <summary>
/// Validation Agent – stub implementation.
/// Enforces submission business rules (decoupled from content safety and plagiarism):
///   • Page count: minimum 8, maximum 25
///   • Required sections: Title, Abstract, Keywords, Authors, References
/// TODO: Replace section detection with SK Semantic Function / Native Function
/// that performs semantic search for section headers within extracted text.
/// </summary>
public class ValidationAgent : IValidationAgent
{
    private const int MinPages = 8;
    private const int MaxPages = 25;

    private static readonly string[] RequiredSections =
        ["Title", "Abstract", "Keywords", "Authors", "References"];

    private readonly ILogger<ValidationAgent> _logger;

    public ValidationAgent(ILogger<ValidationAgent> logger) => _logger = logger;

    public async Task<StepResult<ValidationResult>> ValidateAsync(
        DocumentMetadata metadata, string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "[ValidationAgent] Validating submission '{Title}' ({Pages} pages)",
            metadata.Title, metadata.PageCount);

        await Task.Delay(200, ct); // TODO: SK Native Function for semantic section detection

        var issues = new List<string>();
        var missingSections = new List<string>();

        // Rule 1: Page count compliance
        var pageCountOk = metadata.PageCount >= MinPages && metadata.PageCount <= MaxPages;
        if (!pageCountOk)
            issues.Add($"Page count {metadata.PageCount} is outside the allowed range ({MinPages}–{MaxPages}).");

        // Rule 2: Required sections present (stub: check if metadata fields are non-empty)
        // TODO: Replace with semantic section header detection via SK plugin
        if (string.IsNullOrWhiteSpace(metadata.Title)) { missingSections.Add("Title"); issues.Add("Title section is missing."); }
        if (string.IsNullOrWhiteSpace(metadata.Abstract)) { missingSections.Add("Abstract"); issues.Add("Abstract section is missing."); }
        if (metadata.Keywords.Count == 0) { missingSections.Add("Keywords"); issues.Add("Keywords section is missing."); }
        if (metadata.Authors.Count == 0) { missingSections.Add("Authors"); issues.Add("Authors section is missing."); }
        if (!text.Contains("References", StringComparison.OrdinalIgnoreCase))
        { missingSections.Add("References"); issues.Add("References section not detected."); }

        var isValid = issues.Count == 0;
        var result = new ValidationResult(
            IsValid: isValid,
            PageCount: metadata.PageCount,
            IsPageCountCompliant: pageCountOk,
            MissingSections: missingSections,
            ValidationIssues: issues
        );

        sw.Stop();
        _logger.LogInformation(
            "[ValidationAgent] Result: IsValid={Valid}, Issues={IssueCount}, MissingSections={Missing}, {Ms}ms",
            result.IsValid, result.ValidationIssues.Count, result.MissingSections.Count, sw.ElapsedMilliseconds);

        return new StepResult<ValidationResult>(isValid, result, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
