using Microsoft.Data.Sqlite;

namespace NavidGelato_Site.Services;

public sealed record OfflineMapTile(byte[] Data, bool IsGzip);

public sealed record OfflineMapStatus(
    bool Available,
    int MinZoom,
    int MaxZoom,
    string ArchiveName);

public sealed class OfflineMapTileService(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<OfflineMapTileService> logger)
{
    private const string DefaultArchiveName = "iran-shortbread-1.0.mbtiles";
    private readonly string _archivePath = ResolveArchivePath(environment, configuration);

    public string ArchiveName => Path.GetFileName(_archivePath);

    public bool IsAvailable => File.Exists(_archivePath);

    public async Task<OfflineMapStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            return new OfflineMapStatus(false, 0, 14, ArchiveName);

        try
        {
            var (minZoom, maxZoom) = await ReadZoomRangeAsync(cancellationToken);
            return new OfflineMapStatus(true, minZoom, maxZoom, ArchiveName);
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Offline map archive exists but could not be read.");
            return new OfflineMapStatus(false, 0, 14, ArchiveName);
        }
    }

    public async Task<OfflineMapTile?> GetTileAsync(
        int zoom,
        int x,
        int y,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || zoom < 0 || zoom > 22)
            return null;

        var axisSize = 1L << zoom;
        if (x < 0 || y < 0 || x >= axisSize || y >= axisSize)
            return null;

        // MBTiles stores tile rows using TMS (origin at bottom-left),
        // while MapLibre requests XYZ rows (origin at top-left).
        var tmsY = axisSize - 1L - y;

        try
        {
            await using var connection = new SqliteConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT tile_data
                FROM tiles
                WHERE zoom_level = $zoom
                  AND tile_column = $x
                  AND tile_row = $y
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("$zoom", zoom);
            command.Parameters.AddWithValue("$x", x);
            command.Parameters.AddWithValue("$y", tmsY);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is not byte[] bytes || bytes.Length == 0)
                return null;

            var isGzip = bytes.Length >= 2 && bytes[0] == 0x1f && bytes[1] == 0x8b;
            return new OfflineMapTile(bytes, isGzip);
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not read offline map tile z={Zoom}, x={X}, y={Y}.", zoom, x, y);
            return null;
        }
    }

    private async Task<(int MinZoom, int MaxZoom)> ReadZoomRangeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(BuildConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT name, value
            FROM metadata
            WHERE name IN ('minzoom', 'maxzoom');
            """;

        var minZoom = 0;
        var maxZoom = 14;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0);
            var value = reader.GetString(1);

            if (!int.TryParse(value, out var parsed))
                continue;

            if (name.Equals("minzoom", StringComparison.OrdinalIgnoreCase))
                minZoom = Math.Clamp(parsed, 0, 22);
            else if (name.Equals("maxzoom", StringComparison.OrdinalIgnoreCase))
                maxZoom = Math.Clamp(parsed, minZoom, 22);
        }

        return (minZoom, maxZoom);
    }

    private string BuildConnectionString()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _archivePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared
        };

        return builder.ToString();
    }

    private static string ResolveArchivePath(
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        var configured =
            Environment.GetEnvironmentVariable("NAVIDPRO_MAP_MBTILES")
            ?? configuration["OfflineMap:MbTilesPath"];

        if (string.IsNullOrWhiteSpace(configured))
            return Path.Combine(
                environment.ContentRootPath,
                "App_Data",
                "Maps",
                DefaultArchiveName);

        configured = configured.Trim();

        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
    }
}
