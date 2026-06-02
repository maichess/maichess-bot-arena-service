using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Tests.Support;
using NSubstitute;
using Reqnroll;
using Xunit;

namespace MaichessBotArenaService.Tests.StepDefinitions;

[Binding]
internal sealed class ArenaSettingsSteps(SettingsContext context)
{
    [Given("no concurrency limit is stored")]
    public void GivenNoConcurrencyLimitIsStored() =>
        context.Store.GetConcurrencyLimitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<int?>(null));

    [Given(@"the stored concurrency limit is (\d+)")]
    public void GivenTheStoredConcurrencyLimitIs(int limit) =>
        context.Store.GetConcurrencyLimitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<int?>(limit));

    [When("the concurrency limit is read")]
    public async Task WhenTheConcurrencyLimitIsRead() =>
        context.LastReadLimit = await context.Service.GetConcurrencyLimitAsync(CancellationToken.None);

    [When(@"the concurrency limit is set to (\d+)")]
    public async Task WhenTheConcurrencyLimitIsSetTo(int limit) =>
        context.SetResult = await context.Service.SetConcurrencyLimitAsync(limit, CancellationToken.None);

    [Then(@"the concurrency limit is (\d+)")]
    public void ThenTheConcurrencyLimitIs(int expected) =>
        Assert.Equal(expected, context.LastReadLimit);

    [Then(@"the set succeeds with limit (\d+)")]
    public void ThenTheSetSucceedsWithLimit(int expected)
    {
        SetConcurrencyLimitResult.Success success =
            Assert.IsType<SetConcurrencyLimitResult.Success>(context.SetResult);
        Assert.Equal(expected, success.Limit);
    }

    [Then(@"the stored concurrency limit is now (\d+)")]
    public async Task ThenTheStoredConcurrencyLimitIsNow(int expected) =>
        await context.Store.Received(1).SetConcurrencyLimitAsync(expected, Arg.Any<CancellationToken>());

    [Then(@"the set is rejected with ""([^""]*)""")]
    public void ThenTheSetIsRejectedWith(string message)
    {
        SetConcurrencyLimitResult.InvalidInput invalid =
            Assert.IsType<SetConcurrencyLimitResult.InvalidInput>(context.SetResult);
        Assert.Equal(message, invalid.Message);
    }
}
