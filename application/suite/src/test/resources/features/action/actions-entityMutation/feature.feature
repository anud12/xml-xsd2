Feature: No-Input Action with Entity Creation
  No-input actions do not target a specific entity or container.
  They are actor-only operations. When executed, they can emit events
  that trigger effects, which may create entities.

  Scenario: No-input action execution creates entity via effect
    Given I run the application in debug mode
    And I have added "./manifest.json" file to archive
    And I have added "./index.js" file to archive
    And I load current archive
    When I run 1 iterations
    And I send action "create_entity_action" from actor "actor1"
    Then assert exported state output table "entity" includes regexes from "./entity.csv"
    And I run 1 iterations
    And I send action "append_name_action" from actor "actor1"
    Then assert exported state output table "entity" includes regexes from "./entity_mutated.csv"
