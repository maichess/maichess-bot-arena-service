using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Tests.Support;
using Reqnroll;
using Xunit;

namespace MaichessBotArenaService.Tests.StepDefinitions;

[Binding]
internal sealed class FenListSteps(ExpansionContext context)
{
    [When(@"the FEN pool ""([^""]*)"" is normalized")]
    public void WhenTheFenPoolIsNormalized(string input)
    {
        context.NormalizedPool = FenList.Normalize(input.Split(','));
    }

    [Then(@"the normalized pool is ""([^""]*)""")]
    public void ThenTheNormalizedPoolIs(string expected)
    {
        Assert.NotNull(context.NormalizedPool);
        Assert.Equal(ExpansionContext.MapFens(expected), context.NormalizedPool);
    }

    [Then(@"the label for the standard position at index (\d+) is ""([^""]*)""")]
    public void ThenTheLabelForTheStandardPosition(int index, string expected) =>
        Assert.Equal(expected, FenList.Label(index, FenList.StandardFen));

    [Then(@"the label for a custom position at index (\d+) is ""([^""]*)""")]
    public void ThenTheLabelForACustomPosition(int index, string expected) =>
        Assert.Equal(expected, FenList.Label(index, "custom-position"));
}
