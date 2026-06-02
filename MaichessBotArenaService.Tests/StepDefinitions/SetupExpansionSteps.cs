using System.Globalization;
using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Tests.Support;
using Reqnroll;
using Xunit;

namespace MaichessBotArenaService.Tests.StepDefinitions;

[Binding]
internal sealed class SetupExpansionSteps(ExpansionContext context)
{
    [When(@"a single setup expands white ""([^""]*)"" black ""([^""]*)"" over FENs ""([^""]*)"" with (\d+) games per FEN and color switching (on|off)")]
    public void WhenASingleSetupExpands(string white, string black, string fens, int gamesPerFen, string switching)
    {
        context.Games = SetupExpansion.ExpandSingle(
            white, black, ExpansionContext.MapFens(fens), gamesPerFen, switching == "on");
    }

    [When(@"a matrix setup expands bots ""([^""]*)"" over FENs ""([^""]*)"" with (\d+) games per FEN")]
    public void WhenAMatrixSetupExpands(string bots, string fens, int gamesPerFen)
    {
        context.Games = SetupExpansion.ExpandMatrix(
            SplitTokens(bots), ExpansionContext.MapFens(fens), gamesPerFen);
    }

    [When(@"a tournament stage expands bots ""([^""]*)"" and ""([^""]*)"" over FENs ""([^""]*)"" in both-colors mode")]
    public void WhenATournamentStageBothColors(string botA, string botB, string fens)
    {
        context.Games = SetupExpansion.ExpandTournamentStage(
            botA, botB, ExpansionContext.MapFens(fens), false, new FakeArenaRandom(0));
    }

    [When(@"a tournament stage expands bots ""([^""]*)"" and ""([^""]*)"" over FENs ""([^""]*)"" in random mode with RNG ""([^""]*)""")]
    public void WhenATournamentStageRandom(string botA, string botB, string fens, string rng)
    {
        int[] values = [.. rng.Split(',').Select(value => int.Parse(value, CultureInfo.InvariantCulture))];
        context.Games = SetupExpansion.ExpandTournamentStage(
            botA, botB, ExpansionContext.MapFens(fens), true, new FakeArenaRandom(values));
    }

    [Then(@"the expansion produces:")]
    public void ThenTheExpansionProduces(DataTable table)
    {
        Assert.NotNull(context.Games);
        Assert.Equal(table.Rows.Count, context.Games.Count);

        int index = 0;
        foreach (DataTableRow row in table.Rows)
        {
            ExpandedGame game = context.Games[index];
            Assert.Equal(int.Parse(row["order"], CultureInfo.InvariantCulture), game.Order);
            Assert.Equal(row["white"], game.WhiteBotId);
            Assert.Equal(row["black"], game.BlackBotId);
            Assert.Equal(row["label"], game.FenLabel);
            index++;
        }
    }

    [Then(@"the expansion has (\d+) games")]
    public void ThenTheExpansionHasGames(int count)
    {
        Assert.NotNull(context.Games);
        Assert.Equal(count, context.Games.Count);
    }

    [Then(@"the expansion covers (\d+) distinct unordered pairs")]
    public void ThenTheExpansionCoversDistinctPairs(int count)
    {
        Assert.NotNull(context.Games);
        int distinct = context.Games
            .Select(game => string.CompareOrdinal(game.WhiteBotId, game.BlackBotId) <= 0
                ? (game.WhiteBotId, game.BlackBotId)
                : (game.BlackBotId, game.WhiteBotId))
            .Distinct()
            .Count();
        Assert.Equal(count, distinct);
    }

    private static IReadOnlyList<string> SplitTokens(string csv) =>
        [.. csv.Split(',').Select(token => token.Trim())];
}
