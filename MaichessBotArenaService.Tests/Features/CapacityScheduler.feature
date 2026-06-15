Feature: Global capacity scheduler
  The arena reconciles the whole set of collections against the single global
  concurrency cap: queued games start as soon as capacity frees, oldest
  collection first (FIFO), and never more than the cap run at once. Raising the
  limit fills the new headroom immediately; lowering it launches nothing and lets
  in-flight games drain.

  Scenario: A collection created at full capacity starts when a running game finishes
    Given the known bots are "a,b,c,d"
    And the concurrency limit is 1
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 1     |
    And a setup is created:
      | field         | value |
      | white         | c     |
      | black         | d     |
      | games_per_fen | 1     |
    Then 1 games are running in total
    And 1 games are pending in total
    And setup 2 is "pending"
    When one running game finishes as a white win
    Then 1 games are finished in total
    And 1 games are running in total
    And 0 games are pending in total
    And setup 2 is "running"

  Scenario: Queued collections start FIFO as capacity frees
    Given the known bots are "a,b,c,d,e,f"
    And the concurrency limit is 1
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 1     |
    And a setup is created:
      | field         | value |
      | white         | c     |
      | black         | d     |
      | games_per_fen | 1     |
    And a setup is created:
      | field         | value |
      | white         | e     |
      | black         | f     |
      | games_per_fen | 1     |
    Then 1 games are running in total
    And 2 games are pending in total
    When one running game finishes as a white win
    Then setup 2 is "running"
    And setup 3 is "pending"
    When one running game finishes as a white win
    Then setup 3 is "running"

  Scenario: Raising the limit immediately launches queued games up to the new cap
    Given the known bots are "a,b"
    And the concurrency limit is 1
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 4     |
    Then 1 games are running in total
    And 3 games are pending in total
    When the concurrency limit is changed to 3
    Then 3 games are running in total
    And 1 games are pending in total

  Scenario: Lowering the limit launches nothing and running drains to the cap
    Given the known bots are "a,b"
    And the concurrency limit is 4
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 4     |
    Then 4 games are running in total
    When the concurrency limit is changed to 2
    Then 4 games are running in total
    And 0 games are pending in total
    When one running game finishes as a white win
    Then 3 games are running in total
    And 0 games are pending in total

  Scenario: An invalid limit change is rejected and launches nothing
    Given the known bots are "a,b"
    And the concurrency limit is 2
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 4     |
    Then 2 games are running in total
    When the concurrency limit is changed to 0
    Then 2 games are running in total
    And 2 games are pending in total

  Scenario: Several games finishing in one tick never launch past the cap
    Given the known bots are "a,b"
    And the concurrency limit is 3
    When a setup is created:
      | field         | value |
      | white         | a     |
      | black         | b     |
      | games_per_fen | 9     |
    Then 3 games are running in total
    And 6 games are pending in total
    When all running games finish as a white win
    Then 3 games are running in total
    And 3 games are finished in total
    And 3 games are pending in total
