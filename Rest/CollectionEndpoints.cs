using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MaichessBotArenaService.Arena;
using Microsoft.AspNetCore.Mvc;

namespace MaichessBotArenaService.Rest;

[ExcludeFromCodeCoverage]
internal static class CollectionEndpoints
{
    internal static IEndpointRouteBuilder MapCollectionEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/collections").RequireAuthorization();
        group.MapPost("/", Create);
        group.MapGet("/", List);
        group.MapGet("/{id}", Get);
        return routes;
    }

    private static async Task<IResult> Create(
        [FromBody] CollectionRequests.CreateCollection body,
        ClaimsPrincipal principal,
        CollectionService service,
        ResultViewBuilder viewBuilder,
        CancellationToken ct)
    {
        if (!TryGetUserId(principal, out string userId))
        {
            return Results.Unauthorized();
        }

        CreateCollectionCommand? command = BuildCommand(body, userId);
        if (command is null)
        {
            return Results.BadRequest(new ErrorResponse("exactly one of tournament, matrix, single is required"));
        }

        CreateCollectionResult result = await service.CreateAsync(command, ct);
        if (result is CreateCollectionResult.InvalidInput invalid)
        {
            return Results.BadRequest(new ErrorResponse(invalid.Message));
        }

        string id = ((CreateCollectionResult.Success)result).Collection.Id;
        (ArenaCollection collection, IReadOnlyList<ArenaGame> games) = (await service.GetWithGamesAsync(id, ct))!.Value;
        return Results.Created($"/collections/{id}", viewBuilder.BuildDetail(collection, games));
    }

    private static async Task<IResult> List(
        [FromQuery] string? status,
        [FromQuery] int limit,
        [FromQuery] int offset,
        CollectionService service,
        CancellationToken ct)
    {
        IReadOnlyList<ArenaCollection> collections =
            await service.ListAsync(string.IsNullOrEmpty(status) ? null : status, limit, offset, ct);

        List<CollectionViews.Summary> summaries = [];
        foreach (ArenaCollection collection in collections)
        {
            (ArenaCollection c, IReadOnlyList<ArenaGame> games) = (await service.GetWithGamesAsync(collection.Id, ct))!.Value;
            summaries.Add(ResultViewBuilder.BuildSummary(c, games));
        }

        return Results.Ok(new CollectionListResponse(summaries));
    }

    private static async Task<IResult> Get(
        string id, CollectionService service, ResultViewBuilder viewBuilder, CancellationToken ct)
    {
        (ArenaCollection Collection, IReadOnlyList<ArenaGame> Games)? found = await service.GetWithGamesAsync(id, ct);
        return found is null
            ? Results.NotFound()
            : Results.Ok(viewBuilder.BuildDetail(found.Value.Collection, found.Value.Games));
    }

    private static CreateCollectionCommand? BuildCommand(CollectionRequests.CreateCollection body, string userId)
    {
        int configs = (body.Tournament is null ? 0 : 1) + (body.Matrix is null ? 0 : 1) + (body.Single is null ? 0 : 1);
        if (configs != 1)
        {
            return null;
        }

        string name = body.Name ?? string.Empty;

        if (body.Tournament is { } tournament)
        {
            return new CreateCollectionCommand(
                name,
                userId,
                SetupKind.Tournament,
                tournament.BotIds ?? [],
                string.Empty,
                string.Empty,
                tournament.FenList ?? [],
                0,
                tournament.FensPerStage,
                tournament.ColorMode == "random" ? TournamentColorMode.Random : TournamentColorMode.BothColors,
                false,
                tournament.TimeFormatId ?? string.Empty);
        }

        if (body.Matrix is { } matrix)
        {
            return new CreateCollectionCommand(
                name,
                userId,
                SetupKind.Matrix,
                matrix.BotIds ?? [],
                string.Empty,
                string.Empty,
                matrix.FenList ?? [],
                matrix.GamesPerFen,
                0,
                TournamentColorMode.BothColors,
                false,
                matrix.TimeFormatId ?? string.Empty);
        }

        CollectionRequests.Single single = body.Single!;
        return new CreateCollectionCommand(
            name,
            userId,
            SetupKind.Single,
            [],
            single.WhiteBotId ?? string.Empty,
            single.BlackBotId ?? string.Empty,
            single.FenList ?? [],
            single.GamesPerFen,
            0,
            TournamentColorMode.BothColors,
            single.KeepSwitchingColors,
            single.TimeFormatId ?? string.Empty);
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out string userId)
    {
        string? value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        userId = value ?? string.Empty;
        return value is not null;
    }
}
