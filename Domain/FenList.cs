namespace MaichessBotArenaService.Domain;

// Normalization and labelling for a configured FEN pool.
internal static class FenList
{
    internal const string StandardFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    // Normalizes a configured FEN pool: blank entries are dropped, a literal
    // "standard" (any case) maps to the standard start, and an empty result
    // collapses to a single standard position.
    internal static IReadOnlyList<string> Normalize(IEnumerable<string> fenList)
    {
        List<string> cleaned = [.. fenList
            .Select(fen => fen.Trim())
            .Where(fen => fen.Length > 0)
            .Select(fen => string.Equals(fen, "standard", StringComparison.OrdinalIgnoreCase)
                ? StandardFen
                : fen)];

        return cleaned.Count == 0 ? [StandardFen] : cleaned;
    }

    // Display label for the FEN at a 0-based index within a normalized pool.
    internal static string Label(int index, string fen) =>
        fen == StandardFen ? "Standard" : $"FEN {index + 1}";
}
