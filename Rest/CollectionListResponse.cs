using System.Diagnostics.CodeAnalysis;

namespace MaichessBotArenaService.Rest;

[ExcludeFromCodeCoverage]
internal sealed record CollectionListResponse(IReadOnlyList<CollectionViews.Summary> Collections);
