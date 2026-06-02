namespace MaichessBotArenaService.Domain;

// Pure evaluation of a tournament bracket from its stored seeding and the games
// played so far. The whole bracket is derived (not stored): each stage's winner
// is recomputed from its games with a deterministic per-stage RNG, so the result
// is stable across calls. Reports the champion when decided and the pairings that
// still need their games created.
internal static class TournamentBracket
{
    internal sealed record StagePairing(
        string BotA, string? BotB, bool Bye, string? WinnerBotId, IReadOnlyList<GameRecord> Games);

    internal sealed record BracketRound(int RoundNumber, IReadOnlyList<StagePairing> Pairings);

    internal sealed record PendingStage(int Round, int PairingIndex, string BotA, string BotB);

    internal sealed record BracketState(
        IReadOnlyList<BracketRound> Rounds, string? Champion, IReadOnlyList<PendingStage> Pending);

    // stageGames(round, pairingIndex) returns the games played for that stage
    // (empty if not created yet). rngFor(round, pairingIndex) supplies the
    // tie-break coin flip for that stage.
    internal static BracketState Evaluate(
        IReadOnlyList<string> seeded,
        Func<int, int, IReadOnlyList<GameRecord>> stageGames,
        Func<int, int, IArenaRandom> rngFor)
    {
        List<BracketRound> rounds = [];
        List<PendingStage> pending = [];
        string? champion = null;

        IReadOnlyList<Pairing> roundPairings = Bracket.FirstRound(seeded);
        int round = 0;

        while (true)
        {
            List<StagePairing> evaluated = [];
            List<string?> winners = [];
            bool frontierResolved = true;

            for (int i = 0; i < roundPairings.Count; i++)
            {
                Pairing pairing = roundPairings[i];

                if (pairing.IsBye)
                {
                    evaluated.Add(new StagePairing(pairing.BotA, null, true, pairing.BotA, []));
                    winners.Add(pairing.BotA);
                    continue;
                }

                IReadOnlyList<GameRecord> games = stageGames(round, i);

                if (games.Count == 0)
                {
                    pending.Add(new PendingStage(round, i, pairing.BotA, pairing.BotB!));
                    evaluated.Add(new StagePairing(pairing.BotA, pairing.BotB, false, null, games));
                    winners.Add(null);
                    frontierResolved = false;
                }
                else if (games.All(game => game.Outcome != GameOutcome.Ongoing))
                {
                    string winner = TieBreak.DecideWinner(pairing.BotA, pairing.BotB!, games, rngFor(round, i));
                    evaluated.Add(new StagePairing(pairing.BotA, pairing.BotB, false, winner, games));
                    winners.Add(winner);
                }
                else
                {
                    evaluated.Add(new StagePairing(pairing.BotA, pairing.BotB, false, null, games));
                    winners.Add(null);
                    frontierResolved = false;
                }
            }

            rounds.Add(new BracketRound(round + 1, evaluated));

            if (!frontierResolved)
            {
                break;
            }

            if (winners.Count == 1)
            {
                champion = winners[0];
                break;
            }

            roundPairings = Bracket.NextRound([.. winners.Select(winner => winner!)]);
            round++;
        }

        return new BracketState(rounds, champion, pending);
    }
}
