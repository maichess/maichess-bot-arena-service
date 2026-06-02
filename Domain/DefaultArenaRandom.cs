namespace MaichessBotArenaService.Domain;

// Seeded production implementation of IArenaRandom. A per-collection seed makes
// a tournament's seeding and color choices reproducible.
internal sealed class DefaultArenaRandom(int seed) : IArenaRandom
{
    private readonly Random random = new(seed);

    public int Next(int maxExclusive) => random.Next(maxExclusive);
}
