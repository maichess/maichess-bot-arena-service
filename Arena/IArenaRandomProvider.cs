using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Arena;

// Supplies randomness for tournaments. Seeding happens once and need not be
// reproducible; per-stage randomness must be stable across re-evaluations so a
// stage always resolves to the same winner.
internal interface IArenaRandomProvider
{
    IArenaRandom ForSeeding();

    IArenaRandom ForStage(string collectionId, int round, int pairing);
}
