using MaichessBotArenaService.Domain;
using Xunit;

namespace MaichessBotArenaService.Tests;

public sealed class DefaultArenaRandomTests
{
    [Fact]
    public void Next_StaysWithinBound()
    {
        DefaultArenaRandom random = new(seed: 42);

        for (int i = 0; i < 100; i++)
        {
            int value = random.Next(3);
            Assert.InRange(value, 0, 2);
        }
    }

    [Fact]
    public void Next_SameSeed_ProducesSameSequence()
    {
        DefaultArenaRandom a = new(seed: 7);
        DefaultArenaRandom b = new(seed: 7);

        int[] first = [.. Enumerable.Range(0, 10).Select(_ => a.Next(1000))];
        int[] second = [.. Enumerable.Range(0, 10).Select(_ => b.Next(1000))];

        Assert.Equal(first, second);
    }
}
