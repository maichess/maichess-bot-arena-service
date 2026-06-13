using Maichess.Events.V1;
using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Domain;
using MatchOutcome = MaichessBotArenaService.Arena.MatchOutcome;

namespace MaichessBotArenaService.Kafka;

// Pure decision core for the arena's match-completion consumer. Maps a
// match.events.v1 event plus the arena's running-game lookup to either a
// completion command (the game to finalize + its outcome) or a no-op.
//
// Only a MatchEnded carrying a terminal status, for a game the arena is
// currently running, produces a command; any other event type, an untracked
// match, or a non-terminal status is a no-op.
//
// MatchEnded carries the final clocks and FEN (contracts >= 0.11.0), so the
// outcome is mapped straight through — the clock and material tournament
// tie-breaks keep full fidelity without a synchronous GetMatch read.
internal static class ArenaMatchCompletionProjection
{
    internal static ArenaCompletion? Decide(MatchEvent ev, ArenaGame? trackedGame)
    {
        if (ev.PayloadCase != MatchEvent.PayloadOneofCase.MatchEnded || trackedGame is null)
        {
            return null;
        }

        MatchEnded ended = ev.MatchEnded;
        GameOutcome? outcome = ended.Status switch
        {
            MatchStatus.WhiteWon => GameOutcome.WhiteWon,
            MatchStatus.BlackWon => GameOutcome.BlackWon,
            MatchStatus.Draw => GameOutcome.Draw,
            _ => null,
        };

        return outcome is null
            ? null
            : new ArenaCompletion(
                trackedGame,
                new MatchOutcome(outcome.Value, ended.WhiteTimeMs, ended.BlackTimeMs, ended.FinalFen));
    }
}
