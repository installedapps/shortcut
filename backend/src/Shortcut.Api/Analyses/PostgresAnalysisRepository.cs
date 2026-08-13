using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace Shortcut.Api.Analyses;

public sealed class PostgresAnalysisRepository(string connectionString) : IAnalysisRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(AnalysisResponse analysis, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            insert into analyses (id, file_name, created_at, summary, lightroom_settings, darktable_settings)
            values (@id, @file_name, @created_at, @summary, @lightroom_settings, @darktable_settings)
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", analysis.Id);
        command.Parameters.AddWithValue("file_name", analysis.FileName);
        command.Parameters.AddWithValue("created_at", analysis.CreatedAt);
        command.Parameters.AddWithValue("summary", analysis.Summary);
        command.Parameters.Add("lightroom_settings", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(analysis.LightroomSettings, JsonOptions);
        command.Parameters.Add("darktable_settings", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(analysis.DarktableSettings, JsonOptions);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AnalysisResponse>> ListRecentAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            select id, file_name, created_at, summary, lightroom_settings, darktable_settings
            from analyses
            order by created_at desc
            limit 20
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var analyses = new List<AnalysisResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var lightroomSettings = JsonSerializer.Deserialize<IReadOnlyList<EditSetting>>(reader.GetString(4), JsonOptions) ?? [];
            var darktableSettings = JsonSerializer.Deserialize<IReadOnlyList<EditSetting>>(reader.GetString(5), JsonOptions) ?? [];
            analyses.Add(new AnalysisResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetString(3),
                lightroomSettings,
                darktableSettings));
        }

        return analyses;
    }
}
