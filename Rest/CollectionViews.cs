namespace MaichessBotArenaService.Rest;

// JSON shapes for the collection REST responses. Constructed by ResultViewBuilder
// and serialized with snake_case naming; null result/config branches are omitted.
internal static class CollectionViews
{
    internal sealed record TimeFormatView(string Id, long BaseMs, long IncrementMs, string Category);

    internal sealed record SingleConfigView(
        string WhiteBotId,
        string BlackBotId,
        IReadOnlyList<string> FenList,
        int GamesPerFen,
        bool KeepSwitchingColors,
        TimeFormatView TimeFormat);

    internal sealed record MatrixConfigView(
        IReadOnlyList<string> BotIds,
        IReadOnlyList<string> FenList,
        int GamesPerFen,
        TimeFormatView TimeFormat);

    internal sealed record TournamentConfigView(
        IReadOnlyList<string> BotIds,
        IReadOnlyList<string> FenList,
        int FensPerStage,
        string ColorMode,
        TimeFormatView TimeFormat);

    // Exactly one branch is populated, selected by the collection's setup type.
    internal sealed record ConfigView(
        TournamentConfigView? Tournament,
        MatrixConfigView? Matrix,
        SingleConfigView? Single);

    internal sealed record Progress(int TotalGames, int FinishedGames, int RunningGames, int PendingGames);

    internal sealed record GameResult(
        string MatchId, string Fen, string FenLabel, string WhiteBotId, string BlackBotId, string Result, int Order);

    internal sealed record SingleSeries(
        string BotAId, string BotBId, double BotAScore, double BotBScore, IReadOnlyList<GameResult> Games);

    internal sealed record MatrixCell(string BotAId, string BotBId, double BotAScore, double BotBScore);

    internal sealed record MatrixTable(
        IReadOnlyList<string> BotIds, IReadOnlyList<MatrixCell> Cells, IReadOnlyList<GameResult> Games);

    internal sealed record BracketPairing(
        string BotAId,
        string? BotBId,
        bool Bye,
        string? WinnerBotId,
        double BotAScore,
        double BotBScore,
        IReadOnlyList<GameResult> Games);

    internal sealed record BracketRound(int RoundNumber, IReadOnlyList<BracketPairing> Pairings);

    internal sealed record Bracket(IReadOnlyList<BracketRound> Rounds, string? WinnerBotId);

    // Exactly one branch is populated, selected by the collection's setup type.
    internal sealed record ResultView(
        Bracket? Bracket,
        MatrixTable? MatrixTable,
        SingleSeries? SingleSeries);

    internal sealed record Summary(
        string Id,
        string Name,
        string Type,
        string CreatedBy,
        string Status,
        long CreatedAtMs,
        long FinishedAtMs,
        Progress Progress);

    internal sealed record Detail(
        string Id,
        string Name,
        string Type,
        string CreatedBy,
        string Status,
        long CreatedAtMs,
        long FinishedAtMs,
        ConfigView Config,
        Progress Progress,
        ResultView Result);
}
