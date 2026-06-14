# Contract Notes

The arena contract (`protos/bot-arena-service/v1/arena.proto`,
`rest/bot-arena.md`) and the bot-vs-bot `start_fen` / `created_by` additions are
implemented as specified in `maichess-api-contracts` at version `0.4.0`. No open
blockers.

## Resolved deviations from `maichess-knowledge-base/tasks/implemented/04-bot-arena-service.md`

These were corrections to the prompt, agreed with the maintainer before the
`v0.4.0` contract was published:

1. **`start_fen` field number.** The prompt said add `string start_fen = 4;` to
   `CreateMatchRequest`, but field 4 is already `created_by`. Published as
   `start_fen = 5`.
2. **Invalid-FEN rejection status.** Match Manager's `CreateMatch` is gRPC, so an
   invalid `start_fen` returns `INVALID_ARGUMENT`; Match Maker translates that to
   REST `400` on `POST /matches/bot-vs-bot`.
3. **`created_by` attribution.** The bot-vs-bot path now stamps the authenticated
   caller as the match initiator (Match Maker derives it from the bearer token
   and forwards it as `CreateMatchRequest.created_by`). The arena mints a
   short-lived JWT for the setup creator so its games are attributed to them.
4. **Tournament stage semantics.** The prompt's "best-of-3 / first to 2 points"
   conflicted with "each FEN both colors". Replaced with a creator-chosen
   `fens_per_stage` plus a `both_colors` / `random` color mode, and an explicit
   tie-break ladder (wins → rounds-to-win → aggregate clock → aggregate material
   → seeded coin flip). See the knowledge-base ADR.

## Resolved — `MatchEnded` carries final clocks / FEN (contracts 0.11.0)

Since Kafka task 18 the arena learns about completions from `MatchEnded` events
on `match.events.v1` (`protos/events/v1/match_events.proto`) instead of reading
match-manager's `GetMatch` over gRPC.

`MatchEnded` originally carried only the terminal `status`, which dropped the
final clocks and FEN the previous `GetMatch` read supplied and degraded the
tournament clock (rung 3) and material (rung 4) tie-breaks. The contract was
extended (approved) in **0.11.0**: `MatchEnded` now has `white_time_ms = 9`,
`black_time_ms = 10`, and `final_fen = 11`, populated by the match-manager
projector (`Kafka/MatchEndedFactory`) from the live read model it already holds
at match end. `Kafka/ArenaMatchCompletionProjection` maps them straight into
`MatchOutcome`, so all tie-break rungs keep full fidelity with no gRPC call.
Events written before 0.11.0 leave the fields at `0` / empty (backward compatible).

## Pending publish — matrix `color_mode` (contracts 0.12.0, task 20)

`MatrixConfig` gained a `MatrixColorMode color_mode = 5` field
(`MATRIX_COLOR_MODE_ALTERNATING` default / `MATRIX_COLOR_MODE_RANDOM`) plus REST
docs for the matrix `color_mode` body field. **Handoff:** commit + tag the
contracts repo as **`v0.12.0`** and push so `Maichess.PlatformProtos` publishes,
then bump the pin platform-wide (every `*.csproj` / `build.sbt`) to `0.12.0`. This
service consumes the arena contract over **REST**, not via generated arena
proto types, so it builds and tests green at the current `0.11.0` pin — the bump
is convention/alignment only and is not a compile blocker here.

### Interpretation: `games_per_fen` stays the count authority in both modes

Task `20`'s table/labels frame matrix `color_mode` as a game-*count* switch
("Alternate ×2 / Random ×1", "the count will halve"). That conflicts with the
same spec's "Alternating (default): keep the existing behavior" and with the
established matrix semantics (existing tests assert `games_per_fen` games per
`(pair, FEN)` with alternating colors). Implementing a count change would silently
double existing alternating output and make `games_per_fen` ambiguous.

Resolved (color-strategy interpretation): **`color_mode` changes only how colors
are *assigned*, never how many games are spawned.** `games_per_fen` is the per-FEN
game count in both modes; `alternating` swaps colors deterministically (unchanged),
`random` draws each game's colors from the per-collection arena RNG. The spec's
"×2 vs ×1 / halve" framing is the natural outcome of choosing an even
`games_per_fen` for balanced alternating coverage vs. a smaller `games_per_fen`
for a quick random series — not a behavior baked into the mode. This keeps the
change additive and fully backward compatible.
