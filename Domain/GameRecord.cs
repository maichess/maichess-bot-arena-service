namespace MaichessBotArenaService.Domain;

// A finished (or in-flight) game's data needed for stage scoring and tie-breaks.
// The clocks and final FEN feed the clock and material tie-breaks respectively.
internal sealed record GameRecord(
    string WhiteBotId,
    string BlackBotId,
    GameOutcome Outcome,
    long WhiteTimeMs,
    long BlackTimeMs,
    string FinalFen);
