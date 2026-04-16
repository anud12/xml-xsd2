Feature: Stage 1 Implementation

  Scenario Outline: Archive with empty module
    Given I run the application in debug mode
    And I have added "./<directory>/manifest.json" file to archive
    And I have added "./<directory>/index.js" file to archive
    Then assert log line containing "Runtime launched" regex
    Examples:
      | directory     |
      | module/first  |
      | module/second |