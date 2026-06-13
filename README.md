# Bot Arena Service

Orchestrates bot-vs-bot **setups** (tournament / matrix / single), spawning each
game through Match Maker's bot-vs-bot path and collecting results into stored
**collections** with typed result views.

## Contracts

- **REST:** `maichess-api-contracts/rest/bot-arena.md`
- **gRPC model:** `maichess-api-contracts/protos/bot-arena-service/v1/arena.proto`
  (`maichess.bot_arena.v1`) — source of truth for the data model.
- **Consumes:** Match Maker REST `POST /matches/bot-vs-bot` (game creation),
  `match.events.v1` Kafka `MatchEnded` events (completion observation), Engine
  gRPC `ListBots` (bot validation), Database Service gRPC (`arena-db` instance,
  persistence).
- **Generated stubs:** `Maichess.PlatformProtos` (see `maichess-api-contracts/dotnet/`).

Implement against these contracts exactly. Document blockers in `CONTRACT_NOTES.md`.

## Stack

- **Runtime:** ASP.NET (net10.0), C#, nullable enabled.
- **Persistence:** `arena-db` database service via `Database.DatabaseClient` gRPC
  (`Services:DatabaseService`) — generic CRUD with `Struct` records, never a
  direct DB driver.
- **Game creation:** HTTP to Match Maker (`Services:MatchMaker`), authenticated
  with a short-lived JWT minted from the shared `Jwt:Key` for the setup creator.
- **Completion observation:** a Kafka consumer (`Kafka/ArenaMatchCompletionConsumer`,
  group `bot-arena-completion`) reacts to `MatchEnded` on `match.events.v1` for games
  the arena spawned, replacing the old 2-second `GetMatch` poll (Kafka task 18). The
  pure routing decision lives in `Kafka/ArenaMatchCompletionProjection`. `KAFKA_BOOTSTRAP`
  is injected by the deployment when `kafka.enabled`. `MatchEnded` carries the final
  clocks and FEN (contracts >= 0.11.0), so tournament tie-breaks keep full fidelity.

## Setup semantics

- **Single** — for each FEN, `games_per_fen` games; `keep_switching_colors`
  alternates colors every game (continuously across FENs), else colors are fixed.
- **Matrix** — every unordered bot pair, for each FEN, `games_per_fen` games,
  colors alternating per game continuously within a pairing.
- **Tournament** — random single-elimination bracket (seeded by an injectable
  RNG; non-power-of-2 fields get byes). Each stage plays `fens_per_stage`
  positions from the pool; `both_colors` plays each twice (colors swapped),
  `random` plays each once with RNG-chosen colors. Stage winner by tie-break
  ladder: wins → rounds-to-win → aggregate clock → aggregate material → coin flip.

The global concurrency limit caps how many arena games run at once and is a
single shared value editable by any user.

## Structure

```
MaichessBotArenaService.csproj   # main project (REST + gRPC clients)
Program.cs                       # DI wiring, middleware, routes
Domain/                          # pure, fully-tested: expansion, brackets,
                                 #   aggregation, tie-breaks, FEN normalization
MaichessBotArenaService.Tests/   # xUnit + Reqnroll, NSubstitute, coverlet
```

## Testing

- 100% line/branch/method coverage on non-excluded code is mandatory:
  ```
  dotnet test MaichessBotArenaService.Tests/MaichessBotArenaService.Tests.csproj \
    -p:CollectCoverage=true "-p:Include=[MaichessBotArenaService]*"
  ```
- Excluded (`[ExcludeFromCodeCoverage]`): REST endpoint adapters, the
  database-service repository wrapper, the fire-and-forget scheduler loop body,
  `[LoggerMessage]` partials, REST DTO records; coverlet excludes `Program.cs`,
  `*.g.cs`, `*.generated.cs`.

### Mutation testing

Stryker.NET is wired as a local tool. From the test project directory:
```
dotnet tool restore
dotnet stryker
```
Config: `MaichessBotArenaService.Tests/stryker-config.json` (exclusions mirror coverage).
