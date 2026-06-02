using MaichessBotArenaService.Arena;
using Xunit;

namespace MaichessBotArenaService.Tests;

public sealed class DeterministicSeedTests
{
    [Fact]
    public void From_IsStableForTheSameInputs() =>
        Assert.Equal(DeterministicSeed.From("collection-1", 1, 2), DeterministicSeed.From("collection-1", 1, 2));

    [Theory]
    [InlineData("collection-1", 1, 3)]
    [InlineData("collection-2", 1, 2)]
    [InlineData("collection-1", 5, 2)]
    public void From_VariesWithInputs(string text, int a, int b) =>
        Assert.NotEqual(DeterministicSeed.From("collection-1", 1, 2), DeterministicSeed.From(text, a, b));
}
