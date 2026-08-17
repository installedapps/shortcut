using Shortcut.Api.Analyses;

var builder = WebApplication.CreateBuilder(args);

// dotnet run starts as Production without a launch profile, so load local
// project secrets explicitly for API keys configured with dotnet user-secrets.
builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services.AddAnalysisServices(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/api/health", (AnalysisRuntimeInfo analysis) => Results.Ok(new
{
    status = "ok",
    analysisProvider = analysis.PhotoAnalysisProvider
}));
app.MapAnalysisEndpoints();

app.Run();

public partial class Program;
