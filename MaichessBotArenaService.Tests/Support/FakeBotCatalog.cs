using MaichessBotArenaService.Arena;

namespace MaichessBotArenaService.Tests.Support;

internal sealed class FakeBotCatalog : IBotCatalog
{
    internal HashSet<string> Bots { get; } = [];

    public Task<IReadOnlySet<string>> KnownBotIdsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlySet<string>>(Bots);
}
