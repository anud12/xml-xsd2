Feature: Archive all files in directory
  Scenario: Archive and verify file contents

    Given I have added "./helloWorld.txt" file to archive
    When I run the application using archive
    Then the stdout must contain line "Hello with text content"