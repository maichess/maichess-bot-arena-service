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
