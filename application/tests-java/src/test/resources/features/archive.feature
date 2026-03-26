Feature: Archive all files in directory
  Scenario: Archive and verify file contents
    Given the test directory contains files
    When I create an archive of all files in the directory
    Then the archive should contain all files with correct contents
