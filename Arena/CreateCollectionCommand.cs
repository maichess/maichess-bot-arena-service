namespace MaichessBotArenaService.Arena;

// The validated-on-arrival inputs for creating a setup. The REST layer builds
// this from the request body; CollectionService validates and normalizes it.
internal sealed record CreateCollectionCommand(
    string Name,
    string CreatedBy,
    SetupKind Kind,
    IReadOnlyList<string> BotIds,
    string WhiteBotId,
    string BlackBotId,
    IReadOnlyList<string> FenList,
    int GamesPerFen,
    int FensPerStage,
    TournamentColorMode ColorMode,
    MatrixColorMode MatrixColorMode,
    bool KeepSwitchingColors,
    string TimeFormatId);
