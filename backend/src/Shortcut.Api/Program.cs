using Shortcut.Api.Analyses;

var builder = WebApplication.CreateBuilder(args);

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

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapAnalysisEndpoints();

app.Run();

public partial class Program;
