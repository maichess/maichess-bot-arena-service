Feature: Collection lifecycle
  Creating a setup validates it, expands it into games, launches them under the
  global concurrency cap, advances as games finish, and finalizes.

  # ── Validation ──────────────────────────────────────────────────────────────

  Scenario: A blank name is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field | value |
      | name  |       |
      | white | a     |
      | black | b     |
    Then the create result is invalid input "name is required"

  Scenario: An unknown time format is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field       | value |
      | white       | a     |
      | black       | b     |
      | time_format | 99+99 |
    Then the create result is invalid input "unknown time_format_id"

  Scenario: A single setup without both bots is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field | value |
      | white | a     |
      | black |       |
    Then the create result is invalid input "white_bot_id and black_bot_id are required"

  Scenario: A single setup with an unknown bot is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field | value |
      | white | a     |
      | black | z     |
    Then the create result is invalid input "unknown bot_id"

  Scenario: A single setup with a blank white bot is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field | value |
      | white |       |
      | black | b     |
    Then the create result is invalid input "white_bot_id and black_bot_id are required"

  Scenario: A single setup with an unknown white bot is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field | value |
      | white | z     |
      | black | b     |
    Then the create result is invalid input "unknown bot_id"

  Scenario: A single setup with no games per FEN is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 0     |
    Then the create result is invalid input "games_per_fen must be at least 1"

  Scenario: A matrix setup with fewer than two bots is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field | value  |
      | kind  | matrix |
      | bots  | a      |
    Then the create result is invalid input "at least 2 bots are required"

  Scenario: A matrix setup with an unknown bot is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field | value  |
      | kind  | matrix |
      | bots  | a,z    |
    Then the create result is invalid input "unknown bot_id"

  Scenario: A matrix setup with no games per FEN is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field         | value  |
      | kind          | matrix |
      | bots          | a,b    |
      | games_per_fen | 0      |
    Then the create result is invalid input "games_per_fen must be at least 1"

  Scenario: A tournament setup with no FENs per stage is rejected
    Given the known bots are "a,b"
    When a setup is created:
      | field          | value      |
      | kind           | tournament |
      | bots           | a,b        |
      | fens_per_stage | 0          |
    Then the create result is invalid input "fens_per_stage must be at least 1"

  # ── Single ────────────────────────────────────────────────────────────────────

  Scenario: A single setup launches its games and finishes
    Given the known bots are "a,b"
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 4     |
      | switching     | on    |
    Then the create result is success
    And the collection status is "running"
    And 4 games are running
    And a game was launched with start_fen ""
    When the setup runs to completion with white always winning
    Then the collection status is "finished"
    And 4 games are finished
    And the collection has 4 games

  Scenario: A single setup from a custom FEN launches with that start position
    Given the known bots are "a,b"
    When a setup is created:
      | field         | value                                                        |
      | white         | a                                                            |
      | black         | b                                                            |
      | games_per_fen | 1                                                            |
      | fens          | rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR w KQkq c6 0 2 |
    Then a game was launched with start_fen "rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR w KQkq c6 0 2"

  # ── Matrix ────────────────────────────────────────────────────────────────────

  Scenario: A matrix setup plays every pair and finishes
    Given the known bots are "a,b,c"
    When a setup is created:
      | field         | value  |
      | kind          | matrix |
      | bots          | a,b,c  |
      | games_per_fen | 1      |
    Then the collection has 3 games
    When the setup runs to completion with white always winning
    Then the collection status is "finished"
    And 3 games are finished

  Scenario: A matrix setup in random-colors mode still plays every pair once per game
    Given the known bots are "a,b,c"
    When a setup is created:
      | field         | value  |
      | kind          | matrix |
      | bots          | a,b,c  |
      | games_per_fen | 1      |
      | matrix_mode   | random |
    Then the collection has 3 games
    When the setup runs to completion with white always winning
    Then the collection status is "finished"
    And 3 games are finished

  # ── Tournament ────────────────────────────────────────────────────────────────

  Scenario: A four-bot tournament runs through two rounds to a champion
    Given the known bots are "a,b,c,d"
    When a setup is created:
      | field          | value      |
      | kind           | tournament |
      | bots           | a,b,c,d    |
      | fens_per_stage | 1          |
    Then the collection has 4 games
    When the setup runs to completion with white always winning
    Then the collection status is "finished"
    And the collection winner is "a"
    And the collection has 6 games

  Scenario: A three-bot tournament gives the top seed a bye
    Given the known bots are "a,b,c"
    When a setup is created:
      | field          | value      |
      | kind           | tournament |
      | bots           | a,b,c      |
      | fens_per_stage | 1          |
    Then the collection has 2 games
    When the setup runs to completion with white always winning
    Then the collection status is "finished"
    And the collection winner is "a"

  Scenario: A tournament in random-colors mode plays one game per stage
    Given the known bots are "a,b,c,d"
    When a setup is created:
      | field          | value      |
      | kind           | tournament |
      | bots           | a,b,c,d    |
      | fens_per_stage | 1          |
      | mode           | random     |
    Then the collection has 2 games
    When the setup runs to completion with white always winning
    Then the collection winner is "a"
    And the collection has 3 games

  # ── Concurrency cap ───────────────────────────────────────────────────────────

  Scenario: The cap limits how many games of one setup run at once
    Given the known bots are "a,b"
    And the concurrency limit is 2
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 6     |
    Then 2 games are running
    And 4 games are pending

  Scenario: A new setup waits when the cap is already saturated
    Given the known bots are "a,b"
    And the concurrency limit is 1
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 2     |
    And a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 2     |
    Then the collection status is "pending"
    And 0 games are running
    And 2 games are pending

  # ── Reads & edge cases ────────────────────────────────────────────────────────

  Scenario: Collections can be listed and fetched
    Given the known bots are "a,b"
    When a setup is created:
      | field | value |
      | white | a     |
      | black | b     |
    And collections are listed with status "" limit 0 offset 0
    Then 1 collections are listed
    When collections are listed with status "running" limit 5 offset 0
    Then 1 collections are listed
    When the collection is fetched
    Then the fetch returns the collection with its games

  Scenario: Fetching a missing collection returns nothing
    When a missing collection "nope" is fetched
    Then the fetch returns nothing

  Scenario: Finishing a game whose collection is gone is a no-op
    When a finished game for a missing collection is handled
    Then no games were launched
