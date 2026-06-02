namespace MaichessBotArenaService.Arena;

// The set of selectable bot ids, used to validate setup configs. Implemented
// over Engine's ListBots gRPC.
internal interface IBotCatalog
{
    Task<IReadOnlySet<string>> KnownBotIdsAsync(CancellationToken ct);
}
