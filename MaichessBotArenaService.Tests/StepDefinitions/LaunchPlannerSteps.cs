using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Tests.Support;
using Reqnroll;
using Xunit;

namespace MaichessBotArenaService.Tests.StepDefinitions;

[Binding]
internal sealed class LaunchPlannerSteps(LaunchPlannerContext context)
{
    [When(@"the limit is (\d+), (\d+) are running and (\d+) are waiting")]
    public void WhenThePlannerIsAsked(int limit, int running, int pending) =>
        context.Launchable = LaunchPlanner.LaunchableCount(limit, running, pending);

    [Then(@"(\d+) games may launch")]
    public void ThenGamesMayLaunch(int expected) =>
        Assert.Equal(expected, context.Launchable);
}
