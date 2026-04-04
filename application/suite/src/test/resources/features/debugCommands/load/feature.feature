Feature: Module load support
  If the application is ran in debug mode, it must support loading modules

  Scenario: Archive with empty module
    Given I run the application in debug mode
    And I load current archive
    Then assert exported state output table "module" includes regexes from "./module.csv"
