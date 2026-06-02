Feature: Concurrency limit setting
  The global concurrency limit defaults until set and rejects values below 1.

  Scenario: An unset limit returns the default
    Given no concurrency limit is stored
    When the concurrency limit is read
    Then the concurrency limit is 4

  Scenario: A stored limit is returned
    Given the stored concurrency limit is 8
    When the concurrency limit is read
    Then the concurrency limit is 8

  Scenario: Setting a valid limit succeeds and persists
    When the concurrency limit is set to 6
    Then the set succeeds with limit 6
    And the stored concurrency limit is now 6

  Scenario: Setting a limit below 1 is rejected
    When the concurrency limit is set to 0
    Then the set is rejected with "limit must be at least 1"
