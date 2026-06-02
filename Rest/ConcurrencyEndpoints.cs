using System.Diagnostics.CodeAnalysis;
using MaichessBotArenaService.Arena;
using Microsoft.AspNetCore.Mvc;

namespace MaichessBotArenaService.Rest;

[ExcludeFromCodeCoverage]
internal static class ConcurrencyEndpoints
{
    internal static IEndpointRouteBuilder MapConcurrencyEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/concurrency-limit").RequireAuthorization();
        group.MapGet("/", GetLimit);
        group.MapPut("/", SetLimit);
        return routes;
    }

    private static async Task<IResult> GetLimit(ArenaSettingsService service, CancellationToken ct)
    {
        int limit = await service.GetConcurrencyLimitAsync(ct);
        return Results.Ok(new ConcurrencyResponse(limit));
    }

    private static async Task<IResult> SetLimit(
        [FromBody] SetConcurrencyRequest body, ArenaSettingsService service, CancellationToken ct)
    {
        SetConcurrencyLimitResult result = await service.SetConcurrencyLimitAsync(body.Limit, ct);
        return result switch
        {
            SetConcurrencyLimitResult.Success ok => Results.Ok(new ConcurrencyResponse(ok.Limit)),
            SetConcurrencyLimitResult.InvalidInput err => Results.BadRequest(new ErrorResponse(err.Message)),
            _ => Results.Problem(),
        };
    }
}
