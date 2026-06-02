Feature: Tournament stage tie-break
  The stage winner is decided by the ladder: wins, then rounds-to-win, then
  aggregate clock advantage, then aggregate material advantage, then a coin flip.

  Scenario: More wins wins the stage (bot a)
    Given the stage games:
      | white | black | outcome   |
      | a     | b     | white_won |
      | a     | b     | white_won |
      | b     | a     | white_won |
    When the stage winner is decided
    Then the stage winner is "a"

  Scenario: More wins wins the stage (bot b)
    Given the stage games:
      | white | black | outcome   |
      | b     | a     | white_won |
      | b     | a     | white_won |
      | a     | b     | white_won |
    When the stage winner is decided
    Then the stage winner is "b"

  Scenario: Equal wins, fewer rounds to win favours bot a
    Given the stage games:
      | white | black | outcome   |
      | a     | b     | white_won |
      | b     | a     | white_won |
    When the stage winner is decided
    Then the stage winner is "a"

  Scenario: Equal wins, fewer rounds to win favours bot b
    Given the stage games:
      | white | black | outcome   |
      | b     | a     | white_won |
      | a     | b     | white_won |
    When the stage winner is decided
    Then the stage winner is "b"

  Scenario: All draws, greater clock advantage favours bot a
    Given the stage games:
      | white | black | outcome | whiteMs | blackMs |
      | a     | b     | draw    | 1000    | 500     |
    When the stage winner is decided
    Then the stage winner is "a"

  Scenario: All draws, greater clock advantage favours bot b
    Given the stage games:
      | white | black | outcome | whiteMs | blackMs |
      | a     | b     | draw    | 500     | 1000    |
    When the stage winner is decided
    Then the stage winner is "b"

  Scenario: Equal clocks, greater material favours bot a
    Given the stage games:
      | white | black | outcome | whiteMs | blackMs | fen                           |
      | a     | b     | draw    | 0       | 0       | Q6k/8/8/8/8/8/8/K7 w - - 0 1  |
    When the stage winner is decided
    Then the stage winner is "a"

  Scenario: Equal clocks, greater material favours bot b
    Given the stage games:
      | white | black | outcome | whiteMs | blackMs | fen                           |
      | b     | a     | draw    | 0       | 0       | Q6k/8/8/8/8/8/8/K7 w - - 0 1  |
    When the stage winner is decided
    Then the stage winner is "b"

  Scenario: Everything equal falls back to the coin flip (bot a)
    Given the stage games:
      | white | black | outcome |
      | a     | b     | draw    |
    And the tie-break coin flip yields 0
    When the stage winner is decided
    Then the stage winner is "a"

  Scenario: Everything equal falls back to the coin flip (bot b)
    Given the stage games:
      | white | black | outcome |
      | a     | b     | draw    |
    And the tie-break coin flip yields 1
    When the stage winner is decided
    Then the stage winner is "b"
