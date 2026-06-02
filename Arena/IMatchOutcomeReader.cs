namespace MaichessBotArenaService.Arena;

// Reads the current outcome of a match. Returns null while the match is still
// ongoing. Implemented over Match Manager's GetMatch gRPC.
internal interface IMatchOutcomeReader
{
    Task<MatchOutcome?> ReadAsync(string matchId, CancellationToken ct);
}
