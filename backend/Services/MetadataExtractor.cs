using System.Diagnostics;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Stub implementation of metadata extraction.
/// Replace with real PDF parsing (e.g. PdfPig, iText, Azure Document Intelligence).
/// </summary>
public class MetadataExtractor : IMetadataExtractor
{
    private readonly ILogger<MetadataExtractor> _logger;

    public MetadataExtractor(ILogger<MetadataExtractor> logger) => _logger = logger;

    public async Task<StepResult<DocumentMetadata>> ExtractAsync(
        Stream pdfStream, string fileName, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Extracting metadata from '{FileName}'", fileName);

        await Task.Delay(500, ct); // TODO: replace with real PDF parsing

        var metadata = new DocumentMetadata(
            Title: Path.GetFileNameWithoutExtension(fileName),
            Authors: ["Author A", "Author B"],
            Abstract: "Abstract text extracted from the document.",
            Keywords: ["research", "multilingual", "AI"],
            PageCount: 12,
            Format: "PDF"
        );

        sw.Stop();
        _logger.LogInformation("Metadata extraction completed in {Ms}ms", sw.ElapsedMilliseconds);
        return new StepResult<DocumentMetadata>(true, metadata, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
