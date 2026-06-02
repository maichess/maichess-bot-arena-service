namespace MaichessBotArenaService.Arena;

// Starts a bot-vs-bot game and returns its match id. Implemented over Match
// Maker's bot-vs-bot REST path.
internal interface IGameLauncher
{
    Task<string> LaunchAsync(
        string whiteBotId,
        string blackBotId,
        string timeFormatId,
        string startFen,
        string createdByUserId,
        CancellationToken ct);
}
