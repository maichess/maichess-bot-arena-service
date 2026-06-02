namespace MaichessBotArenaService.Domain;

// Sums standard piece values from a FEN's placement field. Used only for the
// final material tie-break, where what matters is the relative heavy/minor
// material left on the board; kings carry no value.
internal static class Material
{
    // Returns (whiteValue, blackValue) using P=1, N=3, B=3, R=5, Q=9.
    internal static (int White, int Black) Balance(string fen)
    {
        string placement = fen.Split(' ')[0];
        int white = 0;
        int black = 0;

        foreach (char piece in placement)
        {
            int value = char.ToUpperInvariant(piece) switch
            {
                'P' => 1,
                'N' => 3,
                'B' => 3,
                'R' => 5,
                'Q' => 9,
                _ => 0,
            };

            if (value == 0)
            {
                continue;
            }

            if (char.IsUpper(piece))
            {
                white += value;
            }
            else
            {
                black += value;
            }
        }

        return (white, black);
    }
}
