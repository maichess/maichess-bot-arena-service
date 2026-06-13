using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Persistence;

namespace MaichessBotArenaService.Tests.Support;

// In-memory IArenaStore so the orchestration can be driven through full
// lifecycles in tests.
internal sealed class FakeArenaStore : IArenaStore
{
    private readonly Dictionary<string, ArenaCollection> collections = [];
    private readonly Dictionary<string, ArenaGame> games = [];
    private int? limit;
    private int collectionSeq;
    private int gameSeq;

    public Task<int?> GetConcurrencyLimitAsync(CancellationToken ct) => Task.FromResult(limit);

    public Task SetConcurrencyLimitAsync(int value, CancellationToken ct)
    {
        limit = value;
        return Task.CompletedTask;
    }

    public Task<ArenaCollection> InsertCollectionAsync(ArenaCollection collection, CancellationToken ct)
    {
        collection.Id = $"collection-{++collectionSeq}";
        collections[collection.Id] = collection;
        return Task.FromResult(collection);
    }

    public Task UpdateCollectionAsync(ArenaCollection collection, CancellationToken ct)
    {
        collections[collection.Id] = collection;
        return Task.CompletedTask;
    }

    public Task<ArenaCollection?> GetCollectionAsync(string id, CancellationToken ct) =>
        Task.FromResult(collections.GetValueOrDefault(id));

    public Task<IReadOnlyList<ArenaCollection>> ListCollectionsAsync(string? status, int limitArg, int offset, CancellationToken ct)
    {
        IReadOnlyList<ArenaCollection> result =
        [
            .. collections.Values
                .Where(c => status is null || c.Status == status)
                .OrderByDescending(c => c.CreatedAtMs)
                .Skip(offset)
                .Take(limitArg),
        ];
        return Task.FromResult(result);
    }

    public Task<ArenaGame> InsertGameAsync(ArenaGame game, CancellationToken ct)
    {
        game.Id = $"game-{++gameSeq}";
        games[game.Id] = game;
        return Task.FromResult(game);
    }

    public Task UpdateGameAsync(ArenaGame game, CancellationToken ct)
    {
        games[game.Id] = game;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ArenaGame>> ListGamesAsync(string collectionId, CancellationToken ct)
    {
        IReadOnlyList<ArenaGame> result =
            [.. games.Values.Where(g => g.CollectionId == collectionId).OrderBy(g => g.Order)];
        return Task.FromResult(result);
    }

    public Task<int> CountRunningGamesAsync(CancellationToken ct) =>
        Task.FromResult(games.Values.Count(g => g.Status == "running"));

    public Task<IReadOnlyList<ArenaGame>> ListRunningGamesAsync(CancellationToken ct)
    {
        IReadOnlyList<ArenaGame> result = [.. games.Values.Where(g => g.Status == "running")];
        return Task.FromResult(result);
    }

    public Task<ArenaGame?> TryGetRunningGameAsync(string matchId, CancellationToken ct) =>
        Task.FromResult(games.Values.FirstOrDefault(g => g.MatchId == matchId && g.Status == "running"));
}
