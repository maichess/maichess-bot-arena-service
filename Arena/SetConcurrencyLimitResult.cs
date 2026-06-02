namespace MaichessBotArenaService.Arena;

internal abstract record SetConcurrencyLimitResult
{
    internal sealed record Success(int Limit) : SetConcurrencyLimitResult;

    internal sealed record InvalidInput(string Message) : SetConcurrencyLimitResult;
}
