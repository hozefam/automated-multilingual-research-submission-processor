using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.SemanticKernel;

namespace Backend.Services.MetadataExtraction;

/// <summary>
/// SK Plugin – groups Native Functions used by the metadata pipeline.
///
/// ┌─────────────────────────────────────────────────────────────────┐
/// │  SK Concepts demonstrated:                                      │
/// │  • Plugin        – registered as "MetadataExtraction" plugin    │
/// │  • Native Fn #1  – extract_document_info  (Azure AI call)      │
/// │  • Native Fn #2  – validate_metadata_json (JSON validation)     │
/// └─────────────────────────────────────────────────────────────────┘
/// </summary>
public sealed class MetadataExtractionPlugin
{
    private readonly IConfiguration _config;
    private readonly ILogger<MetadataExtractionPlugin> _logger;

    // ── Semantic Function prompt template (registered separately in Program.cs) ──
    // Exposed as a constant so the kernel registration in Program.cs can use it.
    public const string ExtractionPromptTemplate = """
        You are an expert academic paper metadata extractor.
        Read the following research paper text and extract metadata.

        Return ONLY a single valid JSON object – no markdown, no explanation.

        Required JSON schema:
        {
          "title":    "<full paper title>",
          "authors":  ["<Author Name>", ...],
          "abstract": "<the full abstract paragraph>",
          "keywords": ["<keyword>", ...],
          "pageCount": 0,
          "format":   "PDF"
        }

        Research paper text:
        {{$documentText}}
        """;

    public MetadataExtractionPlugin(
        IConfiguration config,
        ILogger<MetadataExtractionPlugin> logger)
    {
        _config = config;
        _logger = logger;
    }

    // ── Native Function 1 ─────────────────────────────────────────────────────
    /// <summary>
    /// Calls Azure Document Intelligence (AI service) to extract all readable
    /// text and page count from a PDF.  Returns JSON: { "text": "...", "pageCount": N }.
    /// </summary>
    [KernelFunction("extract_document_info")]
    [Description("Extracts all readable text and page count from a PDF using Azure Document Intelligence AI. Returns JSON with 'text' and 'pageCount'.")]
    public async Task<string> ExtractDocumentInfoAsync(
        [Description("Base64-encoded bytes of the PDF file")] string base64PdfBytes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Plugin:NativeFn] extract_document_info – calling Azure Document Intelligence");

        var endpoint = _config["AzureDocumentIntelligence:Endpoint"]
            ?? throw new InvalidOperationException("AzureDocumentIntelligence:Endpoint is not configured.");
        var apiKey = _config["AzureDocumentIntelligence:ApiKey"]
            ?? throw new InvalidOperationException("AzureDocumentIntelligence:ApiKey is not configured.");

        var client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        var pdfBytes = Convert.FromBase64String(base64PdfBytes);

        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed, "prebuilt-read", BinaryData.FromBytes(pdfBytes), cancellationToken: cancellationToken);

        var result = operation.Value;
        var sb = new StringBuilder();

        foreach (var page in result.Pages)
            foreach (var line in page.Lines ?? [])
                sb.AppendLine(line.Content);

        var text = sb.ToString();
        var pageCount = result.Pages.Count;

        _logger.LogInformation("[Plugin:NativeFn] extract_document_info – extracted {Chars} chars, {Pages} pages",
            text.Length, pageCount);

        return JsonSerializer.Serialize(new { text, pageCount });
    }

    // ── Native Function 2 ─────────────────────────────────────────────────────
    /// <summary>
    /// Validates that a metadata JSON string contains all required fields.
    /// Returns an empty string when valid, or a description of missing/invalid fields.
    /// </summary>
    [KernelFunction("validate_metadata_json")]
    [Description("Validates that metadata JSON contains all required fields: title, authors, abstract, keywords, pageCount, format. Returns empty string if valid, otherwise returns a description of the problems.")]
    public string ValidateMetadataJson(
        [Description("JSON string of extracted metadata to validate")] string metadataJson)
    {
        _logger.LogInformation("[Plugin:NativeFn] validate_metadata_json – validating JSON");

        try
        {
            var clean = StripMarkdownFences(metadataJson);
            using var doc = JsonDocument.Parse(clean);
            var root = doc.RootElement;
            var missing = new List<string>();

            if (!root.TryGetProperty("title", out var t) || string.IsNullOrWhiteSpace(t.GetString()))
                missing.Add("title");
            if (!root.TryGetProperty("authors", out var a) || a.GetArrayLength() == 0)
                missing.Add("authors");
            if (!root.TryGetProperty("abstract", out var ab) || string.IsNullOrWhiteSpace(ab.GetString()))
                missing.Add("abstract");
            if (!root.TryGetProperty("keywords", out var k) || k.GetArrayLength() == 0)
                missing.Add("keywords");
            if (!root.TryGetProperty("pageCount", out _))
                missing.Add("pageCount");
            if (!root.TryGetProperty("format", out _))
                missing.Add("format");

            var validationResult = missing.Count == 0
                ? string.Empty
                : $"Missing or empty fields: {string.Join(", ", missing)}";

            _logger.LogInformation("[Plugin:NativeFn] validate_metadata_json – result: {Result}",
                string.IsNullOrEmpty(validationResult) ? "VALID" : validationResult);

            return validationResult;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("[Plugin:NativeFn] validate_metadata_json – invalid JSON: {Error}", ex.Message);
            return $"Invalid JSON: {ex.Message}";
        }
    }

    // ── Utility ───────────────────────────────────────────────────────────────
    /// <summary>Strips ```json ... ``` fences that LLMs sometimes add.</summary>
    public static string StripMarkdownFences(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[7..];
        else if (trimmed.StartsWith("```"))
            trimmed = trimmed[3..];
        if (trimmed.EndsWith("```"))
            trimmed = trimmed[..^3];
        return trimmed.Trim();
    }
}
