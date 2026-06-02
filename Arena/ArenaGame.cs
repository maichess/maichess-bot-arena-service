using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Arena;

// A persisted game within a collection. Round/Pairing group games into
// tournament stages; they are 0 for matrix/single.
internal sealed class ArenaGame
{
    public string Id { get; set; } = string.Empty;

    public string CollectionId { get; set; } = string.Empty;

    public int Order { get; set; }

    public int Round { get; set; }

    public int Pairing { get; set; }

    public string MatchId { get; set; } = string.Empty;

    public string WhiteBotId { get; set; } = string.Empty;

    public string BlackBotId { get; set; } = string.Empty;

    public string Fen { get; set; } = string.Empty;

    public string FenLabel { get; set; } = string.Empty;

    // pending | running | finished
    public string Status { get; set; } = "pending";

    public GameOutcome Outcome { get; set; } = GameOutcome.Ongoing;

    public long WhiteTimeMs { get; set; }

    public long BlackTimeMs { get; set; }

    public string FinalFen { get; set; } = string.Empty;
}
