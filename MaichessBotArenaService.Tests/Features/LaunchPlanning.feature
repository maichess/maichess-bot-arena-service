Feature: Launch planning
  The global concurrency limit caps how many arena games may run at once; the
  planner launches the spare capacity, never more than are waiting.

  Scenario Outline: How many games may launch now
    When the limit is <limit>, <running> are running and <pending> are waiting
    Then <launchable> games may launch

    Examples:
      | limit | running | pending | launchable |
      | 4     | 0       | 10      | 4          |
      | 4     | 3       | 10      | 1          |
      | 4     | 4       | 10      | 0          |
      | 4     | 5       | 10      | 0          |
      | 4     | 1       | 2       | 2          |
      | 0     | 0       | 5       | 0          |
