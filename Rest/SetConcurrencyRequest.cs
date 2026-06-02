using System.Diagnostics.CodeAnalysis;

namespace MaichessBotArenaService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record SetConcurrencyRequest(int Limit);
