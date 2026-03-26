Feature: Archive creation from directory
  As a user
  I want to create an in-memory archive from all files in a directory
  So that I can list the files in the archive

  Scenario: Archive contains all files in directory
    Given a directory with files "file1.txt" and "file2.txt"
    When I create an archive from the directory
    Then the archive should contain "file1.txt" and "file2.txt"
