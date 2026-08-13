namespace Shortcut.Api.Analyses;

public static class AnalysisEndpoints
{
    public static void MapAnalysisEndpoints(this WebApplication app)
    {
        app.MapPost("/api/analyses", AnalyzeAsync).DisableAntiforgery();
        app.MapGet("/api/analyses", ListRecentAsync);
    }

    private static async Task<IResult> AnalyzeAsync(
        IFormFile? photo,
        IPhotoUploadValidator uploadValidator,
        IPhotoAnalysisService analysisService,
        IAnalysisRepository repository,
        ErrorLogWriter errorLogWriter,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await uploadValidator.ValidateAsync(photo, cancellationToken);
        if (!validation.IsValid)
        {
            await errorLogWriter.WriteAsync(validation.ErrorType, validation.Message, photo, httpContext);
            return Results.BadRequest(validation.Message);
        }

        try
        {
            await using var stream = photo!.OpenReadStream();
            var response = await analysisService.AnalyzeAsync(photo.FileName, photo.ContentType, stream, cancellationToken);
            await repository.SaveAsync(response, cancellationToken);
            return Results.Ok(response);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            const string message = "Kimi analysis timed out before settings were returned. Try a smaller exported image, or run the request again.";
            await errorLogWriter.WriteAsync("kimi_timeout", message, photo, httpContext, exception);
            return Results.Problem(message, statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (HttpRequestException exception)
        {
            var message = $"Kimi analysis could not reach the API: {exception.Message}";
            await errorLogWriter.WriteAsync("kimi_network_error", message, photo, httpContext, exception);
            return Results.Problem(message, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (InvalidOperationException exception) when (exception.Message.StartsWith("Kimi ", StringComparison.Ordinal))
        {
            await errorLogWriter.WriteAsync("kimi_api_error", exception.Message, photo, httpContext, exception);
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception exception)
        {
            const string message = "Shortcut could not complete photo analysis because of an unexpected server error.";
            await errorLogWriter.WriteAsync("unexpected_analysis_error", message, photo, httpContext, exception);
            return Results.Problem(message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ListRecentAsync(
        IAnalysisRepository repository,
        CancellationToken cancellationToken)
    {
        var analyses = await repository.ListRecentAsync(cancellationToken);
        return Results.Ok(analyses);
    }
}
