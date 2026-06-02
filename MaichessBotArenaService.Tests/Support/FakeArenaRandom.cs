using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Tests.Support;

// Deterministic IArenaRandom returning a scripted sequence (cycled), each value
// reduced modulo the requested bound.
internal sealed class FakeArenaRandom(params int[] values) : IArenaRandom
{
    private readonly int[] values = values;
    private int index;

    public int Next(int maxExclusive)
    {
        int value = values[index % values.Length];
        index++;
        return value % maxExclusive;
    }
}
