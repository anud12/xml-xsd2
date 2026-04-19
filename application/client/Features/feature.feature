Feature: Test

Scenario: Add two numbers
	Given I load the runtime
	And I have added "./<directory>/manifest.json" file as "./manifest.json" to archive
	When I load current archive
	Then assert that \"GetPanelNames\" returns "first,second"