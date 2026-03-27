Feature: Archive all files in directory
  Scenario: Archive and verify file contents

    Given I have added "./helloWorld.txt" file to archive
    And I have added "./helloWorldCopy.txt" file to archive
    When I run the application using archive
    Then output table "files" must be "./fileData.csv" csv
