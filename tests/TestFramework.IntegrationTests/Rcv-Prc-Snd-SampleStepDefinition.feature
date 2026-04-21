Feature: Receive-Process-Send-Sample-StepDefinition
	As a integration specialist
	I want to verify that chained correlated workflows with nested structures are properly validated in the test framework

# Sample receive workflow with http trigger
Scenario: Validate http request with file content
    When Workflow "rcvHttp" is triggered with file "sample-http-request-content.json"
    Then The workflow executed these actions:
    | StepName                                                              | Status    |
    | Initialize variables                                                  | Succeeded |
    And In "Try":
    | StepName                                                              | Status    |
    | Validate json schema                                                  | Succeeded |

# Validate rcv-prc-snd chain
Scenario: Validate complete workflow chain
    # Recurrence triggered workflow
    When Workflow "rcv" is triggered
    Then The workflow executed these actions:
    | StepName                                                              | Status    |
    | Initialize variable sampleMessage                                     | Succeeded |
    | Initialize variable correlationId                                     | Succeeded |
    | Initialize variable counter                                           | Succeeded |
    | Initialize variable errorMessage                                      | Succeeded |
    | Initialize variable                                                   | Succeeded |
    And In "Try":
    | StepName                                                              | Status    |
    | Validate json schema                                                  | Succeeded |
    And All iterations of "For each item in items" executed:
    | StepName                                                              | Status    |
    | Increment variable counter                                            | Succeeded |
    | Write message payload to storage                                      | Succeeded |
    | Send claim-check message to topic                                     | Succeeded |
    And In "Try":
    | StepName                                                              | Status    |
    | Send success tracked properties to table storage                      | Succeeded |
    | Send success tracked properties to log analytics                      | Succeeded |
    
    Then For all instances of "prc" with the same correlation:
    | StepName                                                              | Status    |
    | Receive new claim-check message from queue                            | Succeeded |
    | Initialize variable correlationId                                     | Succeeded |
    | Initialize variable errorMessage                                      | Succeeded |
    And In "Try":
    | StepName                                                              | Status    |
    | Retrieve message payload from storage                                 | Succeeded |
    | Transform source data type to target data type                        | Succeeded |
    | Write message payload to storage                                      | Succeeded |
    | Send claim-check message to queue                                     | Succeeded |
    | Complete claim-check message in queue                                 | Succeeded |
    | Send success tracked properties to log analytics                      | Succeeded |

    Then For all instances of "snd" with the same correlation:
    | StepName                                                              | Status    |
    | Receive new claim-check message from queue                            | Succeeded |
    | Initialize variable correlationId                                     | Succeeded |
    | Initialize variable errorMessage                                      | Succeeded |
    And In "Try":
    | StepName                                                              | Status    |
    | Retrieve message payload from storage                                 | Succeeded |
    | Post to google.com api                                                | Failed    |
    And In "Try.Check http status code.Default":
    | StepName                                                              | Status    |
    | Set variable errorMessage to http response body                       | Succeeded |
    | Throw exception                                                       | Succeeded |
    And In "Try":
    | StepName                                                              | Status    |
    | Complete claim-check message in queue                                 | Succeeded |
    | Send success tracked properties to log analytics                      | Succeeded |