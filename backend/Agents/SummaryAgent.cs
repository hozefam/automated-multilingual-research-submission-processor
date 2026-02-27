using System.Diagnostics;
using Backend.Models;

namespace Backend.Agents;

/// <summary>
/// Summary Agent – stub implementation.
/// TODO: Use SK Semantic Function with a prompt template to generate a
/// ≤250-word structured summary including key findings, validation issues
/// and missing sections via Azure OpenAI.
/// </summary>
public class SummaryAgent : ISummaryAgent
{
    private readonly ILogger<SummaryAgent> _logger;

    public SummaryAgent(ILogger<SummaryAgent> logger) => _logger = logger;

    public async Task<StepResult<SummarizationResult>> SummarizeAsync(
        string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Generating AI summary for {CharCount} characters", text.Length);

        await Task.Delay(1000, ct); // TODO: call Azure OpenAI completions endpoint

        var result = new SummarizationResult(
            Summary: "This paper presents a novel approach to automated multilingual research " +
                     "processing using AI-driven pipeline stages including extraction, safety, " +
                     "plagiarism detection and RAG-based Q&A.",
            KeyFindings: [
                "Multilingual document handling at scale",
                "AI-powered safety and plagiarism guardrails",
                "RAG-based interactive Q&A system"
            ],
            Topics: ["NLP", "Multilingual AI", "Document Processing", "RAG"],
            Methodology: "Pipeline-based AI processing with modular service components"
        );

        sw.Stop();
        _logger.LogInformation("AI summarization complete in {Ms}ms", sw.ElapsedMilliseconds);
        return new StepResult<SummarizationResult>(true, result, ElapsedMs: sw.ElapsedMilliseconds);
    }
}
