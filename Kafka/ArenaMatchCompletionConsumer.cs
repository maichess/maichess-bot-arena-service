using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Maichess.Events.V1;
using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Persistence;

namespace MaichessBotArenaService.Kafka;

// Live-Kafka shell that replaces the 2-second CollectionPoller: it consumes
// match.events.v1 and reacts to MatchEnded for games the arena spawned, feeding
// each completion into CollectionService instead of polling match-manager over
// gRPC every tick. Excluded from coverage and mutation like the platform's other
// consumer shells — the decision lives in the pure, fully-tested
// ArenaMatchCompletionProjection; this class only moves bytes.
//
// At-least-once (EnableAutoCommit): reprocessing a MatchEnded after a crash is a
// no-op because the game is no longer "running", so TryGetRunningGameAsync
// returns null and the projection declines it.
[ExcludeFromCodeCoverage]
internal sealed class ArenaMatchCompletionConsumer(
    IArenaStore store, CollectionService service, ILogger<ArenaMatchCompletionConsumer> logger)
    : BackgroundService
{
    private const string Topic = "match.events.v1";
    private const string GroupId = "bot-arena-completion";

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

#pragma warning disable CA1031 // Resilient consumer loop: log and continue on per-message failures.
    private void ConsumeLoop(CancellationToken ct)
    {
        string bootstrap = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP") ?? "kafka:9092";

        using IConsumer<string, MatchEvent> consumer = new ConsumerBuilder<string, MatchEvent>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
        })
            .SetValueDeserializer(ProtobufEventSerdes.Deserializer<MatchEvent>())
            .Build();
        consumer.Subscribe(Topic);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                ConsumeResult<string, MatchEvent>? result = consumer.Consume(ct);

                // Pre-filter to MatchEnded so a per-move event never triggers a
                // store lookup; the projection re-checks the type independently.
                if (result?.Message?.Value is { } ev && ev.PayloadCase == MatchEvent.PayloadOneofCase.MatchEnded)
                {
                    HandleAsync(ev, ct).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogWarning(ex, "Failed to consume a match event; skipping");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to handle a match completion; skipping");
            }
        }

        consumer.Close();
    }
#pragma warning restore CA1031

    private async Task HandleAsync(MatchEvent ev, CancellationToken ct)
    {
        ArenaGame? game = await store.TryGetRunningGameAsync(ev.AggregateId, ct);
        ArenaCompletion? completion = ArenaMatchCompletionProjection.Decide(ev, game);
        if (completion is not null)
        {
            await service.HandleFinishedGameAsync(completion.Game, completion.Outcome, ct);
        }
    }
}
