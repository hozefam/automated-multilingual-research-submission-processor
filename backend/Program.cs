using Backend.Agents;
using Backend.Endpoints;
using Backend.Pipeline;
using Backend.Plugins;
using Backend.Storage;
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

// ── SK Plugins (registered in DI so Kernel can resolve them) ────────────────
builder.Services.AddSingleton<OcrPlugin>();

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

    // Volatile (in-memory) vector store:
    // TODO (SK sprint): builder.Services.AddVolatileVectorStore();
    //   Requires resolving correct extension package. RagAgent/QnAAgent will consume IVectorStore.

    // Register SK plugins
    kBuilder.Plugins.AddFromObject(sp.GetRequiredService<OcrPlugin>(), "OcrPlugin");

    return kBuilder.Build();
});

// ── Document Store (singleton in-memory persistence) ────────────────────────────────────
// Stores pipeline results, audit log and HITL corrections for the lifetime of the process.
// TODO: Replace with EF Core + SQLite or Azure Cosmos DB for durable persistence.
builder.Services.AddSingleton<IDocumentStore, DocumentStore>();

// ── Agent Services (11 pipeline agents) ───────────────────────────────────────────────
builder.Services.AddScoped<IIngestionAgent, IngestionAgent>();
builder.Services.AddScoped<IPreProcessAgent, PreProcessAgent>();
builder.Services.AddScoped<ITranslationAgent, TranslationAgent>();
builder.Services.AddScoped<IExtractionAgent, ExtractionAgent>();
builder.Services.AddScoped<IValidationAgent, ValidationAgent>();
builder.Services.AddScoped<IContentSafetyAgent, ContentSafetyAgent>();
builder.Services.AddScoped<IPlagiarismDetectionAgent, PlagiarismDetectionAgent>();
builder.Services.AddScoped<IRagAgent, RagAgent>();
builder.Services.AddScoped<ISummaryAgent, SummaryAgent>();
builder.Services.AddScoped<IQnAAgent, QnAAgent>();
builder.Services.AddScoped<IHumanFeedbackAgent, HumanFeedbackAgent>();
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

