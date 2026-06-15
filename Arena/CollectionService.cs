using System.Diagnostics.CodeAnalysis;
using MaichessBotArenaService.Domain;
using MaichessBotArenaService.Persistence;

namespace MaichessBotArenaService.Arena;

// Orchestrates the lifecycle of a setup: validate, expand into games, launch
// them under the global concurrency cap, observe completions, advance the
// tournament bracket, and finalize. All side effects go through injected
// abstractions so the logic is testable end-to-end.
internal sealed class CollectionService(
    IArenaStore store,
    IGameLauncher launcher,
    IBotCatalog catalog,
    ArenaSettingsService settings,
    IArenaRandomProvider randomProvider,
    Func<long> clock) : IDisposable
{
    // Serializes the global capacity reconcile. Several games can finish in one
    // poll tick, and creation / limit changes can race with them; without this a
    // concurrent reconcile could read the same running count twice and launch
    // past the cap.
    private readonly SemaphoreSlim reconcileLock = new(1, 1);

    // Disposes the reconcile semaphore. The service is a singleton, so this only
    // runs at application shutdown; excluded as trivial lifecycle plumbing.
    [ExcludeFromCodeCoverage]
    public void Dispose() => reconcileLock.Dispose();

    internal async Task<CreateCollectionResult> CreateAsync(CreateCollectionCommand command, CancellationToken ct)
    {
        string? error = await ValidateAsync(command, ct);
        if (error is not null)
        {
            return new CreateCollectionResult.InvalidInput(error);
        }

        ArenaCollection collection = new()
        {
            Name = command.Name,
            Kind = command.Kind,
            CreatedBy = command.CreatedBy,
            Status = "pending",
            CreatedAtMs = clock(),
            BotIds = command.BotIds,
            WhiteBotId = command.WhiteBotId,
            BlackBotId = command.BlackBotId,
            FenList = FenList.Normalize(command.FenList),
            GamesPerFen = command.GamesPerFen,
            FensPerStage = command.FensPerStage,
            ColorMode = command.ColorMode,
            MatrixColorMode = command.MatrixColorMode,
            KeepSwitchingColors = command.KeepSwitchingColors,
            TimeFormat = TimeFormatRegistry.Resolve(command.TimeFormatId),
        };

        if (command.Kind == SetupKind.Tournament)
        {
            collection.Seeded = Bracket.Seed(command.BotIds, randomProvider.ForSeeding());
        }

        collection = await store.InsertCollectionAsync(collection, ct);

        await AdvanceAsync(collection, ct);
        await FillAvailableCapacityAsync(ct);

        return new CreateCollectionResult.Success(collection);
    }

    // Records a finished game, advances the collection, and launches more.
    internal async Task HandleFinishedGameAsync(ArenaGame game, MatchOutcome outcome, CancellationToken ct)
    {
        game.Status = "finished";
        game.Outcome = outcome.Outcome;
        game.WhiteTimeMs = outcome.WhiteTimeMs;
        game.BlackTimeMs = outcome.BlackTimeMs;
        game.FinalFen = outcome.FinalFen;
        await store.UpdateGameAsync(game, ct);

        ArenaCollection? collection = await store.GetCollectionAsync(game.CollectionId, ct);
        if (collection is not null)
        {
            await AdvanceAsync(collection, ct);
        }

        // Reconcile the whole arena, not just the finishing collection: the freed
        // slot may belong to an older collection that was queued behind the cap.
        await FillAvailableCapacityAsync(ct);
    }

    // Updates the global concurrency limit and, when it is raised, immediately
    // fills the new headroom with queued games. Lowering it launches nothing;
    // in-flight games drain naturally until running <= cap.
    internal async Task<SetConcurrencyLimitResult> SetConcurrencyLimitAsync(int limit, CancellationToken ct)
    {
        int previous = await settings.GetConcurrencyLimitAsync(ct);
        SetConcurrencyLimitResult result = await settings.SetConcurrencyLimitAsync(limit, ct);

        if (result is SetConcurrencyLimitResult.Success && limit > previous)
        {
            await FillAvailableCapacityAsync(ct);
        }

        return result;
    }

    // One global reconcile step: launch queued games up to the global cap,
    // oldest collection first (FIFO across collections, then by game order).
    // Serialized so concurrent completions / creates / limit changes can never
    // launch past the cap.
    internal async Task FillAvailableCapacityAsync(CancellationToken ct)
    {
        await reconcileLock.WaitAsync(ct);
        try
        {
            int cap = await settings.GetConcurrencyLimitAsync(ct);
            int running = await store.CountRunningGamesAsync(ct);
            IReadOnlyList<ArenaGame> pending = await store.ListPendingGamesAsync(ct);

            // Spare capacity below the global cap, clamped to what is waiting. The
            // reconcile is serialized, so running only changes inside this loop and
            // the budget stays exact.
            int budget = LaunchPlanner.LaunchableCount(cap, running, pending.Count);
            if (budget == 0)
            {
                return;
            }

            Dictionary<string, ArenaCollection> owners = await ResolveOwnersAsync(pending, ct);
            HashSet<string> promoted = [];
            int launched = 0;

            foreach (ArenaGame game in OrderFifo(pending, owners))
            {
                if (launched >= budget)
                {
                    break;
                }

                ArenaCollection collection = owners[game.CollectionId];
                await LaunchGameAsync(collection, game, ct);
                launched++;

                if (collection.Status == "pending")
                {
                    promoted.Add(collection.Id);
                }
            }

            foreach (string id in promoted)
            {
                ArenaCollection collection = owners[id];
                collection.Status = "running";
                await store.UpdateCollectionAsync(collection, ct);
            }
        }
        finally
        {
            reconcileLock.Release();
        }
    }

    internal Task<IReadOnlyList<ArenaCollection>> ListAsync(string? status, int limit, int offset, CancellationToken ct) =>
        store.ListCollectionsAsync(status, limit <= 0 ? 20 : Math.Min(limit, 100), Math.Max(offset, 0), ct);

    internal async Task<(ArenaCollection Collection, IReadOnlyList<ArenaGame> Games)?> GetWithGamesAsync(
        string id, CancellationToken ct)
    {
        ArenaCollection? collection = await store.GetCollectionAsync(id, ct);
        if (collection is null)
        {
            return null;
        }

        IReadOnlyList<ArenaGame> games = await store.ListGamesAsync(id, ct);
        return (collection, games);
    }

    // Creates any games that should now exist and finalizes the collection when
    // it is complete.
    private async Task AdvanceAsync(ArenaCollection collection, CancellationToken ct)
    {
        IReadOnlyList<ArenaGame> games = await store.ListGamesAsync(collection.Id, ct);

        if (collection.Kind == SetupKind.Tournament)
        {
            TournamentBracket.BracketState state = EvaluateBracket(collection, games);

            foreach (TournamentBracket.PendingStage stage in state.Pending)
            {
                await ExpandStageAsync(collection, stage, games.Count, ct);
                games = await store.ListGamesAsync(collection.Id, ct);
            }

            if (state.Champion is not null)
            {
                collection.WinnerBotId = state.Champion;
                FinalizeCollection(collection);
                await store.UpdateCollectionAsync(collection, ct);
            }

            return;
        }

        if (games.Count == 0)
        {
            await ExpandNonTournamentAsync(collection, ct);
        }
        else if (games.All(game => game.Status == "finished"))
        {
            FinalizeCollection(collection);
            await store.UpdateCollectionAsync(collection, ct);
        }
    }

    private async Task ExpandNonTournamentAsync(ArenaCollection collection, CancellationToken ct)
    {
        IReadOnlyList<ExpandedGame> expanded = collection.Kind == SetupKind.Single
            ? SetupExpansion.ExpandSingle(
                collection.WhiteBotId,
                collection.BlackBotId,
                collection.FenList,
                collection.GamesPerFen,
                collection.KeepSwitchingColors)
            : SetupExpansion.ExpandMatrix(
                collection.BotIds,
                collection.FenList,
                collection.GamesPerFen,
                collection.MatrixColorMode == MatrixColorMode.Random,
                randomProvider.ForStage(collection.Id, round: 0, pairing: 0));

        foreach (ExpandedGame game in expanded)
        {
            await store.InsertGameAsync(NewGame(collection.Id, game, game.Order, round: 0, pairing: 0), ct);
        }
    }

    private async Task ExpandStageAsync(
        ArenaCollection collection, TournamentBracket.PendingStage stage, int baseOrder, CancellationToken ct)
    {
        IReadOnlyList<string> stageFens =
            [.. collection.FenList.Take(Math.Min(collection.FensPerStage, collection.FenList.Count))];
        bool randomColors = collection.ColorMode == TournamentColorMode.Random;
        IArenaRandom rng = randomProvider.ForStage(collection.Id, stage.Round, stage.PairingIndex);

        IReadOnlyList<ExpandedGame> expanded =
            SetupExpansion.ExpandTournamentStage(stage.BotA, stage.BotB, stageFens, randomColors, rng);

        foreach (ExpandedGame game in expanded)
        {
            await store.InsertGameAsync(
                NewGame(collection.Id, game, baseOrder + game.Order, stage.Round, stage.PairingIndex), ct);
        }
    }

    // Resolves the owning collection of every pending game (only collections that
    // actually have queued games, so the reconcile never scans finished history).
    private async Task<Dictionary<string, ArenaCollection>> ResolveOwnersAsync(
        IReadOnlyList<ArenaGame> pending, CancellationToken ct)
    {
        Dictionary<string, ArenaCollection> owners = [];
        foreach (string collectionId in pending.Select(game => game.CollectionId).Distinct())
        {
            ArenaCollection? collection = await store.GetCollectionAsync(collectionId, ct);
            if (collection is not null)
            {
                owners[collectionId] = collection;
            }
        }

        return owners;
    }

    // FIFO across collections: oldest collection (by created_at) first, then by
    // the game's expansion order within that collection.
    private static IEnumerable<ArenaGame> OrderFifo(
        IReadOnlyList<ArenaGame> pending, Dictionary<string, ArenaCollection> owners) =>
        pending
            .Where(game => owners.ContainsKey(game.CollectionId))
            .OrderBy(game => owners[game.CollectionId].CreatedAtMs)
            .ThenBy(game => game.Order);

    private async Task LaunchGameAsync(ArenaCollection collection, ArenaGame game, CancellationToken ct)
    {
        string startFen = game.Fen == FenList.StandardFen ? string.Empty : game.Fen;
        game.MatchId = await launcher.LaunchAsync(
            game.WhiteBotId, game.BlackBotId, collection.TimeFormat.Id, startFen, collection.CreatedBy, ct);
        game.Status = "running";
        await store.UpdateGameAsync(game, ct);
    }

    private TournamentBracket.BracketState EvaluateBracket(ArenaCollection collection, IReadOnlyList<ArenaGame> games) =>
        TournamentBracket.Evaluate(
            collection.Seeded,
            (round, pairing) =>
                [.. games
                    .Where(game => game.Round == round && game.Pairing == pairing)
                    .OrderBy(game => game.Order)
                    .Select(ToGameRecord)],
            (round, pairing) => randomProvider.ForStage(collection.Id, round, pairing));

    private void FinalizeCollection(ArenaCollection collection)
    {
        collection.Status = "finished";
        collection.FinishedAtMs = clock();
    }

    private async Task<string?> ValidateAsync(CreateCollectionCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return "name is required";
        }

        if (!TimeFormatRegistry.IsKnown(command.TimeFormatId))
        {
            return "unknown time_format_id";
        }

        IReadOnlySet<string> known = await catalog.KnownBotIdsAsync(ct);

        return command.Kind switch
        {
            SetupKind.Single => ValidateSingle(command, known),
            _ => ValidateMultiBot(command, known),
        };
    }

    private static string? ValidateSingle(CreateCollectionCommand command, IReadOnlySet<string> known)
    {
        if (string.IsNullOrWhiteSpace(command.WhiteBotId) || string.IsNullOrWhiteSpace(command.BlackBotId))
        {
            return "white_bot_id and black_bot_id are required";
        }

        if (!known.Contains(command.WhiteBotId) || !known.Contains(command.BlackBotId))
        {
            return "unknown bot_id";
        }

        return command.GamesPerFen < 1 ? "games_per_fen must be at least 1" : null;
    }

    private static string? ValidateMultiBot(CreateCollectionCommand command, IReadOnlySet<string> known)
    {
        if (command.BotIds.Count < 2)
        {
            return "at least 2 bots are required";
        }

        if (command.BotIds.Any(botId => !known.Contains(botId)))
        {
            return "unknown bot_id";
        }

        if (command.Kind == SetupKind.Tournament)
        {
            return command.FensPerStage < 1 ? "fens_per_stage must be at least 1" : null;
        }

        return command.GamesPerFen < 1 ? "games_per_fen must be at least 1" : null;
    }

    private static GameRecord ToGameRecord(ArenaGame game) =>
        new(game.WhiteBotId, game.BlackBotId, game.Outcome, game.WhiteTimeMs, game.BlackTimeMs, game.FinalFen);

    private static ArenaGame NewGame(string collectionId, ExpandedGame game, int order, int round, int pairing) =>
        new()
        {
            CollectionId = collectionId,
            Order = order,
            Round = round,
            Pairing = pairing,
            WhiteBotId = game.WhiteBotId,
            BlackBotId = game.BlackBotId,
            Fen = game.Fen,
            FenLabel = game.FenLabel,
            Status = "pending",
        };
}
