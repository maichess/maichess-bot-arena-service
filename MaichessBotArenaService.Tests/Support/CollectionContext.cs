using MaichessBotArenaService.Arena;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace MaichessBotArenaService.Tests.Support;

// Scenario-scoped wiring for the collection-lifecycle steps: an in-memory store,
// fake launcher/catalog, deterministic randomness, and a fixed clock.
internal sealed class CollectionContext
{
    internal FakeArenaStore Store { get; } = new();

    internal FakeGameLauncher Launcher { get; } = new();

    internal FakeBotCatalog Catalog { get; } = new();

    internal CollectionService Service { get; }

    internal long Now { get; set; } = 1000;

    internal CreateCollectionResult? CreateResult { get; set; }

    internal IReadOnlyList<ArenaCollection>? Listed { get; set; }

    internal (ArenaCollection Collection, IReadOnlyList<ArenaGame> Games)? Fetched { get; set; }

    // Ids of the collections created in the scenario, in creation order, so steps
    // can refer to "the first / second setup" when several exist.
    internal List<string> CreatedIds { get; } = [];

    internal CollectionContext()
    {
        IMemoryCache cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        ArenaSettingsService settings = new(Store, cache);
        Service = new CollectionService(Store, Launcher, Catalog, settings, new FakeArenaRandomProvider(), () => Now);
    }

    internal string CollectionId =>
        Assert.IsType<CreateCollectionResult.Success>(CreateResult).Collection.Id;
}
