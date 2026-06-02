namespace MaichessBotArenaService.Domain;

// A single bracket pairing. BotB is null for a bye, where BotA advances without
// playing a game.
internal sealed record Pairing(string BotA, string? BotB)
{
    internal bool IsBye => BotB is null;
}
