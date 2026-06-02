using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Tests.Support;

// Scenario-scoped state shared between the FEN-list and setup-expansion steps.
internal sealed class ExpansionContext
{
    internal IReadOnlyList<string>? NormalizedPool { get; set; }

    internal string? Label { get; set; }

    internal IReadOnlyList<ExpandedGame>? Games { get; set; }

    // Maps a comma-separated list of FEN tokens to FEN strings: "standard" (any
    // case) becomes the standard position; every other token is used verbatim as
    // a (synthetic) custom FEN. Used where the input is already normalized.
    internal static IReadOnlyList<string> MapFens(string csv) =>
        [.. csv.Split(',').Select(token => MapFen(token.Trim()))];

    internal static string MapFen(string token) =>
        string.Equals(token, "standard", StringComparison.OrdinalIgnoreCase)
            ? FenList.StandardFen
            : token;
}
