namespace MaichessBotArenaService.Domain;

// One concrete game produced by expanding a setup: who plays which color, from
// which start position, and its 0-based position in the ordered game list.
internal sealed record ExpandedGame(
    int Order, string WhiteBotId, string BlackBotId, string Fen, string FenLabel);
