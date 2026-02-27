using System.Diagnostics;
using Backend.Models;

namespace Backend.Agents;

/// <summary>
/// Pre-process Agent – stub implementation.
/// TODO: Validate file type, run Azure Document Intelligence OCR for scanned PDFs,
/// and call Azure AI Language for language detection.
/// </summary>
public class PreProcessAgent : IPreProcessAgent
{
    private readonly ILogger<PreProcessAgent> _logger;

    public PreProcessAgent(ILogger<PreProcessAgent> logger) => _logger = logger;

    public async Task<StepResult<PreProcessResult>> PreProcessAsync(
        Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[PreProcessAgent] Validating file type, checking for OCR need, detecting language for '{FileName}'", fileName);

        await Task.Delay(300, ct); // TODO: Azure Document Intelligence OCR + Azure AI Language

        var ext = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();
        var isValidType = ext is "PDF" or "DOCX" or "DOC";

        var result = new PreProcessResult(
            IsValidFileType: isValidType,
            DetectedFileType: ext,
            OcrApplied: false,               // TODO: detect if scanned and run OCR
            ExtractedText: $"[Stub] Extracted text from {fileName}",
            PrimaryLanguage: "English",
            LanguageCode: "en",
            LanguageConfidence: 0.98
        );

        sw.Stop();
        _logger.LogInformation(
            "[PreProcessAgent] Completed: ValidFile={Valid}, Lang={Lang}, OCR={Ocr}, {Ms}ms",
            result.IsValidFileType, result.PrimaryLanguage, result.OcrApplied, sw.ElapsedMilliseconds);
        return new StepResult<PreProcessResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
