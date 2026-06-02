using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Tests.Support;

// Deterministic randomness for tests: seeding produces the identity order (so
// bracket pairings follow the input bot order) and per-stage coin flips favour
// the first bot (never reached when stages are decisive).
internal sealed class FakeArenaRandomProvider : IArenaRandomProvider
{
    public IArenaRandom ForSeeding() => new IdentityShuffleRandom();

    public IArenaRandom ForStage(string collectionId, int round, int pairing) => new FakeArenaRandom(0);
}

// Next(max) == max - 1, which makes Fisher-Yates a no-op so seeding keeps input
// order.
internal sealed class IdentityShuffleRandom : IArenaRandom
{
    public int Next(int maxExclusive) => maxExclusive - 1;
}
