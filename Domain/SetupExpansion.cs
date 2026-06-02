namespace MaichessBotArenaService.Domain;

// Pure expansion of a setup config into the exact ordered list of games to play.
// Color-alternation and FEN-iteration rules are fixed here and mirrored in the
// knowledge-base ADR.
internal static class SetupExpansion
{
    // Single setup: for each FEN, `gamesPerFen` games. With keepSwitchingColors
    // the white/black assignment flips every game across the whole series (and
    // therefore across FENs); otherwise the configured assignment is fixed.
    internal static IReadOnlyList<ExpandedGame> ExpandSingle(
        string whiteBotId,
        string blackBotId,
        IReadOnlyList<string> fenList,
        int gamesPerFen,
        bool keepSwitchingColors)
    {
        List<ExpandedGame> games = [];
        int order = 0;

        for (int fenIndex = 0; fenIndex < fenList.Count; fenIndex++)
        {
            string fen = fenList[fenIndex];
            string label = FenList.Label(fenIndex, fen);
            for (int game = 0; game < gamesPerFen; game++)
            {
                bool swap = keepSwitchingColors && (order % 2 == 1);
                (string white, string black) = swap
                    ? (blackBotId, whiteBotId)
                    : (whiteBotId, blackBotId);
                games.Add(new ExpandedGame(order, white, black, fen, label));
                order++;
            }
        }

        return games;
    }

    // Matrix setup: every unordered bot pair (in input order), for each FEN,
    // `gamesPerFen` games with colors alternating per game continuously within
    // the pairing (across its FENs).
    internal static IReadOnlyList<ExpandedGame> ExpandMatrix(
        IReadOnlyList<string> botIds,
        IReadOnlyList<string> fenList,
        int gamesPerFen)
    {
        List<ExpandedGame> games = [];
        int order = 0;

        for (int i = 0; i < botIds.Count; i++)
        {
            for (int j = i + 1; j < botIds.Count; j++)
            {
                int pairGame = 0;
                for (int fenIndex = 0; fenIndex < fenList.Count; fenIndex++)
                {
                    string fen = fenList[fenIndex];
                    string label = FenList.Label(fenIndex, fen);
                    for (int game = 0; game < gamesPerFen; game++)
                    {
                        bool swap = pairGame % 2 == 1;
                        (string white, string black) = swap
                            ? (botIds[j], botIds[i])
                            : (botIds[i], botIds[j]);
                        games.Add(new ExpandedGame(order, white, black, fen, label));
                        order++;
                        pairGame++;
                    }
                }
            }
        }

        return games;
    }

    // Tournament stage (one pairing): for each chosen FEN, both-colors mode plays
    // two games (bot A white then bot B white); random mode plays one game with
    // colors chosen by the injected RNG.
    internal static IReadOnlyList<ExpandedGame> ExpandTournamentStage(
        string botAId,
        string botBId,
        IReadOnlyList<string> stageFens,
        bool randomColors,
        IArenaRandom random)
    {
        List<ExpandedGame> games = [];
        int order = 0;

        for (int fenIndex = 0; fenIndex < stageFens.Count; fenIndex++)
        {
            string fen = stageFens[fenIndex];
            string label = FenList.Label(fenIndex, fen);

            if (randomColors)
            {
                bool aIsWhite = random.Next(2) == 0;
                (string white, string black) = aIsWhite ? (botAId, botBId) : (botBId, botAId);
                games.Add(new ExpandedGame(order, white, black, fen, label));
                order++;
            }
            else
            {
                games.Add(new ExpandedGame(order, botAId, botBId, fen, label));
                order++;
                games.Add(new ExpandedGame(order, botBId, botAId, fen, label));
                order++;
            }
        }

        return games;
    }
}
