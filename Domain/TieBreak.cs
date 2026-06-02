namespace MaichessBotArenaService.Domain;

// Decides the winner of a tournament stage between two bots from the stage's
// finished games, applying the documented tie-break ladder in order:
//   1. more wins
//   2. fewer rounds needed to win (earlier last win)
//   3. greater aggregate remaining-clock advantage
//   4. greater aggregate final material advantage
//   5. a seeded coin flip (guarantees a decisive winner so the bracket advances)
internal static class TieBreak
{
    internal static string DecideWinner(
        string botA, string botB, IReadOnlyList<GameRecord> games, IArenaRandom random)
    {
        int winsA = CountWins(games, botA);
        int winsB = CountWins(games, botB);
        if (winsA != winsB)
        {
            return winsA > winsB ? botA : botB;
        }

        int roundsA = LastWinRound(games, botA);
        int roundsB = LastWinRound(games, botB);
        if (roundsA != roundsB)
        {
            return roundsA < roundsB ? botA : botB;
        }

        long clockA = ClockAdvantage(games, botA);
        long clockB = ClockAdvantage(games, botB);
        if (clockA != clockB)
        {
            return clockA > clockB ? botA : botB;
        }

        int materialA = MaterialAdvantage(games, botA);
        int materialB = MaterialAdvantage(games, botB);

        // Material decides; failing that a coin flip guarantees a decisive winner
        // so the bracket can always advance.
        return materialA != materialB
            ? materialA > materialB ? botA : botB
            : random.Next(2) == 0 ? botA : botB;
    }

    private static int CountWins(IReadOnlyList<GameRecord> games, string botId) =>
        games.Count(game => IsWin(game, botId));

    private static bool IsWin(GameRecord game, string botId) =>
        (game.WhiteBotId == botId && game.Outcome == GameOutcome.WhiteWon) ||
        (game.BlackBotId == botId && game.Outcome == GameOutcome.BlackWon);

    // 1-based index of the bot's last win — the round by which it had accumulated
    // all its wins. int.MaxValue when it never won, so two winless bots tie here.
    private static int LastWinRound(IReadOnlyList<GameRecord> games, string botId)
    {
        int lastWinRound = int.MaxValue;
        for (int i = 0; i < games.Count; i++)
        {
            if (IsWin(games[i], botId))
            {
                lastWinRound = i + 1;
            }
        }

        return lastWinRound;
    }

    private static long ClockAdvantage(IReadOnlyList<GameRecord> games, string botId)
    {
        long total = 0;
        foreach (GameRecord game in games)
        {
            total += game.WhiteBotId == botId
                ? game.WhiteTimeMs - game.BlackTimeMs
                : game.BlackTimeMs - game.WhiteTimeMs;
        }

        return total;
    }

    private static int MaterialAdvantage(IReadOnlyList<GameRecord> games, string botId)
    {
        int total = 0;
        foreach (GameRecord game in games)
        {
            (int white, int black) = Material.Balance(game.FinalFen);
            total += game.WhiteBotId == botId ? white - black : black - white;
        }

        return total;
    }
}
