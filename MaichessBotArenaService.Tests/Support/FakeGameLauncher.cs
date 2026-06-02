using MaichessBotArenaService.Arena;

namespace MaichessBotArenaService.Tests.Support;

internal sealed class FakeGameLauncher : IGameLauncher
{
    private int seq;

    internal List<LaunchedGame> Launches { get; } = [];

    public Task<string> LaunchAsync(
        string whiteBotId, string blackBotId, string timeFormatId, string startFen, string createdByUserId, CancellationToken ct)
    {
        Launches.Add(new LaunchedGame(whiteBotId, blackBotId, timeFormatId, startFen, createdByUserId));
        return Task.FromResult($"match-{++seq}");
    }

    internal sealed record LaunchedGame(
        string WhiteBotId, string BlackBotId, string TimeFormatId, string StartFen, string CreatedBy);
}
