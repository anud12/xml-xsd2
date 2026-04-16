Feature: Runtime Start
  Runtime must launch successfully

  Scenario Outline: Archive with empty module
    Given I run the application in debug mode
    And I have added "./manifest.json" file to archive
    Then assert log line containing "Runtime launched" regex
    Examples:
    |manifest|entry|
    |./manifest.json|index.js|
    |./manifest-empty-second.json|index-empty-second.js|