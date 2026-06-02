using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Maichess.Database.V1;
using MaichessBotArenaService.Arena;
using MaichessBotArenaService.Domain;

namespace MaichessBotArenaService.Persistence;

// Database-service-backed store for the arena-db domain. Excluded from coverage:
// it is a thin adapter over the generic CRUD gRPC contract and requires a live
// database service. List ordering and string-list fields are handled here so the
// rest of the service works with plain domain models.
[ExcludeFromCodeCoverage]
internal sealed class ArenaStore(Database.DatabaseClient db) : IArenaStore
{
    private const string SettingsCollection = "settings";
    private const string CollectionsCollection = "collections";
    private const string GamesCollection = "games";
    private const string ConcurrencyKey = "concurrency_limit";

    public async Task<int?> GetConcurrencyLimitAsync(CancellationToken ct)
    {
        Struct? record = await FindConcurrencyRecordAsync(ct);
        return record is null ? null : (int)record.Fields["value"].NumberValue;
    }

    public async Task SetConcurrencyLimitAsync(int limit, CancellationToken ct)
    {
        Struct? existing = await FindConcurrencyRecordAsync(ct);

        if (existing is null)
        {
            Struct record = new();
            record.Fields["key"] = Value.ForString(ConcurrencyKey);
            record.Fields["value"] = Value.ForNumber(limit);
            await db.InsertAsync(new InsertRequest { Collection = SettingsCollection, Record = record }, cancellationToken: ct);
            return;
        }

        Struct fields = new();
        fields.Fields["value"] = Value.ForNumber(limit);
        await db.UpdateAsync(
            new UpdateRequest { Collection = SettingsCollection, Id = existing.Fields["id"].StringValue, Fields = fields },
            cancellationToken: ct);
    }

    public async Task<ArenaCollection> InsertCollectionAsync(ArenaCollection collection, CancellationToken ct)
    {
        InsertResponse response = await db.InsertAsync(
            new InsertRequest { Collection = CollectionsCollection, Record = ToStruct(collection) },
            cancellationToken: ct);
        return ToCollection(response.Record);
    }

    public async Task UpdateCollectionAsync(ArenaCollection collection, CancellationToken ct) =>
        await db.UpdateAsync(
            new UpdateRequest { Collection = CollectionsCollection, Id = collection.Id, Fields = ToStruct(collection) },
            cancellationToken: ct);

    public async Task<ArenaCollection?> GetCollectionAsync(string id, CancellationToken ct)
    {
        try
        {
            GetResponse response = await db.GetAsync(
                new GetRequest { Collection = CollectionsCollection, Id = id }, cancellationToken: ct);
            return ToCollection(response.Record);
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ArenaCollection>> ListCollectionsAsync(
        string? status, int limit, int offset, CancellationToken ct)
    {
        ListRequest request = new() { Collection = CollectionsCollection };
        if (status is not null)
        {
            request.Filter = new Struct();
            request.Filter.Fields["status"] = Value.ForString(status);
        }

        ListResponse response = await db.ListAsync(request, cancellationToken: ct);
        return
        [
            .. response.Records
                .Select(ToCollection)
                .OrderByDescending(collection => collection.CreatedAtMs)
                .Skip(offset)
                .Take(limit),
        ];
    }

    public async Task<ArenaGame> InsertGameAsync(ArenaGame game, CancellationToken ct)
    {
        InsertResponse response = await db.InsertAsync(
            new InsertRequest { Collection = GamesCollection, Record = ToStruct(game) }, cancellationToken: ct);
        return ToGame(response.Record);
    }

    public async Task UpdateGameAsync(ArenaGame game, CancellationToken ct) =>
        await db.UpdateAsync(
            new UpdateRequest { Collection = GamesCollection, Id = game.Id, Fields = ToStruct(game) },
            cancellationToken: ct);

    public async Task<IReadOnlyList<ArenaGame>> ListGamesAsync(string collectionId, CancellationToken ct)
    {
        Struct filter = new();
        filter.Fields["collection_id"] = Value.ForString(collectionId);
        ListResponse response = await db.ListAsync(
            new ListRequest { Collection = GamesCollection, Filter = filter }, cancellationToken: ct);
        return [.. response.Records.Select(ToGame).OrderBy(game => game.Order)];
    }

    public async Task<int> CountRunningGamesAsync(CancellationToken ct)
    {
        Struct filter = new();
        filter.Fields["status"] = Value.ForString("running");
        CountResponse response = await db.CountAsync(
            new CountRequest { Collection = GamesCollection, Filter = filter }, cancellationToken: ct);
        return (int)response.Count;
    }

    public async Task<IReadOnlyList<ArenaGame>> ListRunningGamesAsync(CancellationToken ct)
    {
        Struct filter = new();
        filter.Fields["status"] = Value.ForString("running");
        ListResponse response = await db.ListAsync(
            new ListRequest { Collection = GamesCollection, Filter = filter }, cancellationToken: ct);
        return [.. response.Records.Select(ToGame)];
    }

    private async Task<Struct?> FindConcurrencyRecordAsync(CancellationToken ct)
    {
        Struct filter = new();
        filter.Fields["key"] = Value.ForString(ConcurrencyKey);
        ListResponse response = await db.ListAsync(
            new ListRequest { Collection = SettingsCollection, Filter = filter, Limit = 1 }, cancellationToken: ct);
        return response.Records.Count > 0 ? response.Records[0] : null;
    }

    private static Struct ToStruct(ArenaCollection collection)
    {
        Struct s = new();
        s.Fields["name"] = Value.ForString(collection.Name);
        s.Fields["kind"] = Value.ForString(collection.Kind.ToString());
        s.Fields["created_by"] = Value.ForString(collection.CreatedBy);
        s.Fields["status"] = Value.ForString(collection.Status);
        s.Fields["created_at_ms"] = Value.ForNumber(collection.CreatedAtMs);
        s.Fields["finished_at_ms"] = Value.ForNumber(collection.FinishedAtMs);
        s.Fields["bot_ids"] = Value.ForString(JsonSerializer.Serialize(collection.BotIds));
        s.Fields["white_bot_id"] = Value.ForString(collection.WhiteBotId);
        s.Fields["black_bot_id"] = Value.ForString(collection.BlackBotId);
        s.Fields["fen_list"] = Value.ForString(JsonSerializer.Serialize(collection.FenList));
        s.Fields["games_per_fen"] = Value.ForNumber(collection.GamesPerFen);
        s.Fields["fens_per_stage"] = Value.ForNumber(collection.FensPerStage);
        s.Fields["color_mode"] = Value.ForString(collection.ColorMode.ToString());
        s.Fields["keep_switching_colors"] = Value.ForBool(collection.KeepSwitchingColors);
        s.Fields["time_format_id"] = Value.ForString(collection.TimeFormat.Id);
        s.Fields["time_format_base_ms"] = Value.ForNumber(collection.TimeFormat.BaseMs);
        s.Fields["time_format_increment_ms"] = Value.ForNumber(collection.TimeFormat.IncrementMs);
        s.Fields["time_format_category"] = Value.ForString(collection.TimeFormat.Category);
        s.Fields["seeded"] = Value.ForString(JsonSerializer.Serialize(collection.Seeded));
        s.Fields["winner_bot_id"] = Value.ForString(collection.WinnerBotId);
        return s;
    }

    private static ArenaCollection ToCollection(Struct s) => new()
    {
        Id = s.Fields["id"].StringValue,
        Name = s.Fields["name"].StringValue,
        Kind = System.Enum.Parse<SetupKind>(s.Fields["kind"].StringValue),
        CreatedBy = s.Fields["created_by"].StringValue,
        Status = s.Fields["status"].StringValue,
        CreatedAtMs = (long)s.Fields["created_at_ms"].NumberValue,
        FinishedAtMs = (long)s.Fields["finished_at_ms"].NumberValue,
        BotIds = JsonSerializer.Deserialize<List<string>>(s.Fields["bot_ids"].StringValue) ?? [],
        WhiteBotId = s.Fields["white_bot_id"].StringValue,
        BlackBotId = s.Fields["black_bot_id"].StringValue,
        FenList = JsonSerializer.Deserialize<List<string>>(s.Fields["fen_list"].StringValue) ?? [],
        GamesPerFen = (int)s.Fields["games_per_fen"].NumberValue,
        FensPerStage = (int)s.Fields["fens_per_stage"].NumberValue,
        ColorMode = System.Enum.Parse<TournamentColorMode>(s.Fields["color_mode"].StringValue),
        KeepSwitchingColors = s.Fields["keep_switching_colors"].BoolValue,
        TimeFormat = new TimeFormatInfo(
            s.Fields["time_format_id"].StringValue,
            (long)s.Fields["time_format_base_ms"].NumberValue,
            (long)s.Fields["time_format_increment_ms"].NumberValue,
            s.Fields["time_format_category"].StringValue),
        Seeded = JsonSerializer.Deserialize<List<string>>(s.Fields["seeded"].StringValue) ?? [],
        WinnerBotId = s.Fields["winner_bot_id"].StringValue,
    };

    private static Struct ToStruct(ArenaGame game)
    {
        Struct s = new();
        s.Fields["collection_id"] = Value.ForString(game.CollectionId);
        s.Fields["order"] = Value.ForNumber(game.Order);
        s.Fields["round"] = Value.ForNumber(game.Round);
        s.Fields["pairing"] = Value.ForNumber(game.Pairing);
        s.Fields["match_id"] = Value.ForString(game.MatchId);
        s.Fields["white_bot_id"] = Value.ForString(game.WhiteBotId);
        s.Fields["black_bot_id"] = Value.ForString(game.BlackBotId);
        s.Fields["fen"] = Value.ForString(game.Fen);
        s.Fields["fen_label"] = Value.ForString(game.FenLabel);
        s.Fields["status"] = Value.ForString(game.Status);
        s.Fields["outcome"] = Value.ForString(game.Outcome.ToString());
        s.Fields["white_time_ms"] = Value.ForNumber(game.WhiteTimeMs);
        s.Fields["black_time_ms"] = Value.ForNumber(game.BlackTimeMs);
        s.Fields["final_fen"] = Value.ForString(game.FinalFen);
        return s;
    }

    private static ArenaGame ToGame(Struct s) => new()
    {
        Id = s.Fields["id"].StringValue,
        CollectionId = s.Fields["collection_id"].StringValue,
        Order = (int)s.Fields["order"].NumberValue,
        Round = (int)s.Fields["round"].NumberValue,
        Pairing = (int)s.Fields["pairing"].NumberValue,
        MatchId = s.Fields["match_id"].StringValue,
        WhiteBotId = s.Fields["white_bot_id"].StringValue,
        BlackBotId = s.Fields["black_bot_id"].StringValue,
        Fen = s.Fields["fen"].StringValue,
        FenLabel = s.Fields["fen_label"].StringValue,
        Status = s.Fields["status"].StringValue,
        Outcome = System.Enum.Parse<GameOutcome>(s.Fields["outcome"].StringValue),
        WhiteTimeMs = (long)s.Fields["white_time_ms"].NumberValue,
        BlackTimeMs = (long)s.Fields["black_time_ms"].NumberValue,
        FinalFen = s.Fields["final_fen"].StringValue,
    };
}
