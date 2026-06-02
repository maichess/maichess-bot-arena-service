using System.Globalization;
using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Tests.Support;
using Reqnroll;

namespace MaichessBotArenaService.Tests.StepDefinitions;

[Binding]
internal sealed class StageGameSteps(StageContext context)
{
    [Given("the stage games:")]
    public void GivenTheStageGames(DataTable table)
    {
        bool hasWhiteMs = table.Header.Contains("whiteMs");
        bool hasBlackMs = table.Header.Contains("blackMs");
        bool hasFen = table.Header.Contains("fen");

        foreach (DataTableRow row in table.Rows)
        {
            long whiteMs = hasWhiteMs ? long.Parse(row["whiteMs"], CultureInfo.InvariantCulture) : 0;
            long blackMs = hasBlackMs ? long.Parse(row["blackMs"], CultureInfo.InvariantCulture) : 0;
            string fen = hasFen ? ExpansionContext.MapFen(row["fen"]) : FenList.StandardFen;
            context.AddGame(row["white"], row["black"], row["outcome"], whiteMs, blackMs, fen);
        }
    }

    [Given(@"the tie-break coin flip yields (\d+)")]
    public void GivenTheCoinFlipYields(int value) =>
        context.Random = new FakeArenaRandom(value);
}
