using Backend.Endpoints;
using Backend.Pipeline;
using Backend.Services;
using Backend.Services.MetadataExtraction;
#pragma warning disable SKEXP0001   // SK Memory APIs are experimental
#pragma warning disable SKEXP0050   // VolatileMemoryStore is for evaluation purposes

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
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
//
//  ④ Filters with Logging – registered as singleton, added to the kernel below
builder.Services.AddSingleton<MetadataLoggingFilter>();

//  ⑤ Memory – VolatileMemoryStore (in-process, no external store required)
//     Keys are SHA-256 hashes so GetAsync (exact lookup) needs no embedding model.
builder.Services.AddSingleton<IMemoryStore, VolatileMemoryStore>();

//  ①②③ Kernel – configures Native Functions, Semantic Functions, Plugins + attaches Filter
builder.Services.AddSingleton<Kernel>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var filter = sp.GetRequiredService<MetadataLoggingFilter>();
    var logger = sp.GetRequiredService<ILogger<MetadataExtractionPlugin>>();

    var kBuilder = Kernel.CreateBuilder();

    // Azure OpenAI chat completion (drives Semantic Function + Agent)
    kBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: config["AzureOpenAI:ChatDeployment"]
            ?? throw new InvalidOperationException("AzureOpenAI:ChatDeployment is not configured."),
        endpoint: config["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured."),
        apiKey: config["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured."));

    var kernel = kBuilder.Build();

    // ③ Plugin – "MetadataExtraction": wraps Native Functions (Azure Doc Intelligence calls)
    var plugin = new MetadataExtractionPlugin(config, logger);
    kernel.Plugins.AddFromObject(plugin, "MetadataExtraction");

    // ② Semantic Function – registered as "MetadataPrompts.ExtractMetadataFromText"
    //   The prompt template sends raw document text to GPT-4o and returns metadata JSON.
    var semanticFn = KernelFunctionFactory.CreateFromPrompt(
        promptTemplate: MetadataExtractionPlugin.ExtractionPromptTemplate,
        functionName: "ExtractMetadataFromText",
        description: "Extracts structured metadata JSON (title, authors, abstract, keywords) from raw research paper text using AI.");

    kernel.Plugins.AddFromFunctions("MetadataPrompts", [semanticFn]);

    // ④ Filters – MetadataLoggingFilter intercepts every function invocation + prompt render
    kernel.FunctionInvocationFilters.Add(filter);
    kernel.PromptRenderFilters.Add(filter);

    return kernel;
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

