using System.Diagnostics.CodeAnalysis;
using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Arena;

// Production randomness: a fresh seed per setup for seeding, and a deterministic
// per-stage seed so re-evaluating a stage always yields the same coin flip.
// Excluded from coverage — trivial wiring around the tested DeterministicSeed
// and DefaultArenaRandom.
[ExcludeFromCodeCoverage]
internal sealed class DefaultArenaRandomProvider : IArenaRandomProvider
{
    public IArenaRandom ForSeeding() => new DefaultArenaRandom(Guid.NewGuid().GetHashCode());

    public IArenaRandom ForStage(string collectionId, int round, int pairing) =>
        new DefaultArenaRandom(DeterministicSeed.From(collectionId, round, pairing));
}
