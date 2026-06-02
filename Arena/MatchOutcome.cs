using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Arena;

// A finished match's result as read from Match Manager: the outcome plus the
// final clocks and position used for tie-breaks.
internal sealed record MatchOutcome(GameOutcome Outcome, long WhiteTimeMs, long BlackTimeMs, string FinalFen);
