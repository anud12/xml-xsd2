Feature: Module manifest validation
  The runtime must require a manifest.json at the root of every module archive, describing metadata and entrypoint. After a module is loaded, the runtime must emit a module-loaded event and register the event in the event registry.

  Scenario: Archive with valid manifest.json
    Given I have added "./manifest.json" file to archive
    And I have added "./index.js" file to archive
    When I run the application using archive
    Then assert log line containing "manifest.json loaded"
    And assert log line containing "index.js loaded"

  Scenario: Archive missing manifest.json
    Given I have added "./index.js" file to archive
    When I run the application using archive
    Then assert log line containing "manifest.json not found"
    And assert log line containing "module rejected"

  Scenario: Emit event after module load
    Given I have added "./manifest.json" file to archive
    And I have added "./index.js" file to archive
    When I run the application using archive
    Then assert log line containing "event: module-loaded"
    And assert log line containing "event registered: empty event"
