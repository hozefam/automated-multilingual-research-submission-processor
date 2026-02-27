using System.Diagnostics;
using Backend.Models;

namespace Backend.Agents;

/// <summary>
/// Extraction Agent – stub implementation.
/// TODO: Replace with Azure Document Intelligence + SK Native Functions
/// to extract title, authors, affiliations, abstract, keywords and figures.
/// </summary>
public class ExtractionAgent : IExtractionAgent
{
    private readonly ILogger<ExtractionAgent> _logger;

    public ExtractionAgent(ILogger<ExtractionAgent> logger) => _logger = logger;

    public async Task<StepResult<DocumentMetadata>> ExtractAsync(
        Stream pdfStream, string fileName, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[ExtractionAgent] Extracting structured fields from '{FileName}'", fileName);

        await Task.Delay(500, ct); // TODO: Azure Document Intelligence + SK plugin

        var metadata = new DocumentMetadata(
            Title: Path.GetFileNameWithoutExtension(fileName),
            Authors: ["Author A", "Author B"],
            Affiliations: ["University of Research"],
            Abstract: "Abstract text extracted from the document.",
            Keywords: ["research", "multilingual", "AI"],
            Figures: ["Figure 1: System Architecture"],
            PageCount: 12,
            Format: "PDF"
        );

        sw.Stop();
        _logger.LogInformation("[ExtractionAgent] Completed in {Ms}ms", sw.ElapsedMilliseconds);
        return new StepResult<DocumentMetadata>(true, metadata, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
