Feature: Bracket
  The tournament seeds bots randomly and reduces a non-power-of-2 field to a
  power of 2 in the first round using byes.

  Scenario: Seeding is a deterministic shuffle
    When bots "a,b,c,d" are seeded with RNG "0,1,0"
    Then the seeding is "c,d,b,a"

  Scenario: First round of a power-of-two field has no byes
    When the first round is built from seeds "a,b,c,d"
    Then the pairings are:
      | botA | botB |
      | a    | b    |
      | c    | d    |

  Scenario: First round of a three-bot field gives the top seed a bye
    When the first round is built from seeds "a,b,c"
    Then the pairings are:
      | botA | botB |
      | a    |      |
      | b    | c    |

  Scenario: First round of a five-bot field gives three byes
    When the first round is built from seeds "a,b,c,d,e"
    Then the pairings are:
      | botA | botB |
      | a    |      |
      | b    |      |
      | c    |      |
      | d    | e    |

  Scenario: The next round pairs advancing bots sequentially
    When the next round is built from advancing "a,b,c,d"
    Then the pairings are:
      | botA | botB |
      | a    | b    |
      | c    | d    |
