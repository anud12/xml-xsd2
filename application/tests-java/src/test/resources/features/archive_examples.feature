Feature: Archive all files in directory
  Scenario Outline: Archive and verify file contents for different files
    Given the test directory contains files named <file1> and <file2> with contents <content1> and <content2>
    When I create an archive of all files in the directory
    When I run the application on the archive
    Then the archive should contain all files with correct contents

    Examples:
      | file1      | content1      | file2      | content2        |
      | fileA.txt  | Hello A       | fileB.txt  | Goodbye B       |
      | alpha.txt  | Alpha Content | beta.txt   | Beta Content    |
