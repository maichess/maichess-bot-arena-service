namespace MaichessBotArenaService.Domain;

// Single-elimination bracket construction. Seeding is random; a non-power-of-2
// field is reduced to a power of 2 in the first round by giving the leading
// seeds byes.
internal static class Bracket
{
    // Randomly seeds the bots into bracket order via a Fisher-Yates shuffle.
    internal static IReadOnlyList<string> Seed(IReadOnlyList<string> botIds, IArenaRandom random)
    {
        List<string> seeded = [.. botIds];
        for (int i = seeded.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (seeded[i], seeded[j]) = (seeded[j], seeded[i]);
        }

        return seeded;
    }

    // First-round pairings: the leading `byes` seeds advance directly and the
    // remaining (even-sized) field plays sequential pairings, leaving a clean
    // power-of-2 number of bots for the next round.
    internal static IReadOnlyList<Pairing> FirstRound(IReadOnlyList<string> seeded)
    {
        int byes = NextPowerOfTwo(seeded.Count) - seeded.Count;
        List<Pairing> pairings = [];

        for (int i = 0; i < byes; i++)
        {
            pairings.Add(new Pairing(seeded[i], null));
        }

        for (int i = byes; i < seeded.Count; i += 2)
        {
            pairings.Add(new Pairing(seeded[i], seeded[i + 1]));
        }

        return pairings;
    }

    // Pairs an even list of advancing bots sequentially for the next round.
    internal static IReadOnlyList<Pairing> NextRound(IReadOnlyList<string> advancing)
    {
        List<Pairing> pairings = [];
        for (int i = 0; i < advancing.Count; i += 2)
        {
            pairings.Add(new Pairing(advancing[i], advancing[i + 1]));
        }

        return pairings;
    }

    // Smallest power of two greater than or equal to value (value >= 1).
    internal static int NextPowerOfTwo(int value)
    {
        int power = 1;
        while (power < value)
        {
            power *= 2;
        }

        return power;
    }
}
