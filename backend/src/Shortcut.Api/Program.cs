using Shortcut.Api.Analyses;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IPhotoAnalysisService, HeuristicPhotoAnalysisService>();
builder.Services.AddSingleton<IAnalysisRepository>(services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("ShortcutDb");
    return string.IsNullOrWhiteSpace(connectionString)
        ? new InMemoryAnalysisRepository()
        : new PostgresAnalysisRepository(connectionString);
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/analyses", async (
    IFormFile? photo,
    IPhotoAnalysisService analysisService,
    IAnalysisRepository repository,
    CancellationToken cancellationToken) =>
{
    if (photo is null || photo.Length == 0)
    {
        return Results.BadRequest("Upload a photograph to analyze.");
    }

    if (!ImageUploadValidator.IsSupported(photo))
    {
        return Results.BadRequest("Only JPG, PNG, WebP, or TIFF image uploads are supported.");
    }

    await using var stream = photo.OpenReadStream();
    var response = await analysisService.AnalyzeAsync(photo.FileName, photo.ContentType, stream, cancellationToken);
    await repository.SaveAsync(response, cancellationToken);

    return Results.Ok(response);
}).DisableAntiforgery();

app.MapGet("/api/analyses", async (IAnalysisRepository repository, CancellationToken cancellationToken) =>
{
    var analyses = await repository.ListRecentAsync(cancellationToken);
    return Results.Ok(analyses);
});

app.Run();

public partial class Program;
