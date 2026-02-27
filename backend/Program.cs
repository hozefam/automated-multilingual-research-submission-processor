using Backend.Endpoints;
using Backend.Pipeline;
using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
.WithTags("Health");

// Document pipeline endpoints
app.MapDocumentEndpoints();

app.Run();

