using System.Diagnostics.CodeAnalysis;
using Maichess.Engine.V1;
using MaichessBotArenaService.Arena;

namespace MaichessBotArenaService.Clients;

// IBotCatalog over Engine's ListBots gRPC. Excluded from coverage: a thin adapter
// requiring a live engine.
[ExcludeFromCodeCoverage]
internal sealed class EngineBotCatalog(Bots.BotsClient engine) : IBotCatalog
{
    public async Task<IReadOnlySet<string>> KnownBotIdsAsync(CancellationToken ct)
    {
        ListBotsResponse response = await engine.ListBotsAsync(new ListBotsRequest(), cancellationToken: ct);
        return response.Bots.Select(bot => bot.Id).ToHashSet();
    }
}
