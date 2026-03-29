Feature: Iteration support
  If the application is ran in debug mode, it must support running finite iterations

  Scenario: Archive with empty module
    Given I run the application in debug mode
    And I run 3 iterations
    Then assert that 3 log line(s) contains "Iteration completed in {[0-9]+:[0-9]+}ns" regex