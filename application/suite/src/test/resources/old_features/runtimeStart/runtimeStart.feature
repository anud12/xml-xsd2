Feature: Runtime Start
  Runtime must launch successfully

  Scenario: Archive with empty module
    Given I run the application in debug mode
    Then assert log line containing "Runtime launched" regex