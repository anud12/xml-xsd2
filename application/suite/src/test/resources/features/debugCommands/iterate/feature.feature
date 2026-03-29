Feature: Iteration support
  If the application is ran in debug mode, it must support running finite iterations

  Scenario Outline: Archive with empty module
    Given I run the application in debug mode
    And I run <iterations> iterations
    Then assert that <no of logs> log line(s) contains "Iteration completed in {[0-9]+:[0-9]+}ns" regex

    Examples:
      | iterations | no of logs |
      | 1          | 1          |
      | 2          | 2          |
      | 3          | 3          |