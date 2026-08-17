namespace Shortcut.Api.Analyses;

public static class AnalysisServiceCollectionExtensions
{
    public static IServiceCollection AddAnalysisServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSingleton<ErrorLogWriter>();
        services.AddSingleton<IPhotoUploadValidator, PhotoUploadValidator>();
        services.AddPhotoAnalysisService(configuration);
        services.AddSingleton<IAnalysisRepository>(_ => CreateRepository(configuration));
        return services;
    }

    private static void AddPhotoAnalysisService(this IServiceCollection services, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(KimiConfiguration.ReadApiKey(configuration)))
        {
            services.AddSingleton(new AnalysisRuntimeInfo("heuristic"));
            services.AddSingleton<IPhotoAnalysisService, HeuristicPhotoAnalysisService>();
            return;
        }

        services.AddSingleton(new AnalysisRuntimeInfo("kimi"));
        services.AddSingleton(new KimiAnalysisRequestFactory(configuration["Kimi:Model"] ?? "kimi-k2.6"));
        services.AddHttpClient<IPhotoAnalysisService, KimiPhotoAnalysisService>(client =>
        {
            var baseUrl = configuration["Kimi:BaseUrl"] ?? "https://api.moonshot.ai/v1/";
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/");
            client.Timeout = TimeSpan.FromMinutes(5);
        });
    }

    private static IAnalysisRepository CreateRepository(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ShortcutDb");
        return string.IsNullOrWhiteSpace(connectionString)
            ? new InMemoryAnalysisRepository()
            : new PostgresAnalysisRepository(connectionString);
    }
}
