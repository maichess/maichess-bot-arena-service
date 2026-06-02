Feature: Scoring
  Each game awards 1 point for a win, 0.5 for a draw, and 0 for a loss or an
  unfinished game.

  Scenario: Points are awarded for each outcome
    Given the stage games:
      | white | black | outcome   |
      | a     | b     | white_won |
      | a     | b     | black_won |
      | a     | b     | draw      |
      | a     | b     | ongoing   |
    Then bot "a" has 1.5 points
    And bot "b" has 1.5 points
    And the pair score for "a" and "b" is 1.5 to 1.5

  Scenario: A decisive series totals correctly
    Given the stage games:
      | white | black | outcome   |
      | a     | b     | white_won |
      | b     | a     | black_won |
      | a     | b     | white_won |
    Then bot "a" has 3 points
    And bot "b" has 0 points
