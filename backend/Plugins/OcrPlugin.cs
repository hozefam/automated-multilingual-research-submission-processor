using System.ComponentModel;
using Microsoft.SemanticKernel;
using Tesseract;

namespace Backend.Plugins;

/// <summary>
/// Semantic Kernel plugin that wraps Tesseract OCR.
/// Exposes a single KernelFunction so any SK agent in the pipeline can invoke
/// OCR on a raw file payload without coupling to the Tesseract API directly.
/// </summary>
[Description(
    "Optical Character Recognition plugin powered by Tesseract. " +
    "Extracts plain text from image files so downstream agents " +
    "can operate on the document content.")]
public sealed class OcrPlugin
{
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { "png", "jpg", "jpeg", "tif", "tiff", "bmp", "gif", "webp" };

    private readonly ILogger<OcrPlugin> _logger;
    private readonly string _tessdataPath;
    private readonly string _language;

    public OcrPlugin(ILogger<OcrPlugin> logger, IConfiguration config)
    {
        _logger = logger;
        _tessdataPath = config["Tesseract:TessdataPath"] ?? "tessdata";
        _language = config["Tesseract:Language"] ?? "eng";
    }

    /// <summary>
    /// Extracts plain text from an image file using Tesseract OCR.
    /// PDF files are not supported without a PDF-to-image renderer and will
    /// return an empty string with a diagnostic log message.
    /// </summary>
    /// <param name="fileBytes">Raw bytes of the file to process.</param>
    /// <param name="fileName">Original file name (used to determine format).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted plain text, or an empty string on failure.</returns>
    [KernelFunction("ExtractTextFromFile")]
    [Description(
        "Runs Tesseract OCR on the supplied image file bytes and returns the extracted plain text. " +
        "Supports PNG, JPEG, TIFF, BMP and similar raster formats. " +
        "Returns an empty string when tessdata is unavailable or the format is unsupported.")]
    public Task<string> ExtractTextFromFileAsync(
        [Description("Raw bytes of the image file to process")] byte[] fileBytes,
        [Description("Original file name including extension, e.g. scan.png")] string fileName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[OcrPlugin] ExtractTextFromFile invoked for '{FileName}' ({Bytes} bytes).",
            fileName, fileBytes.Length);

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        if (ext == "pdf")
        {
            _logger.LogWarning(
                "[OcrPlugin] PDF rendering requires a dedicated library (e.g. PdfPig). " +
                "Only pure Tesseract is enabled — skipping '{FileName}'.",
                fileName);
            return Task.FromResult(string.Empty);
        }

        if (!ImageExtensions.Contains(ext))
        {
            _logger.LogWarning(
                "[OcrPlugin] Unsupported file extension '{Ext}' for '{FileName}' — skipping.",
                ext, fileName);
            return Task.FromResult(string.Empty);
        }

        return Task.FromResult(ExtractFromImageBytes(fileBytes, fileName));
    }

    // ── private ──────────────────────────────────────────────────────────────

    private string ExtractFromImageBytes(byte[] imageBytes, string fileName)
    {
        try
        {
            using var engine = new TesseractEngine(_tessdataPath, _language, EngineMode.Default);
            using var img = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(img);

            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            _logger.LogInformation(
                "[OcrPlugin] OCR complete for '{FileName}': {Chars} chars, confidence {Confidence:P0}.",
                fileName, text.Length, confidence);

            return text ?? string.Empty;
        }
        catch (TesseractException tex)
        {
            _logger.LogWarning(tex,
                "[OcrPlugin] Tesseract failed for '{FileName}'. " +
                "Ensure tessdata is present at '{TessdataPath}' and language '{Language}' is installed.",
                fileName, _tessdataPath, _language);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[OcrPlugin] Unexpected error during OCR for '{FileName}' — returning empty string.",
                fileName);
            return string.Empty;
        }
    }
}
