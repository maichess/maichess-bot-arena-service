namespace MaichessBotArenaService.Arena;

// A persisted setup. The resolved config is stored flat; tournament bracket
// state is derived from Seeded plus the games, never stored.
internal sealed class ArenaCollection
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public SetupKind Kind { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public string Status { get; set; } = "pending";

    public long CreatedAtMs { get; set; }

    public long FinishedAtMs { get; set; }

    public IReadOnlyList<string> BotIds { get; set; } = [];

    public string WhiteBotId { get; set; } = string.Empty;

    public string BlackBotId { get; set; } = string.Empty;

    public IReadOnlyList<string> FenList { get; set; } = [];

    public int GamesPerFen { get; set; }

    public int FensPerStage { get; set; }

    public TournamentColorMode ColorMode { get; set; }

    public MatrixColorMode MatrixColorMode { get; set; }

    public bool KeepSwitchingColors { get; set; }

    public TimeFormatInfo TimeFormat { get; set; } = new(string.Empty, 0, 0, string.Empty);

    // Random tournament seeding order; empty for matrix/single.
    public IReadOnlyList<string> Seeded { get; set; } = [];

    // Final tournament champion; empty until finished.
    public string WinnerBotId { get; set; } = string.Empty;
}
