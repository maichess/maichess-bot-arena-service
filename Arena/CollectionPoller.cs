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
            foreach (ArenaGame game in await store.ListRunningGamesAsync(stoppingToken))
            {
                MatchOutcome? outcome = await reader.ReadAsync(game.MatchId, stoppingToken);
                if (outcome is not null)
                {
                    await service.HandleFinishedGameAsync(game, outcome, stoppingToken);
                }
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
