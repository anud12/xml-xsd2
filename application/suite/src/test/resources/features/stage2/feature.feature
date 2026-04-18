Feature: Stage 2 Interactivity

  Rule: UI
    Scenario Outline: Create panel
      Given I run the application in debug mode
      And I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
      And I have added "./<directory>/index.js" file as "./index.js" to archive
      And I have added "./<directory>/texture.exr" file as "./texture.exr" to archive
      When I load current archive
      Then assert exported state panel includes regexes from "./<directory>/panel.csv" file
      Then assert that `get_panel_names` is array "<panelName>" from json
      Examples:
        | directory           | panelName                          |
        | create_panel/first  | [\"panel\", \"panel_2\"]               |
        | create_panel/second | [\"second panel\", \"second panel_2\"] |