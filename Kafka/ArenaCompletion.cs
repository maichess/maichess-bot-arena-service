using System.Diagnostics.CodeAnalysis;
using MaichessBotArenaService.Arena;

namespace MaichessBotArenaService.Kafka;

// A decided match completion: the tracked arena game to finalize plus the
// outcome to record on it. Produced by ArenaMatchCompletionProjection and
// handed to CollectionService.HandleFinishedGameAsync by the consumer shell.
// Excluded from coverage as a pure data record (mirrors match-manager's
// ProjectorOutcome).
[ExcludeFromCodeCoverage]
internal sealed record ArenaCompletion(ArenaGame Game, MatchOutcome Outcome);
