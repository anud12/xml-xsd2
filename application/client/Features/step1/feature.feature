Feature: Stage 1 Ui Box display

  Scenario Outline: Add two panels
    Given I load the runtime
    Given I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
    Given I have added "./<directory>/index.js" file as "./index.js" to archive
    When I load current archive
    Then assert that `GetPanelIds` returns "<panelNames>"
    Examples:
      | directory             | panelNames                  |
      | add_two_panels/first  | panel,panel_2               |
      | add_two_panels/second | second panel,second panel_2 |


#  Scenario Outline: View panel data
#    Given I load the runtime
#    Given I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
#    Given I have added "./<directory>/index.js" file as "./index.js" to archive
#    When I load current archive
#    Then assert that `GetPanelData` for "<id>" has id "<id>"
#    Examples:
#      | directory              | id           |
#      | view_panel_data/first  | panel        |
#      | view_panel_data/second | second panel |