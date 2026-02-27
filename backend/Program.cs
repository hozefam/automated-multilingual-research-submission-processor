using Backend.Endpoints;
using Backend.Pipeline;
using Backend.Services;
using Microsoft.SemanticKernel;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title = "AMRSP – Automated Multilingual Research Submission Processor",
            Version = "v1",
            Description = """
                REST API for the AMRSP platform.  
                Upload a PDF research paper and run it through a 7-step AI pipeline:
                metadata extraction → language detection → content safety →
                plagiarism detection → RAG indexing → AI summarization → Q&A.
                """,
            Contact = new() { Name = "AMRSP Team" }
        };
        return Task.CompletedTask;
    });
});

// Allow Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Semantic Kernel ───────────────────────────────────────────────────────────
// Kernel – registered as singleton; pipeline services can inject it later.
builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var kBuilder = Kernel.CreateBuilder();

    // Azure OpenAI chat completion – drives summarization, Q&A and metadata extraction
    kBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: config["AzureOpenAI:ChatDeployment"]
            ?? throw new InvalidOperationException("AzureOpenAI:ChatDeployment is not configured."),
        endpoint: config["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured."),
        apiKey: config["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured."));

    return kBuilder.Build();
});

// ── Pipeline Services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IMetadataExtractor, MetadataExtractor>();
builder.Services.AddScoped<ILanguageDetector, LanguageDetector>();
builder.Services.AddScoped<IContentSafetyChecker, ContentSafetyChecker>();
builder.Services.AddScoped<IPlagiarismDetector, PlagiarismDetector>();
builder.Services.AddScoped<IRagIndexer, RagIndexer>();
builder.Services.AddScoped<IAiSummarizer, AiSummarizer>();
builder.Services.AddScoped<IQnAService, QnAService>();
builder.Services.AddScoped<DocumentPipelineOrchestrator>();

// Required for IFormFile in Minimal APIs
builder.Services.AddAntiforgery();

var app = builder.Build();

// Configure the HTTP request pipeline.
// OpenAPI JSON spec + Scalar interactive UI (available in all environments)
app.MapOpenApi();  // → /openapi/v1.json
app.MapScalarApiReference(options =>
{
    options.Title = "AMRSP API";
    options.Theme = ScalarTheme.DeepSpace;
    options.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.HttpClient);
    options.DefaultOpenAllTags = true;  // expand all tag groups on load
});  // → /scalar/v1

app.UseCors("AllowAngular");
app.UseHttpsRedirection();

// Health / version endpoint
app.MapGet("/api/health", () =>
    Results.Ok(new
    {
        version = "1.0.0",
        status = "healthy",
        timestamp = DateTime.UtcNow
    }))
.WithName("GetHealth")
.WithTags("Health")
.WithSummary("API health check")
.WithDescription("Returns the current API version, health status and UTC timestamp. Used by the frontend status badge.");

// Document pipeline endpoints
app.MapDocumentEndpoints();

app.Run();

