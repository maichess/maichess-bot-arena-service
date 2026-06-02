using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Tests.Support;
using Reqnroll;
using Xunit;

namespace MaichessBotArenaService.Tests.StepDefinitions;

[Binding]
internal sealed class BracketSteps(StageContext context)
{
    [When(@"bots ""([^""]*)"" are seeded with RNG ""([^""]*)""")]
    public void WhenBotsAreSeeded(string bots, string rng)
    {
        int[] values = [.. rng.Split(',').Select(int.Parse)];
        context.Seeded = Bracket.Seed(Split(bots), new FakeArenaRandom(values));
    }

    [When(@"the first round is built from seeds ""([^""]*)""")]
    public void WhenTheFirstRoundIsBuilt(string seeds) =>
        context.Pairings = Bracket.FirstRound(Split(seeds));

    [When(@"the next round is built from advancing ""([^""]*)""")]
    public void WhenTheNextRoundIsBuilt(string advancing) =>
        context.Pairings = Bracket.NextRound(Split(advancing));

    [Then(@"the seeding is ""([^""]*)""")]
    public void ThenTheSeedingIs(string expected)
    {
        Assert.NotNull(context.Seeded);
        Assert.Equal(Split(expected), context.Seeded);
    }

    [Then("the pairings are:")]
    public void ThenThePairingsAre(DataTable table)
    {
        Assert.NotNull(context.Pairings);
        Assert.Equal(table.Rows.Count, context.Pairings.Count);

        int index = 0;
        foreach (DataTableRow row in table.Rows)
        {
            Pairing pairing = context.Pairings[index];
            string? expectedB = row["botB"].Length == 0 ? null : row["botB"];

            Assert.Equal(row["botA"], pairing.BotA);
            Assert.Equal(expectedB, pairing.BotB);
            Assert.Equal(expectedB is null, pairing.IsBye);
            index++;
        }
    }

    private static IReadOnlyList<string> Split(string csv) =>
        [.. csv.Split(',').Select(token => token.Trim())];
}
