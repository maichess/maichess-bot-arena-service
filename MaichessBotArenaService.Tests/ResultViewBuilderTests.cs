using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Rest;
using MaichessBotArenaService.Tests.Support;
using Xunit;

namespace MaichessBotArenaService.Tests;

public sealed class ResultViewBuilderTests
{
    private static readonly TimeFormatInfo Blitz = new("5+0", 300_000, 0, "blitz");

    [Fact]
    public void BuildDetail_Single_ScoresByBotAndRendersEveryOutcome()
    {
        ArenaCollection collection = new()
        {
            Id = "c1", Name = "S", Kind = SetupKind.Single, CreatedBy = "u", Status = "finished",
            WhiteBotId = "a", BlackBotId = "b", FenList = ["standard"], GamesPerFen = 4, TimeFormat = Blitz,
        };
        List<ArenaGame> games =
        [
            SingleGame(0, GameOutcome.WhiteWon, "finished"),
            SingleGame(1, GameOutcome.BlackWon, "finished"),
            SingleGame(2, GameOutcome.Draw, "finished"),
            SingleGame(3, GameOutcome.Ongoing, "running"),
        ];

        CollectionViews.Detail detail = new ResultViewBuilder(new FakeArenaRandomProvider()).BuildDetail(collection, games);

        Assert.Equal("single", detail.Type);
        Assert.NotNull(detail.SingleSeries);
        Assert.Equal(1.5, detail.SingleSeries!.BotAScore);
        Assert.Equal(1.5, detail.SingleSeries.BotBScore);
        Assert.Equal(["white_won", "black_won", "draw", "ongoing"], detail.SingleSeries.Games.Select(game => game.Result));
        Assert.Equal(4, detail.Progress.TotalGames);
        Assert.Equal(3, detail.Progress.FinishedGames);
        Assert.Equal(1, detail.Progress.RunningGames);
        Assert.Equal("a", detail.Config.WhiteBotId);
        Assert.Null(detail.Config.BotIds);
        Assert.Null(detail.Config.ColorMode);
        Assert.Null(detail.MatrixTable);
        Assert.Null(detail.Bracket);
    }

    [Fact]
    public void BuildDetail_Matrix_BuildsACellPerPair()
    {
        ArenaCollection collection = new()
        {
            Id = "c2", Name = "M", Kind = SetupKind.Matrix, CreatedBy = "u", Status = "finished",
            BotIds = ["a", "b", "c"], FenList = ["standard"], GamesPerFen = 1, TimeFormat = Blitz,
        };
        List<ArenaGame> games =
        [
            MatrixGame(0, "a", "b", GameOutcome.WhiteWon),
            MatrixGame(1, "a", "c", GameOutcome.BlackWon),
            MatrixGame(2, "b", "c", GameOutcome.Draw),
        ];

        CollectionViews.Detail detail = new ResultViewBuilder(new FakeArenaRandomProvider()).BuildDetail(collection, games);

        Assert.Equal("matrix", detail.Type);
        Assert.NotNull(detail.MatrixTable);
        Assert.Equal(3, detail.MatrixTable!.Cells.Count);
        Assert.Equal(["a", "b", "c"], detail.MatrixTable.BotIds);
        Assert.Null(detail.Config.WhiteBotId);
        Assert.Equal(["a", "b", "c"], detail.Config.BotIds);
        Assert.Null(detail.SingleSeries);
    }

    [Fact]
    public void BuildSummary_IncludesProgress()
    {
        ArenaCollection collection = new()
        {
            Id = "c3", Name = "S", Kind = SetupKind.Single, CreatedBy = "u", Status = "running",
            WhiteBotId = "a", BlackBotId = "b", FenList = ["standard"], TimeFormat = Blitz,
        };

        CollectionViews.Summary summary =
            ResultViewBuilder.BuildSummary(collection, [SingleGame(0, GameOutcome.Ongoing, "running")]);

        Assert.Equal("single", summary.Type);
        Assert.Equal("c3", summary.Id);
        Assert.Equal(1, summary.Progress.TotalGames);
        Assert.Equal(1, summary.Progress.PendingGames == 0 ? summary.Progress.RunningGames : 0);
    }

    [Fact]
    public async Task BuildDetail_Tournament_RendersByeBracketAndChampion()
    {
        (ArenaCollection collection, IReadOnlyList<ArenaGame> games) =
            await RunTournamentAsync("a,b,c", TournamentColorMode.BothColors, run: true);

        CollectionViews.Detail detail = new ResultViewBuilder(new FakeArenaRandomProvider()).BuildDetail(collection, games);

        Assert.Equal("tournament", detail.Type);
        Assert.NotNull(detail.Bracket);
        Assert.Equal("a", detail.Bracket!.WinnerBotId);
        Assert.Equal(2, detail.Bracket.Rounds.Count);
        Assert.Contains(detail.Bracket.Rounds[0].Pairings, pairing => pairing.Bye);
        Assert.Equal("both_colors", detail.Config.ColorMode);
    }

    [Fact]
    public async Task BuildDetail_Tournament_UnfinishedHasNullWinners()
    {
        (ArenaCollection collection, IReadOnlyList<ArenaGame> games) =
            await RunTournamentAsync("a,b", TournamentColorMode.Random, run: false);

        CollectionViews.Detail detail = new ResultViewBuilder(new FakeArenaRandomProvider()).BuildDetail(collection, games);

        Assert.Null(detail.Bracket!.WinnerBotId);
        Assert.All(detail.Bracket.Rounds[0].Pairings, pairing => Assert.Null(pairing.WinnerBotId));
        Assert.Equal("random", detail.Config.ColorMode);
    }

    private static async Task<(ArenaCollection, IReadOnlyList<ArenaGame>)> RunTournamentAsync(
        string bots, TournamentColorMode mode, bool run)
    {
        FakeArenaStore store = new();
        FakeBotCatalog catalog = new();
        catalog.Bots.UnionWith(bots.Split(','));
        ArenaSettingsService settings = new(store);
        CollectionService service = new(
            store, new FakeGameLauncher(), catalog, settings, new FakeArenaRandomProvider(), () => 1000);

        CreateCollectionCommand command = new(
            "T", "creator", SetupKind.Tournament, [.. bots.Split(',')], string.Empty, string.Empty,
            ["standard"], 1, 1, mode, false, "5+0");
        CreateCollectionResult result = await service.CreateAsync(command, CancellationToken.None);
        string id = ((CreateCollectionResult.Success)result).Collection.Id;

        if (run)
        {
            for (int guard = 0; guard < 1000; guard++)
            {
                ArenaCollection? current = await store.GetCollectionAsync(id, CancellationToken.None);
                if (current!.Status == "finished")
                {
                    break;
                }

                IReadOnlyList<ArenaGame> running = await store.ListRunningGamesAsync(CancellationToken.None);
                foreach (ArenaGame game in running)
                {
                    await service.HandleFinishedGameAsync(
                        game, new MatchOutcome(GameOutcome.WhiteWon, 1000, 1000, FenList.StandardFen), CancellationToken.None);
                }
            }
        }

        ArenaCollection? collection = await store.GetCollectionAsync(id, CancellationToken.None);
        IReadOnlyList<ArenaGame> games = await store.ListGamesAsync(id, CancellationToken.None);
        return (collection!, games);
    }

    private static ArenaGame SingleGame(int order, GameOutcome outcome, string status) => new()
    {
        CollectionId = "c1", Order = order, WhiteBotId = "a", BlackBotId = "b", Fen = FenList.StandardFen,
        FenLabel = "Standard", Status = status, Outcome = outcome, MatchId = $"m{order}",
        WhiteTimeMs = 1000, BlackTimeMs = 1000, FinalFen = FenList.StandardFen,
    };

    private static ArenaGame MatrixGame(int order, string white, string black, GameOutcome outcome) => new()
    {
        CollectionId = "c2", Order = order, WhiteBotId = white, BlackBotId = black, Fen = FenList.StandardFen,
        FenLabel = "Standard", Status = "finished", Outcome = outcome, MatchId = $"m{order}",
        WhiteTimeMs = 1000, BlackTimeMs = 1000, FinalFen = FenList.StandardFen,
    };
}
