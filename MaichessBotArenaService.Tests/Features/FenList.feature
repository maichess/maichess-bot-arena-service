Feature: FEN list normalization
  The arena normalizes a configured FEN pool and labels positions for display.

  Scenario: An empty pool collapses to the standard position
    When the FEN pool "" is normalized
    Then the normalized pool is "standard"

  Scenario Outline: A standard literal maps to the standard position
    When the FEN pool "<input>" is normalized
    Then the normalized pool is "standard"

    Examples:
      | input      |
      | standard   |
      | STANDARD   |
      |   Standard |

  Scenario: Blank entries are dropped
    When the FEN pool " , , " is normalized
    Then the normalized pool is "standard"

  Scenario: Custom positions are trimmed and kept in order with standard mapped
    When the FEN pool " posA , standard , posB " is normalized
    Then the normalized pool is "posA,standard,posB"

  Scenario: The standard position is labelled Standard
    Then the label for the standard position at index 0 is "Standard"

  Scenario: A custom position is labelled by its one-based index
    Then the label for a custom position at index 2 is "FEN 3"
