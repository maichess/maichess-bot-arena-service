Feature: Setup expansion
  The arena expands each setup config into the exact ordered list of games.

  Scenario: Single setup with fixed colors keeps the assignment for every game
    When a single setup expands white "w" black "b" over FENs "standard" with 3 games per FEN and color switching off
    Then the expansion produces:
      | order | white | black | label    |
      | 0     | w     | b     | Standard |
      | 1     | w     | b     | Standard |
      | 2     | w     | b     | Standard |

  Scenario: Single setup switching colors alternates every game
    When a single setup expands white "w" black "b" over FENs "standard" with 4 games per FEN and color switching on
    Then the expansion produces:
      | order | white | black | label    |
      | 0     | w     | b     | Standard |
      | 1     | b     | w     | Standard |
      | 2     | w     | b     | Standard |
      | 3     | b     | w     | Standard |

  Scenario: Single setup switching colors continues alternation across FENs
    When a single setup expands white "w" black "b" over FENs "standard,posB" with 1 games per FEN and color switching on
    Then the expansion produces:
      | order | white | black | label    |
      | 0     | w     | b     | Standard |
      | 1     | b     | w     | FEN 2    |

  Scenario: Matrix setup plays every unordered pair once per FEN
    When a matrix setup expands bots "a,b,c,d" over FENs "standard,posB" with 1 games per FEN
    Then the expansion has 12 games
    And the expansion covers 6 distinct unordered pairs

  Scenario: Matrix setup alternates colors per game within a pair
    When a matrix setup expands bots "a,b" over FENs "standard" with 2 games per FEN
    Then the expansion produces:
      | order | white | black | label    |
      | 0     | a     | b     | Standard |
      | 1     | b     | a     | Standard |

  Scenario: Matrix alternation continues across FENs within a pair
    When a matrix setup expands bots "a,b" over FENs "standard,posB" with 1 games per FEN
    Then the expansion produces:
      | order | white | black | label    |
      | 0     | a     | b     | Standard |
      | 1     | b     | a     | FEN 2    |

  Scenario: Matrix random mode assigns each game's colors from the RNG
    When a matrix setup expands bots "a,b" over FENs "standard" with 3 games per FEN in random mode with RNG "0,1,1"
    Then the expansion produces:
      | order | white | black | label    |
      | 0     | a     | b     | Standard |
      | 1     | b     | a     | Standard |
      | 2     | b     | a     | Standard |

  Scenario: Matrix random mode keeps games_per_fen as the game count
    When a matrix setup expands bots "a,b,c" over FENs "standard" with 1 games per FEN in random mode with RNG "0"
    Then the expansion has 3 games
    And the expansion covers 3 distinct unordered pairs
    And the expansion produces:
      | order | white | black | label    |
      | 0     | a     | b     | Standard |
      | 1     | a     | c     | Standard |
      | 2     | b     | c     | Standard |

  Scenario: Tournament stage both-colors plays each FEN twice swapping colors
    When a tournament stage expands bots "a" and "b" over FENs "standard,posB" in both-colors mode
    Then the expansion produces:
      | order | white | black | label    |
      | 0     | a     | b     | Standard |
      | 1     | b     | a     | Standard |
      | 2     | a     | b     | FEN 2    |
      | 3     | b     | a     | FEN 2    |

  Scenario: Tournament stage random mode plays each FEN once using the RNG
    When a tournament stage expands bots "a" and "b" over FENs "standard,posB" in random mode with RNG "0,1"
    Then the expansion produces:
      | order | white | black | label    |
      | 0     | a     | b     | Standard |
      | 1     | b     | a     | FEN 2    |
