namespace MaichessBotArenaService.Domain;

// Result of a single arena game. Deliberately a 1:1 mirror of match-manager's
// MatchStatus (ONGOING / WHITE_WON / BLACK_WON / DRAW) so every match state maps
// without ambiguity. Note there is no dedicated stalemate state: a stalemate —
// like the fifty-move rule, threefold repetition, and insufficient material — is
// a DRAW. The distinction (the EndReason) is not needed for arena scoring, where
// every draw counts as half a point to each side.
internal enum GameOutcome
{
    // The game has not finished yet (MatchStatus.ONGOING / UNSPECIFIED).
    Ongoing,

    // White delivered checkmate or won on time/resignation (MatchStatus.WHITE_WON).
    WhiteWon,

    // Black won by the same means (MatchStatus.BLACK_WON).
    BlackWon,

    // Any drawn result — stalemate, fifty-move, threefold, insufficient material,
    // or agreement (MatchStatus.DRAW).
    Draw,
}
