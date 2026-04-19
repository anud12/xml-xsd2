Feature: Test

  Scenario Outline: Add two panels
    Given I load the runtime
    Given I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
    Given I have added "./<directory>/index.js" file as "./index.js" to archive
    When I load current archive
    Then assert that `GetPanelNames` returns "<panelNames>"
    Examples:
      | directory           | panelNames                  |
      | create_panel/first  | panel,panel_2               |
      | create_panel/second | second panel,second panel_2 |