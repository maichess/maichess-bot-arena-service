using System.Diagnostics.CodeAnalysis;
using Maichess.MatchManager.V1;
using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Clients;

// IMatchOutcomeReader over Match Manager's GetMatch gRPC. Excluded from coverage:
// a thin adapter requiring a live match manager. The MatchStatus mapping is
// exhaustive — ONGOING/UNSPECIFIED mean "not finished yet" (null).
[ExcludeFromCodeCoverage]
internal sealed class MatchManagerOutcomeReader(Matches.MatchesClient matches) : IMatchOutcomeReader
{
    public async Task<MatchOutcome?> ReadAsync(string matchId, CancellationToken ct)
    {
        GetMatchResponse response = await matches.GetMatchAsync(
            new GetMatchRequest { MatchId = matchId }, cancellationToken: ct);
        Match match = response.Match;

        GameOutcome? outcome = match.Status switch
        {
            MatchStatus.WhiteWon => GameOutcome.WhiteWon,
            MatchStatus.BlackWon => GameOutcome.BlackWon,
            MatchStatus.Draw => GameOutcome.Draw,
            _ => null,
        };

        return outcome is null
            ? null
            : new MatchOutcome(outcome.Value, match.WhiteTimeMs, match.BlackTimeMs, match.CurrentFen);
    }
}
