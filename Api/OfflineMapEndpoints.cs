using NavidGelato_Site.Services;

namespace NavidGelato_Site.Api;

public static class OfflineMapEndpoints
{
    public static IEndpointRouteBuilder MapOfflineMapApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/map/offline/status", GetStatusAsync)
            .ExcludeFromDescription();

        endpoints.MapGet("/map/offline/tiles/{z:int}/{x:int}/{y:int}.pbf", GetTileAsync)
            .ExcludeFromDescription();

        return endpoints;
    }

    private static async Task<IResult> GetStatusAsync(
        OfflineMapTileService tileService,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        response.Headers["Cache-Control"] = "no-store";
        var status = await tileService.GetStatusAsync(cancellationToken);

        return Results.Json(new
        {
            available = status.Available,
            minZoom = status.MinZoom,
            maxZoom = status.MaxZoom,
            archive = status.ArchiveName
        });
    }

    private static async Task<IResult> GetTileAsync(
        int z,
        int x,
        int y,
        OfflineMapTileService tileService,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var tile = await tileService.GetTileAsync(z, x, y, cancellationToken);
        if (tile is null)
            return Results.NotFound();

        response.Headers["Cache-Control"] = "public,max-age=86400,immutable";
        response.Headers["X-Content-Type-Options"] = "nosniff";

        if (tile.IsGzip)
            response.Headers["Content-Encoding"] = "gzip";

        return Results.Bytes(tile.Data, "application/vnd.mapbox-vector-tile");
    }
}
