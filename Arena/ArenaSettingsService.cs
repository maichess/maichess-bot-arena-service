using MaichessBotArenaService.Persistence;
using Microsoft.Extensions.Caching.Memory;

namespace MaichessBotArenaService.Arena;

// The global concurrency limit: shared by all users, defaulted until set, and
// never below 1.
internal sealed class ArenaSettingsService(IArenaStore store, IMemoryCache cache)
{
    internal const int DefaultConcurrencyLimit = 4;
    private const string CacheKey = "arena:concurrency_limit";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    internal async Task<int> GetConcurrencyLimitAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out int cached))
        {
            return cached;
        }

        int? stored = await store.GetConcurrencyLimitAsync(ct);
        int limit = stored ?? DefaultConcurrencyLimit;
        cache.Set(CacheKey, limit, CacheTtl);
        return limit;
    }

    internal async Task<SetConcurrencyLimitResult> SetConcurrencyLimitAsync(int limit, CancellationToken ct)
    {
        if (limit < 1)
        {
            return new SetConcurrencyLimitResult.InvalidInput("limit must be at least 1");
        }

        await store.SetConcurrencyLimitAsync(limit, ct);
        cache.Set(CacheKey, limit, CacheTtl);
        return new SetConcurrencyLimitResult.Success(limit);
    }
}
