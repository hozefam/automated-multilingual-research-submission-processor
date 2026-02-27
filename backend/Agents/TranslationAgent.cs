using System.Diagnostics;
using Backend.Models;

namespace Backend.Agents;

/// <summary>
/// Translation Agent – stub implementation.
/// TODO: Replace with Azure AI Translator SDK call to translate non-English
/// document text into English. Store both original and translated versions
/// in the document context so downstream agents can reference either language.
/// </summary>
public class TranslationAgent : ITranslationAgent
{
    private readonly ILogger<TranslationAgent> _logger;

    public TranslationAgent(ILogger<TranslationAgent> logger) => _logger = logger;

    public async Task<StepResult<TranslationResult>> TranslateAsync(
        string text, string sourceLanguageCode, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "[TranslationAgent] Translating {CharCount} characters from '{Lang}'",
            text.Length, sourceLanguageCode);

        // Skip translation for English documents
        var needsTranslation = !string.Equals(sourceLanguageCode, "en", StringComparison.OrdinalIgnoreCase);

        await Task.Delay(needsTranslation ? 800 : 50, ct); // TODO: Azure Translator SDK

        var result = new TranslationResult(
            OriginalText: text,
            TranslatedText: needsTranslation
                ? $"[Stub] English translation of {text.Length}-character {sourceLanguageCode} document."
                : text,
            SourceLanguage: sourceLanguageCode,
            SourceLanguageName: needsTranslation ? "Detected Language" : "English",
            WasTranslated: needsTranslation
        );

        sw.Stop();
        _logger.LogInformation(
            "[TranslationAgent] WasTranslated={Translated}, completed in {Ms}ms",
            result.WasTranslated, sw.ElapsedMilliseconds);

        return new StepResult<TranslationResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
