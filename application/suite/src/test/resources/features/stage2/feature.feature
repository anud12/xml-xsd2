Feature: Stage 2 ui display
  # For
  # - background change to have texture path, and texture filtering type
  # - offset must have top,bottom,left,right
  # - change texture to have hover and for button a click variant
  # - add an "action button" without entity

  Rule: Panel Initialization
    Scenario Outline: Archive with module and script should log by executing the js file
      Given I run the application in debug mode
      And I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
      And I have added "./<directory>/index.js" file as "./index.js" to archive
      When I load current archive
      Then assert exported state panel includes regexes from "./<directory>/panel.csv" file
      Examples:
        | directory                   |
        | panel_initialization/offset |
        | panel_initialization/offset |
        | panel_initialization/offset |