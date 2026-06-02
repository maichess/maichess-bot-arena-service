using System.Globalization;
using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Tests.Support;
using Reqnroll;
using Xunit;

namespace MaichessBotArenaService.Tests.StepDefinitions;

[Binding]
internal sealed class ScoringSteps(StageContext context)
{
    [Then(@"bot ""([^""]*)"" has ([0-9.]+) points")]
    public void ThenBotHasPoints(string botId, string expected) =>
        Assert.Equal(double.Parse(expected, CultureInfo.InvariantCulture), Scoring.Total(context.Games, botId));

    [Then(@"the pair score for ""([^""]*)"" and ""([^""]*)"" is ([0-9.]+) to ([0-9.]+)")]
    public void ThenThePairScoreIs(string botA, string botB, string expectedA, string expectedB)
    {
        (double scoreA, double scoreB) = Scoring.PairScore(context.Games, botA, botB);
        Assert.Equal(double.Parse(expectedA, CultureInfo.InvariantCulture), scoreA);
        Assert.Equal(double.Parse(expectedB, CultureInfo.InvariantCulture), scoreB);
    }
}
