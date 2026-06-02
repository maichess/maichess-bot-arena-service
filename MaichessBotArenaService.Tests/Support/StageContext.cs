using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Tests.Support;

// Scenario-scoped state for the scoring, tie-break, and bracket steps.
internal sealed class StageContext
{
    internal List<GameRecord> Games { get; } = [];

    internal IArenaRandom Random { get; set; } = new FakeArenaRandom(0);

    internal string? Winner { get; set; }

    internal IReadOnlyList<string>? Seeded { get; set; }

    internal IReadOnlyList<Pairing>? Pairings { get; set; }

    internal void AddGame(string white, string black, string outcome, long whiteMs, long blackMs, string fen) =>
        Games.Add(new GameRecord(white, black, ParseOutcome(outcome), whiteMs, blackMs, fen));

    internal static GameOutcome ParseOutcome(string text) => text switch
    {
        "white_won" => GameOutcome.WhiteWon,
        "black_won" => GameOutcome.BlackWon,
        "draw" => GameOutcome.Draw,
        "ongoing" => GameOutcome.Ongoing,
        _ => throw new ArgumentException($"unknown outcome '{text}'", nameof(text)),
    };
}
