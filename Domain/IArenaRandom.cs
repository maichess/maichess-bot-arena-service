namespace MaichessBotArenaService.Domain;

// Abstraction over randomness so bracket seeding, random color assignment, and
// tie-break coin flips are deterministic under test.
internal interface IArenaRandom
{
    // Returns a non-negative value in [0, maxExclusive).
    int Next(int maxExclusive);
}
