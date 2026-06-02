namespace MaichessBotArenaService.Arena;

internal enum TournamentColorMode
{
    // Each chosen FEN is played twice, swapping colors.
    BothColors,

    // Each chosen FEN is played once with colors assigned randomly.
    Random,
}
