using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Domain;
using static MaichessBotArenaService.Rest.CollectionViews;

namespace MaichessBotArenaService.Rest;

// Turns a stored collection and its games into the REST response shapes,
// including the typed result view (single series, matrix table, or bracket).
internal sealed class ResultViewBuilder(IArenaRandomProvider randomProvider)
{
    internal Detail BuildDetail(ArenaCollection collection, IReadOnlyList<ArenaGame> games)
    {
        SingleSeries? single = collection.Kind == SetupKind.Single ? BuildSingle(collection, games) : null;
        MatrixTable? matrix = collection.Kind == SetupKind.Matrix ? BuildMatrix(collection, games) : null;
        CollectionViews.Bracket? bracket = collection.Kind == SetupKind.Tournament ? BuildBracket(collection, games) : null;

        return new Detail(
            collection.Id,
            collection.Name,
            TypeName(collection.Kind),
            collection.CreatedBy,
            collection.Status,
            collection.CreatedAtMs,
            collection.FinishedAtMs,
            BuildProgress(games),
            BuildConfig(collection),
            single,
            matrix,
            bracket);
    }

    internal static Summary BuildSummary(ArenaCollection collection, IReadOnlyList<ArenaGame> games) =>
        new(
            collection.Id,
            collection.Name,
            TypeName(collection.Kind),
            collection.CreatedBy,
            collection.Status,
            collection.CreatedAtMs,
            collection.FinishedAtMs,
            BuildProgress(games));

    private static SingleSeries BuildSingle(ArenaCollection collection, IReadOnlyList<ArenaGame> games)
    {
        IReadOnlyList<GameRecord> records = [.. games.Select(ToRecord)];
        return new SingleSeries(
            collection.WhiteBotId,
            collection.BlackBotId,
            Scoring.Total(records, collection.WhiteBotId),
            Scoring.Total(records, collection.BlackBotId),
            [.. games.Select(ToGameResult)]);
    }

    private static MatrixTable BuildMatrix(ArenaCollection collection, IReadOnlyList<ArenaGame> games)
    {
        List<MatrixCell> cells = [];
        for (int i = 0; i < collection.BotIds.Count; i++)
        {
            for (int j = i + 1; j < collection.BotIds.Count; j++)
            {
                string a = collection.BotIds[i];
                string b = collection.BotIds[j];
                IReadOnlyList<GameRecord> pairRecords = [.. games.Where(game => Involves(game, a, b)).Select(ToRecord)];
                (double scoreA, double scoreB) = Scoring.PairScore(pairRecords, a, b);
                cells.Add(new MatrixCell(a, b, scoreA, scoreB));
            }
        }

        return new MatrixTable(collection.BotIds, cells, [.. games.Select(ToGameResult)]);
    }

    private CollectionViews.Bracket BuildBracket(ArenaCollection collection, IReadOnlyList<ArenaGame> games)
    {
        TournamentBracket.BracketState state = TournamentBracket.Evaluate(
            collection.Seeded,
            (round, pairing) =>
                [.. games.Where(game => game.Round == round && game.Pairing == pairing).OrderBy(game => game.Order).Select(ToRecord)],
            (round, pairing) => randomProvider.ForStage(collection.Id, round, pairing));

        List<BracketRound> rounds = [];
        for (int round = 0; round < state.Rounds.Count; round++)
        {
            List<BracketPairing> pairings = [];
            for (int pairing = 0; pairing < state.Rounds[round].Pairings.Count; pairing++)
            {
                TournamentBracket.StagePairing stage = state.Rounds[round].Pairings[pairing];
                IReadOnlyList<ArenaGame> stageGames =
                    [.. games.Where(game => game.Round == round && game.Pairing == pairing).OrderBy(game => game.Order)];
                (double scoreA, double scoreB) = stage.BotB is null
                    ? (0.0, 0.0)
                    : Scoring.PairScore([.. stageGames.Select(ToRecord)], stage.BotA, stage.BotB);

                pairings.Add(new BracketPairing(
                    stage.BotA,
                    stage.BotB,
                    stage.Bye,
                    stage.WinnerBotId,
                    scoreA,
                    scoreB,
                    [.. stageGames.Select(ToGameResult)]));
            }

            rounds.Add(new BracketRound(state.Rounds[round].RoundNumber, pairings));
        }

        return new CollectionViews.Bracket(rounds, string.IsNullOrEmpty(collection.WinnerBotId) ? null : collection.WinnerBotId);
    }

    private static ConfigView BuildConfig(ArenaCollection collection)
    {
        string? colorMode = collection.Kind == SetupKind.Tournament
            ? collection.ColorMode == TournamentColorMode.Random ? "random" : "both_colors"
            : null;

        return new ConfigView(
            collection.Kind == SetupKind.Single ? null : collection.BotIds,
            collection.Kind == SetupKind.Single ? collection.WhiteBotId : null,
            collection.Kind == SetupKind.Single ? collection.BlackBotId : null,
            collection.FenList,
            collection.GamesPerFen,
            collection.FensPerStage,
            colorMode,
            collection.KeepSwitchingColors,
            new TimeFormatView(
                collection.TimeFormat.Id,
                collection.TimeFormat.BaseMs,
                collection.TimeFormat.IncrementMs,
                collection.TimeFormat.Category));
    }

    private static Progress BuildProgress(IReadOnlyList<ArenaGame> games) =>
        new(
            games.Count,
            games.Count(game => game.Status == "finished"),
            games.Count(game => game.Status == "running"),
            games.Count(game => game.Status == "pending"));

    private static bool Involves(ArenaGame game, string a, string b) =>
        (game.WhiteBotId == a && game.BlackBotId == b) || (game.WhiteBotId == b && game.BlackBotId == a);

    private static GameRecord ToRecord(ArenaGame game) =>
        new(game.WhiteBotId, game.BlackBotId, game.Outcome, game.WhiteTimeMs, game.BlackTimeMs, game.FinalFen);

    private static GameResult ToGameResult(ArenaGame game) =>
        new(game.MatchId, game.Fen, game.FenLabel, game.WhiteBotId, game.BlackBotId, ResultName(game.Outcome), game.Order);

    private static string TypeName(SetupKind kind) => kind switch
    {
        SetupKind.Tournament => "tournament",
        SetupKind.Matrix => "matrix",
        _ => "single",
    };

    private static string ResultName(GameOutcome outcome) => outcome switch
    {
        GameOutcome.WhiteWon => "white_won",
        GameOutcome.BlackWon => "black_won",
        GameOutcome.Draw => "draw",
        _ => "ongoing",
    };
}
