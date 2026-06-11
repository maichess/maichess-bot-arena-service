using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;

namespace MaichessBotArenaService.Arena;

// Background loop that observes running arena games and feeds completions back
// into the orchestration. Excluded from coverage: a fire-and-forget loop over
// live dependencies; the decisions it drives live in CollectionService.
[ExcludeFromCodeCoverage]
internal sealed class CollectionPoller(
    Persistence.IArenaStore store, IMatchOutcomeReader reader, CollectionService service)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<ArenaGame> games = await store.ListRunningGamesAsync(stoppingToken);

            // Fan out all gRPC outcome reads in parallel — each call is independent
            // and read-only. Sequential reads were the main source of poll-cycle
            // saturation for large tournaments (N*RTT per 2-second tick).
            (ArenaGame Game, MatchOutcome? Outcome)[] results = await Task.WhenAll(
                games.Select(async game =>
                {
                    MatchOutcome? outcome = await reader.ReadAsync(game.MatchId, stoppingToken);
                    return (game, outcome);
                }));

            // Apply completions sequentially — HandleFinishedGameAsync mutates
            // collection state and launching logic; concurrent mutations to the
            // same collection would race on AdvanceAsync.
            foreach ((ArenaGame game, MatchOutcome? outcome) in results)
            {
                if (outcome is not null)
                {
                    await service.HandleFinishedGameAsync(game, outcome, stoppingToken);
                }
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
