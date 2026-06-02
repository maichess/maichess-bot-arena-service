using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Persistence;
using NSubstitute;

namespace MaichessBotArenaService.Tests.Support;

// Scenario-scoped state for the concurrency-limit setting steps.
internal sealed class SettingsContext
{
    internal IArenaStore Store { get; } = Substitute.For<IArenaStore>();

    internal ArenaSettingsService Service { get; }

    internal int LastReadLimit { get; set; }

    internal SetConcurrencyLimitResult? SetResult { get; set; }

    internal SettingsContext()
    {
        Service = new ArenaSettingsService(Store);
        Store.GetConcurrencyLimitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<int?>(null));
    }
}
