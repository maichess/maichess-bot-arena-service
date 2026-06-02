namespace MaichessBotArenaService.Arena;

// A stable seed derived from a string and two ints (FNV-1a), so a tournament's
// per-stage tie-break RNG is reproducible across re-evaluations (unlike
// string.GetHashCode, which is randomized per process).
internal static class DeterministicSeed
{
    internal static int From(string text, int a, int b)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (char c in text)
            {
                hash = (hash ^ c) * 16777619u;
            }

            hash = (hash ^ (uint)a) * 16777619u;
            hash = (hash ^ (uint)b) * 16777619u;
            return (int)hash;
        }
    }
}
