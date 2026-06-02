using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Tests.Support;
using Reqnroll;
using Xunit;

namespace MaichessBotArenaService.Tests.StepDefinitions;

[Binding]
internal sealed class TieBreakSteps(StageContext context)
{
    [When("the stage winner is decided")]
    public void WhenTheStageWinnerIsDecided() =>
        context.Winner = TieBreak.DecideWinner("a", "b", context.Games, context.Random);

    [Then(@"the stage winner is ""([^""]*)""")]
    public void ThenTheStageWinnerIs(string expected) =>
        Assert.Equal(expected, context.Winner);
}
