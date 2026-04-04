Feature: Runtime

  Require when the runtime is ran, it should run underlying javascript file in a
  embedded javascript engine.

  Scenario Outline: Archive with empty module
    Given I run the application in debug mode
    And I have added "./manifest<module_suffix>.json" file to archive
    And I have added "./index<module_suffix>.js" file to archive
    Then assert log line containing "Runtime launched" regex
    Then assert exported state output table "events" includes regexes from "./events<module_suffix>.csv"
    Examples:
      | module_suffix |
      | _boolean      |
      | _number       |