using Maichess.Events.V1;
using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Kafka;
using Xunit;

namespace MaichessBotArenaService.Tests;

public sealed class ArenaMatchCompletionProjectionTests
{
    private static ArenaGame RunningGame(string matchId = "m1") =>
        new() { Id = "g1", CollectionId = "c1", MatchId = matchId, Status = "running" };

    private static MatchEvent Ended(MatchStatus status, string matchId = "m1") =>
        new()
        {
            AggregateId = matchId,
            MatchEnded = new MatchEnded
            {
                Status = status,
                WhiteTimeMs = 4200,
                BlackTimeMs = 3100,
                FinalFen = "8/8/8/8/8/8/8/K6k w - - 0 1",
            },
        };

    [Theory]
    [InlineData(MatchStatus.WhiteWon)]
    [InlineData(MatchStatus.BlackWon)]
    [InlineData(MatchStatus.Draw)]
    public void Decide_MatchEndedForTrackedGame_ProducesCompletionWithMappedOutcome(MatchStatus status)
    {
        GameOutcome expected = status switch
        {
            MatchStatus.WhiteWon => GameOutcome.WhiteWon,
            MatchStatus.BlackWon => GameOutcome.BlackWon,
            _ => GameOutcome.Draw,
        };
        ArenaGame game = RunningGame();

        ArenaCompletion? completion = ArenaMatchCompletionProjection.Decide(Ended(status), game);

        Assert.NotNull(completion);
        Assert.Same(game, completion!.Game);
        Assert.Equal(expected, completion.Outcome.Outcome);
    }

    [Fact]
    public void Decide_MatchEnded_OutcomeCarriesFinalClocksAndFen()
    {
        // MatchEnded carries the final clocks and FEN (contracts >= 0.11.0), so
        // the outcome preserves them for the clock/material tie-breaks.
        ArenaCompletion? completion =
            ArenaMatchCompletionProjection.Decide(Ended(MatchStatus.WhiteWon), RunningGame());

        Assert.NotNull(completion);
        Assert.Equal(4200, completion!.Outcome.WhiteTimeMs);
        Assert.Equal(3100, completion.Outcome.BlackTimeMs);
        Assert.Equal("8/8/8/8/8/8/8/K6k w - - 0 1", completion.Outcome.FinalFen);
    }

    [Fact]
    public void Decide_UntrackedMatch_IsNoOp()
    {
        ArenaCompletion? completion =
            ArenaMatchCompletionProjection.Decide(Ended(MatchStatus.WhiteWon), trackedGame: null);

        Assert.Null(completion);
    }

    [Fact]
    public void Decide_NonMatchEndedEvent_IsNoOp()
    {
        MatchEvent moveApplied = new() { AggregateId = "m1", MoveApplied = new MoveApplied() };

        ArenaCompletion? completion = ArenaMatchCompletionProjection.Decide(moveApplied, RunningGame());

        Assert.Null(completion);
    }

    [Theory]
    [InlineData(MatchStatus.Unspecified)]
    [InlineData(MatchStatus.Ongoing)]
    public void Decide_MatchEndedWithNonTerminalStatus_IsNoOp(MatchStatus status)
    {
        ArenaCompletion? completion = ArenaMatchCompletionProjection.Decide(Ended(status), RunningGame());

        Assert.Null(completion);
    }
}
