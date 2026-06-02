namespace MaichessBotArenaService.Domain;

// Decides how many additional arena games may start right now under the global
// concurrency limit. This is the pure scheduling decision; the loop that acts on
// it lives in the orchestration layer.
internal static class LaunchPlanner
{
    // Spare capacity below the limit, clamped to what is actually waiting and
    // never negative.
    internal static int LaunchableCount(int concurrencyLimit, int runningCount, int pendingCount)
    {
        int spare = concurrencyLimit - runningCount;
        return spare <= 0 ? 0 : Math.Min(spare, pendingCount);
    }
}
