using MaichessBotArenaService.Domain;
using Xunit;

namespace MaichessBotArenaService.Tests;

// Low-level pure calculations backing the tie-break and bracket logic.
public sealed class DomainMathTests
{
    [Fact]
    public void Material_StandardPosition_IsBalancedAt39()
    {
        (int white, int black) = Material.Balance(FenList.StandardFen);

        Assert.Equal(39, white);
        Assert.Equal(39, black);
    }

    [Fact]
    public void Material_LoneQueenVersusBareKing_CountsOnlyTheQueen()
    {
        (int white, int black) = Material.Balance("Q6k/8/8/8/8/8/8/K7 w - - 0 1");

        Assert.Equal(9, white);
        Assert.Equal(0, black);
    }

    [Fact]
    public void Material_EmptyBoard_IsZero()
    {
        (int white, int black) = Material.Balance("8/8/8/8/8/8/8/8 w - - 0 1");

        Assert.Equal(0, white);
        Assert.Equal(0, black);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 4)]
    [InlineData(5, 8)]
    [InlineData(8, 8)]
    [InlineData(9, 16)]
    public void NextPowerOfTwo_RoundsUp(int value, int expected) =>
        Assert.Equal(expected, Bracket.NextPowerOfTwo(value));
}
