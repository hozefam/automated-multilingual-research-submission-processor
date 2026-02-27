using Backend.Models;

namespace Backend.Services;

public interface IMetadataExtractor
{
    Task<StepResult<DocumentMetadata>> ExtractAsync(Stream pdfStream, string fileName, CancellationToken ct = default);
}
