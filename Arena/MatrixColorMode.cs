namespace MaichessBotArenaService.Arena;

internal enum MatrixColorMode
{
    // Colors alternate deterministically per game within a pairing (default).
    Alternating,

    // Colors are assigned randomly per game via the arena RNG.
    Random,
}
