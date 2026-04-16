Feature: Export support
  If the application is ran in debug mode, it must support exporting its state

  Scenario: It should have empty state when no module is loaded
    Given I run the application in debug mode
    And assert exported state should be empty