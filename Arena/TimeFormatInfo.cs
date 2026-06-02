namespace MaichessBotArenaService.Arena;

// A resolved time-format preset. Mirrors match-manager's TimeFormat but stays a
// plain service-layer record.
internal sealed record TimeFormatInfo(string Id, long BaseMs, long IncrementMs, string Category);
