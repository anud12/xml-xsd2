Feature: Stage 1 Implementation

  Rule: Module Initialization
    Scenario Outline: Archive with module and script should log by executing the js file
      Given I run the application in debug mode
      And I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
      And I have added "./<directory>/index.js" file as "./index.js" to archive
      When I load current archive
      Then assert exactly <expected> log lines matches "<log>"
      Examples:
        | directory       | log                  | expected |
        | module/first    | First module loaded  | 1        |
        | module/second   | Second module loaded | 1        |
        | module/if-guard | if guard loaded      | 0        |

    Scenario Outline: Archive with module and script should log by executing the js file
      Given I run the application in debug mode
      And I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
      When I load current archive
      Then assert exactly 1 log lines matches "<log>"
      Examples:
        | directory                 | log                                                 |
        | module/missing_entrypoint | Error: entrypoint \"index.js\" not found in archive |


  Rule: Actions
    Scenario Outline: Register action
      Given I run the application in debug mode
      And I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
      And I have added "./<directory>/index.js" file as "./index.js" to archive
      When I load current archive
      Then assert exported state action includes regexes from "./<directory>/action.csv" file
      Examples:
        | directory     |
        | action/first  |
        | action/second |


    Scenario Outline: Call registered action
      Given I run the application in debug mode
      And I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
      And I have added "./<directory>/index.js" file as "./index.js" to archive
      When I load current archive
      Then I trigger action "<action>"
      Then assert exactly 1 log lines matches "<log>"

      Examples:
        | directory          | action        | log                  |
        | call_action/first  | action        | action called        |
        | call_action/second | second action | second action called |
