using System.Globalization;
using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Tests.Support;
using Reqnroll;
using Xunit;

namespace MaichessBotArenaService.Tests.StepDefinitions;

[Binding]
internal sealed class CollectionLifecycleSteps(CollectionContext context)
{
    private static readonly MatchOutcome WhiteWin = new(GameOutcome.WhiteWon, 1000, 1000, FenList.StandardFen);

    [Given(@"the known bots are ""([^""]*)""")]
    public void GivenTheKnownBots(string bots) =>
        context.Catalog.Bots.UnionWith(bots.Split(',').Select(bot => bot.Trim()));

    [Given(@"the concurrency limit is (\d+)")]
    public async Task GivenTheConcurrencyLimit(int limit) =>
        await context.Store.SetConcurrencyLimitAsync(limit, CancellationToken.None);

    [When("a setup is created:")]
    public async Task WhenASetupIsCreated(DataTable table)
    {
        Dictionary<string, string> fields = table.Rows.ToDictionary(row => row["field"], row => row["value"]);
        string Get(string key, string fallback) => fields.TryGetValue(key, out string? value) ? value : fallback;

        CreateCollectionCommand command = new(
            Get("name", "My setup"),
            Get("created_by", "creator-1"),
            Enum.Parse<SetupKind>(Get("kind", "Single"), ignoreCase: true),
            Split(Get("bots", string.Empty)),
            Get("white", string.Empty),
            Get("black", string.Empty),
            Split(Get("fens", "standard")),
            int.Parse(Get("games_per_fen", "1"), CultureInfo.InvariantCulture),
            int.Parse(Get("fens_per_stage", "1"), CultureInfo.InvariantCulture),
            Get("mode", "both") == "random" ? TournamentColorMode.Random : TournamentColorMode.BothColors,
            Get("matrix_mode", "alternating") == "random" ? MatrixColorMode.Random : MatrixColorMode.Alternating,
            Get("switching", "off") == "on",
            Get("time_format", "5+0"));

        context.CreateResult = await context.Service.CreateAsync(command, CancellationToken.None);
        if (context.CreateResult is CreateCollectionResult.Success success)
        {
            context.CreatedIds.Add(success.Collection.Id);
        }
    }

    [When(@"the concurrency limit is changed to (\d+)")]
    public async Task WhenTheConcurrencyLimitIsChangedTo(int limit) =>
        await context.Service.SetConcurrencyLimitAsync(limit, CancellationToken.None);

    [When("one running game finishes as a white win")]
    public async Task WhenOneRunningGameFinishes()
    {
        ArenaGame running = (await context.Store.ListRunningGamesAsync(CancellationToken.None))[0];
        await context.Service.HandleFinishedGameAsync(running, WhiteWin, CancellationToken.None);
    }

    [When("all running games finish as a white win")]
    public async Task WhenAllRunningGamesFinish()
    {
        foreach (ArenaGame game in await context.Store.ListRunningGamesAsync(CancellationToken.None))
        {
            await context.Service.HandleFinishedGameAsync(game, WhiteWin, CancellationToken.None);
        }
    }

    [When("the setup runs to completion with white always winning")]
    public async Task WhenTheSetupRunsToCompletion()
    {
        for (int guard = 0; guard < 1000; guard++)
        {
            ArenaCollection? collection = await context.Store.GetCollectionAsync(context.CollectionId, CancellationToken.None);
            if (collection!.Status == "finished")
            {
                break;
            }

            IReadOnlyList<ArenaGame> running = await context.Store.ListRunningGamesAsync(CancellationToken.None);
            if (running.Count == 0)
            {
                break;
            }

            foreach (ArenaGame game in running)
            {
                await context.Service.HandleFinishedGameAsync(game, WhiteWin, CancellationToken.None);
            }
        }
    }

    [When("a finished game for a missing collection is handled")]
    public async Task WhenAFinishedGameForAMissingCollection() =>
        await context.Service.HandleFinishedGameAsync(
            new ArenaGame { CollectionId = "missing", Status = "running" }, WhiteWin, CancellationToken.None);

    [When(@"collections are listed with status ""([^""]*)"" limit (\d+) offset (\d+)")]
    public async Task WhenCollectionsAreListed(string status, int limit, int offset) =>
        context.Listed = await context.Service.ListAsync(
            status.Length == 0 ? null : status, limit, offset, CancellationToken.None);

    [When("the collection is fetched")]
    public async Task WhenTheCollectionIsFetched() =>
        context.Fetched = await context.Service.GetWithGamesAsync(context.CollectionId, CancellationToken.None);

    [When(@"a missing collection ""([^""]*)"" is fetched")]
    public async Task WhenAMissingCollectionIsFetched(string id) =>
        context.Fetched = await context.Service.GetWithGamesAsync(id, CancellationToken.None);

    [Then(@"the create result is invalid input ""([^""]*)""")]
    public void ThenTheCreateResultIsInvalid(string message) =>
        Assert.Equal(message, Assert.IsType<CreateCollectionResult.InvalidInput>(context.CreateResult).Message);

    [Then("the create result is success")]
    public void ThenTheCreateResultIsSuccess() =>
        Assert.IsType<CreateCollectionResult.Success>(context.CreateResult);

    [Then(@"the collection status is ""([^""]*)""")]
    public async Task ThenTheCollectionStatusIs(string status)
    {
        ArenaCollection? collection = await context.Store.GetCollectionAsync(context.CollectionId, CancellationToken.None);
        Assert.Equal(status, collection!.Status);
    }

    [Then(@"the collection winner is ""([^""]*)""")]
    public async Task ThenTheCollectionWinnerIs(string winner)
    {
        ArenaCollection? collection = await context.Store.GetCollectionAsync(context.CollectionId, CancellationToken.None);
        Assert.Equal(winner, collection!.WinnerBotId);
    }

    [Then(@"(\d+) games are (pending|running|finished)")]
    public async Task ThenGamesAreInState(int count, string status)
    {
        IReadOnlyList<ArenaGame> games = await context.Store.ListGamesAsync(context.CollectionId, CancellationToken.None);
        Assert.Equal(count, games.Count(game => game.Status == status));
    }

    [Then(@"(\d+) games are (pending|running|finished) in total")]
    public async Task ThenGamesAreInStateInTotal(int count, string status)
    {
        int running = (await context.Store.ListRunningGamesAsync(CancellationToken.None)).Count;
        int pending = (await context.Store.ListPendingGamesAsync(CancellationToken.None)).Count;

        int actual = status switch
        {
            "running" => running,
            "pending" => pending,
            _ => await CountFinishedAsync(),
        };
        Assert.Equal(count, actual);
    }

    [Then(@"setup (\d+) is ""([^""]*)""")]
    public async Task ThenSetupIs(int index, string status)
    {
        string id = context.CreatedIds[index - 1];
        ArenaCollection? collection = await context.Store.GetCollectionAsync(id, CancellationToken.None);
        Assert.Equal(status, collection!.Status);
    }

    [Then(@"the collection has (\d+) games")]
    public async Task ThenTheCollectionHasGames(int count)
    {
        IReadOnlyList<ArenaGame> games = await context.Store.ListGamesAsync(context.CollectionId, CancellationToken.None);
        Assert.Equal(count, games.Count);
    }

    [Then(@"a game was launched with start_fen ""([^""]*)""")]
    public void ThenAGameWasLaunchedWithStartFen(string startFen) =>
        Assert.Contains(context.Launcher.Launches, launch => launch.StartFen == startFen);

    [Then("no games were launched")]
    public void ThenNoGamesWereLaunched() =>
        Assert.Empty(context.Launcher.Launches);

    [Then(@"(\d+) collections are listed")]
    public void ThenCollectionsAreListed(int count) =>
        Assert.Equal(count, context.Listed!.Count);

    [Then("the fetch returns the collection with its games")]
    public void ThenTheFetchReturnsTheCollection()
    {
        Assert.NotNull(context.Fetched);
        Assert.NotEmpty(context.Fetched!.Value.Games);
    }

    [Then("the fetch returns nothing")]
    public void ThenTheFetchReturnsNothing() =>
        Assert.Null(context.Fetched);

    private async Task<int> CountFinishedAsync()
    {
        int finished = 0;
        foreach (string id in context.CreatedIds)
        {
            IReadOnlyList<ArenaGame> games = await context.Store.ListGamesAsync(id, CancellationToken.None);
            finished += games.Count(game => game.Status == "finished");
        }

        return finished;
    }

    private static IReadOnlyList<string> Split(string csv) =>
        csv.Length == 0 ? [] : [.. csv.Split(',').Select(token => token.Trim())];
}
