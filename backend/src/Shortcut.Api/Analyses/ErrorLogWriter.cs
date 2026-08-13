using System.Text;

namespace Shortcut.Api.Analyses;

public sealed class ErrorLogWriter(IConfiguration configuration, IWebHostEnvironment environment)
{
    private readonly string _errorDirectory = configuration["LogFiles:ErrorDirectory"]
        ?? Path.Combine(environment.ContentRootPath, "logs", "errors");

    public async Task WriteAsync(
        string errorType,
        string message,
        IFormFile? photo,
        HttpContext httpContext,
        Exception? exception = null)
    {
        Directory.CreateDirectory(_errorDirectory);

        var timestamp = DateTimeOffset.UtcNow;
        var fileName = $"{timestamp:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.log";
        var path = Path.Combine(_errorDirectory, fileName);

        var builder = new StringBuilder()
            .AppendLine($"timestamp_utc: {timestamp:O}")
            .AppendLine($"trace_id: {httpContext.TraceIdentifier}")
            .AppendLine($"error_type: {errorType}")
            .AppendLine($"message: {message}")
            .AppendLine($"method: {httpContext.Request.Method}")
            .AppendLine($"path: {httpContext.Request.Path}")
            .AppendLine($"remote_ip: {httpContext.Connection.RemoteIpAddress}")
            .AppendLine($"file_name: {photo?.FileName ?? "(none)"}")
            .AppendLine($"content_type: {photo?.ContentType ?? "(none)"}")
            .AppendLine($"file_size_bytes: {photo?.Length.ToString() ?? "(none)"}");

        if (exception is not null)
        {
            builder
                .AppendLine($"exception_type: {exception.GetType().FullName}")
                .AppendLine($"exception_message: {exception.Message}")
                .AppendLine("stack_trace:")
                .AppendLine(exception.StackTrace ?? "(none)");
        }

        await File.WriteAllTextAsync(path, builder.ToString());
    }
}
