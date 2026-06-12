Feature: Receive-Other-Trigger-StepDefinition
	As a integration specialist
	I want to verify that chained correlated workflows with nested structures are properly validated in the test framework

Scenario: Validate request
    When Workflow "rcv-other-type-trigger" is triggered
    Then The workflow executed these actions:
    | StepName                                                              | Status    |
    | Initialize variables                                                  | Succeeded |