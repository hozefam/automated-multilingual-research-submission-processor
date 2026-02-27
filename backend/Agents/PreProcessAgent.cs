using System.Diagnostics;
using Backend.Models;
using Microsoft.SemanticKernel;

namespace Backend.Agents;

/// <summary>
/// Pre-process Agent – validates file type and invokes the SK OcrPlugin.
/// TODO: Add Azure AI Language for language detection.
/// </summary>
public class PreProcessAgent : IPreProcessAgent
{
    private readonly ILogger<PreProcessAgent> _logger;
    private readonly Kernel _kernel;

    public PreProcessAgent(ILogger<PreProcessAgent> logger, Kernel kernel)
    {
        _logger = logger;
        _kernel = kernel;
    }

    public async Task<StepResult<PreProcessResult>> PreProcessAsync(
        Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "[PreProcessAgent] Running OcrPlugin for '{FileName}'", fileName);

        // Buffer the stream so the SK plugin can receive raw bytes
        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            await fileStream.CopyToAsync(ms, ct);
            fileBytes = ms.ToArray();
        }

        // Invoke the Tesseract OCR SK plugin
        var ocrResult = await _kernel.InvokeAsync<string>(
            "OcrPlugin", "ExtractTextFromFile",
            new KernelArguments
            {
                ["fileBytes"] = fileBytes,
                ["fileName"] = fileName
            },
            ct);

        var extractedText = ocrResult ?? string.Empty;
        var ext = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();
        var isValidType = ext is "PDF" or "DOCX" or "DOC";

        var result = new PreProcessResult(
            IsValidFileType: isValidType,
            DetectedFileType: ext,
            OcrApplied: !string.IsNullOrEmpty(extractedText),
            ExtractedText: extractedText,
            PrimaryLanguage: "English",         // TODO: Azure AI Language detection
            LanguageCode: "en",
            LanguageConfidence: 0.98
        );

        sw.Stop();
        _logger.LogInformation(
            "[PreProcessAgent] Completed: ValidFile={Valid}, OcrApplied={Ocr}, TextLength={Len}, {Ms}ms",
            result.IsValidFileType, result.OcrApplied, extractedText.Length, sw.ElapsedMilliseconds);

        return new StepResult<PreProcessResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
