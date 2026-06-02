namespace MaichessBotArenaService.Arena;

internal abstract record CreateCollectionResult
{
    internal sealed record Success(ArenaCollection Collection) : CreateCollectionResult;

    internal sealed record InvalidInput(string Message) : CreateCollectionResult;
}
