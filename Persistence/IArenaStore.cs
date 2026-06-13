using MaichessBotArenaService.Arena;

namespace MaichessBotArenaService.Persistence;

// Persistence boundary for the arena, backed by the arena-db database service.
// Kept as an interface so the orchestration and settings logic can be tested
// against a substitute.
internal interface IArenaStore
{
    // The stored global concurrency limit, or null when it has never been set.
    Task<int?> GetConcurrencyLimitAsync(CancellationToken ct);

    // Upserts the global concurrency limit.
    Task SetConcurrencyLimitAsync(int limit, CancellationToken ct);

    // Inserts a new collection and returns it with its assigned id.
    Task<ArenaCollection> InsertCollectionAsync(ArenaCollection collection, CancellationToken ct);

    // Persists changes to an existing collection.
    Task UpdateCollectionAsync(ArenaCollection collection, CancellationToken ct);

    // Returns a collection by id, or null when it does not exist.
    Task<ArenaCollection?> GetCollectionAsync(string id, CancellationToken ct);

    // Returns collections filtered by status (null = all), newest first.
    Task<IReadOnlyList<ArenaCollection>> ListCollectionsAsync(string? status, int limit, int offset, CancellationToken ct);

    // Inserts a new game and returns it with its assigned id.
    Task<ArenaGame> InsertGameAsync(ArenaGame game, CancellationToken ct);

    // Persists changes to an existing game.
    Task UpdateGameAsync(ArenaGame game, CancellationToken ct);

    // Returns all games of a collection in expansion order.
    Task<IReadOnlyList<ArenaGame>> ListGamesAsync(string collectionId, CancellationToken ct);

    // Counts games in the "running" state across all collections (the global
    // in-flight count for the concurrency cap).
    Task<int> CountRunningGamesAsync(CancellationToken ct);

    // Lists all games in the "running" state across all collections.
    Task<IReadOnlyList<ArenaGame>> ListRunningGamesAsync(CancellationToken ct);

    // Returns the running game for a match id, or null when the arena is not
    // running a game for it. Used by the match-completion consumer, which only
    // reacts to completions of games the arena itself spawned.
    Task<ArenaGame?> TryGetRunningGameAsync(string matchId, CancellationToken ct);
}
