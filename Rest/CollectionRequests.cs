using System.Diagnostics.CodeAnalysis;

namespace MaichessBotArenaService.Rest;

// Request bodies for POST /collections. Bound from snake_case JSON; exactly one
// config sub-object selects the setup type.
[ExcludeFromCodeCoverage]
internal static class CollectionRequests
{
    internal sealed record CreateCollection(
        string? Name, Tournament? Tournament, Matrix? Matrix, Single? Single);

    internal sealed record Tournament(
        IReadOnlyList<string>? BotIds,
        IReadOnlyList<string>? FenList,
        int FensPerStage,
        string? ColorMode,
        string? TimeFormatId);

    internal sealed record Matrix(
        IReadOnlyList<string>? BotIds,
        IReadOnlyList<string>? FenList,
        int GamesPerFen,
        string? ColorMode,
        string? TimeFormatId);

    internal sealed record Single(
        string? WhiteBotId,
        string? BlackBotId,
        IReadOnlyList<string>? FenList,
        int GamesPerFen,
        bool KeepSwitchingColors,
        string? TimeFormatId);
}
