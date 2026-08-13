namespace Shortcut.Api.Analyses;

public interface IAnalysisRepository
{
    Task SaveAsync(AnalysisResponse analysis, CancellationToken cancellationToken);

    Task<IReadOnlyList<AnalysisResponse>> ListRecentAsync(CancellationToken cancellationToken);
}
