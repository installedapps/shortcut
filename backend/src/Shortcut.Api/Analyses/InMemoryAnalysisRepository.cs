namespace Shortcut.Api.Analyses;

public sealed class InMemoryAnalysisRepository : IAnalysisRepository
{
    private readonly List<AnalysisResponse> analyses = [];
    private readonly object syncRoot = new();

    public Task SaveAsync(AnalysisResponse analysis, CancellationToken cancellationToken)
    {
        lock (syncRoot)
        {
            analyses.Insert(0, analysis);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AnalysisResponse>> ListRecentAsync(CancellationToken cancellationToken)
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<AnalysisResponse>>(analyses.Take(20).ToList());
        }
    }
}
