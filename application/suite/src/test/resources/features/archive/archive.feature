Feature: Archive all files in directory
  Scenario: Archive and verify file contents

    Given I have added "./helloWorld.txt" file to archive
    When I run the application using archive until completion
    Then assert output table "files" must be "./fileData.csv" csv
    And assert log line containing "loaded helloWorld.txt"
    