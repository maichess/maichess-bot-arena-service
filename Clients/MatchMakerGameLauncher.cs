using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MaichessBotArenaService.Arena;

namespace MaichessBotArenaService.Clients;

// IGameLauncher over Match Maker's POST /matches/bot-vs-bot REST path, signing a
// short-lived JWT for the setup creator. Excluded from coverage: a thin adapter
// requiring a live match maker.
[ExcludeFromCodeCoverage]
internal sealed class MatchMakerGameLauncher(HttpClient http, ServiceTokenMinter minter) : IGameLauncher
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<string> LaunchAsync(
        string whiteBotId, string blackBotId, string timeFormatId, string startFen, string createdByUserId, CancellationToken ct)
    {
        string token = minter.Mint(createdByUserId, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));

        using HttpRequestMessage request = new(HttpMethod.Post, "/matches/bot-vs-bot")
        {
            Content = JsonContent.Create(
                new LaunchBody(whiteBotId, blackBotId, timeFormatId, startFen), options: Json),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        BotMatchResult? result = await response.Content.ReadFromJsonAsync<BotMatchResult>(Json, ct);
        return result!.MatchId;
    }

    private sealed record LaunchBody(
        [property: JsonPropertyName("white_bot_id")] string WhiteBotId,
        [property: JsonPropertyName("black_bot_id")] string BlackBotId,
        [property: JsonPropertyName("time_format_id")] string TimeFormatId,
        [property: JsonPropertyName("start_fen")] string StartFen);

    private sealed record BotMatchResult(
        [property: JsonPropertyName("match_id")] string MatchId);
}
