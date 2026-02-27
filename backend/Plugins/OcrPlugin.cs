using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;
using Tesseract;
using UglyToad.PdfPig;

namespace Backend.Plugins;

/// <summary>
/// Semantic Kernel plugin that extracts plain text from uploaded files.
/// - PDFs: uses PdfPig to extract the text layer directly (works for all
///         standard research-paper PDFs; scanned-image PDFs yield empty text).
/// - Images: uses Tesseract OCR (PNG, JPEG, TIFF, BMP, WebP …).
/// </summary>
[Description(
    "Extracts plain text from PDF or image files. " +
    "PDF files are handled via PdfPig text extraction; " +
    "image files are processed with Tesseract OCR.")]
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

    [KernelFunction("ExtractTextFromFile")]
    [Description(
        "Extracts plain text from a PDF (via PdfPig) or an image file (via Tesseract OCR). " +
        "Returns an empty string for unsupported formats or on failure.")]
    public Task<string> ExtractTextFromFileAsync(
        [Description("Raw bytes of the file to process")] byte[] fileBytes,
        [Description("Original file name including extension, e.g. paper.pdf or scan.png")] string fileName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[OcrPlugin] ExtractTextFromFile invoked for '{FileName}' ({Bytes} bytes).",
            fileName, fileBytes.Length);

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        if (ext == "pdf")
            return Task.FromResult(ExtractFromPdf(fileBytes, fileName));

        if (ImageExtensions.Contains(ext))
            return Task.FromResult(ExtractFromImageBytes(fileBytes, fileName));

        _logger.LogWarning(
            "[OcrPlugin] Unsupported file extension '{Ext}' for '{FileName}' — skipping.",
            ext, fileName);
        return Task.FromResult(string.Empty);
    }

    // ── PDF extraction (PdfPig) ───────────────────────────────────────────────

    private string ExtractFromPdf(byte[] pdfBytes, string fileName)
    {
        try
        {
            using var doc = PdfDocument.Open(pdfBytes);
            var sb = new StringBuilder();

            foreach (var page in doc.GetPages())
            {
                // GetWords() clusters characters into readable tokens
                foreach (var word in page.GetWords())
                    sb.Append(word.Text).Append(' ');
                sb.AppendLine();
            }

            var text = sb.ToString().Trim();

            if (string.IsNullOrWhiteSpace(text))
                _logger.LogWarning(
                    "[OcrPlugin] PdfPig found no text layer in '{FileName}'. " +
                    "The file may be a scanned/image-only PDF.", fileName);
            else
                _logger.LogInformation(
                    "[OcrPlugin] PdfPig extracted {Chars} chars from '{FileName}'.",
                    text.Length, fileName);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[OcrPlugin] PdfPig failed to open '{FileName}' — returning empty string.", fileName);
            return string.Empty;
        }
    }

    // ── Image extraction (Tesseract) ─────────────────────────────────────────

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
                "[OcrPlugin] Tesseract complete for '{FileName}': {Chars} chars, confidence {Confidence:P0}.",
                fileName, text.Length, confidence);

            return text ?? string.Empty;
        }
        catch (TesseractException tex)
        {
            _logger.LogWarning(tex,
                "[OcrPlugin] Tesseract failed for '{FileName}'. " +
                "Ensure tessdata is at '{TessdataPath}' with language '{Language}'.",
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
