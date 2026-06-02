using MaichessBotArenaService.Persistence;

namespace MaichessBotArenaService.Arena;

// The global concurrency limit: shared by all users, defaulted until set, and
// never below 1.
internal sealed class ArenaSettingsService(IArenaStore store)
{
    internal const int DefaultConcurrencyLimit = 4;

    internal async Task<int> GetConcurrencyLimitAsync(CancellationToken ct)
    {
        int? stored = await store.GetConcurrencyLimitAsync(ct);
        return stored ?? DefaultConcurrencyLimit;
    }

    internal async Task<SetConcurrencyLimitResult> SetConcurrencyLimitAsync(int limit, CancellationToken ct)
    {
        if (limit < 1)
        {
            return new SetConcurrencyLimitResult.InvalidInput("limit must be at least 1");
        }

        await store.SetConcurrencyLimitAsync(limit, ct);
        return new SetConcurrencyLimitResult.Success(limit);
    }
}
