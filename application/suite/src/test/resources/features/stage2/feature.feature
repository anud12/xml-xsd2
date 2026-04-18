Feature: Stage 2 Interactivity

  Rule: UI
    Scenario Outline: Create panel
      Given I run the application in debug mode
      And I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
      And I have added "./<directory>/index.js" file as "./index.js" to archive
      And I have added "./<directory>/texture.exr" file as "./texture.exr" to archive
      When I load current archive
      Then assert exported state panel includes regexes from "./<directory>/panel.csv" file
      Then assert exactly 0 log lines matches "register panel"
      Examples:
        | directory          |
        | create_panel/first |
        | create_panel/second |