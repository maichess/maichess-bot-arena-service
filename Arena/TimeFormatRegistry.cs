namespace MaichessBotArenaService.Arena;

// Canonical time-format presets, mirroring match-maker's GET /time-formats.
internal static class TimeFormatRegistry
{
    private static readonly IReadOnlyList<TimeFormatInfo> Presets =
    [
        new TimeFormatInfo("1+0", 60_000, 0, "bullet"),
        new TimeFormatInfo("2+1", 120_000, 1_000, "bullet"),
        new TimeFormatInfo("3+0", 180_000, 0, "blitz"),
        new TimeFormatInfo("3+2", 180_000, 2_000, "blitz"),
        new TimeFormatInfo("5+0", 300_000, 0, "blitz"),
        new TimeFormatInfo("5+3", 300_000, 3_000, "blitz"),
        new TimeFormatInfo("10+0", 600_000, 0, "rapid"),
        new TimeFormatInfo("10+5", 600_000, 5_000, "rapid"),
        new TimeFormatInfo("15+10", 900_000, 10_000, "rapid"),
        new TimeFormatInfo("30+0", 1_800_000, 0, "classical"),
        new TimeFormatInfo("30+20", 1_800_000, 20_000, "classical"),
    ];

    internal static bool IsKnown(string id) => Presets.Any(preset => preset.Id == id);

    internal static TimeFormatInfo Resolve(string id) => Presets.First(preset => preset.Id == id);
}
