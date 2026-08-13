namespace Shortcut.Api.Analyses;

public interface IPhotoAnalysisService
{
    Task<AnalysisResponse> AnalyzeAsync(
        string fileName,
        string contentType,
        Stream photo,
        CancellationToken cancellationToken);
}
