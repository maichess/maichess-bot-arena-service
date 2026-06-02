namespace MaichessBotArenaService.Domain;

// Points scoring for arena games. Every method assumes the bot participates in
// the games it is scored against (the caller filters to the relevant pairing).
internal static class Scoring
{
    // Points for a bot in one game: 1 for a win, 0.5 for a draw, 0 for a loss or
    // an unfinished game.
    internal static double Points(GameRecord game, string botId) => game.Outcome switch
    {
        GameOutcome.Draw => 0.5,
        GameOutcome.WhiteWon => game.WhiteBotId == botId ? 1.0 : 0.0,
        GameOutcome.BlackWon => game.BlackBotId == botId ? 1.0 : 0.0,
        _ => 0.0,
    };

    // Total points for a bot across the given games.
    internal static double Total(IEnumerable<GameRecord> games, string botId) =>
        games.Sum(game => Points(game, botId));

    // Aggregate (scoreA, scoreB) for a pairing across the given games.
    internal static (double ScoreA, double ScoreB) PairScore(
        IReadOnlyList<GameRecord> games, string botA, string botB) =>
        (Total(games, botA), Total(games, botB));
}
