Feature: Module manifest validation
  The runtime must require a manifest.json at the root of every module archive, describing metadata and entrypoint. After a module is loaded, the runtime must emit a module-loaded event and register the event in the event registry.

  Scenario: Archive with event which creates an entity
    Given I have added "./manifest.json" file to archive
    And I have added "./index.js" file to archive
    When I run the application using archive
    Then assert output table "entity" must be "./entity.csv" csv
