Feature: Transformation-Test-Sample-StepDefinition
	As a integration specialist
	I want to verify that I can validate a transformation step in my workflow

# A workflow with a transformation action in it. 
# The workflow is triggered by a service bus message that is put on a topic and forwared to a processing queue, 
# and the payload is stored in storage account. 
# Workflow itself is triggered by the claim check message on the queue.
Scenario: Transform source message to destination message

    Given A message with a data from the source system
    And It has the following source data:
	| Field															| Value									|
	| Value															| Open Sesame							|
	| OnClick														| OpenMagicGate()						|
	When The message payload is put in Storage Account container "demoworkload" with file name "rcv-source-data.json"
	And The claim-check is put on Service Bus topic "sbt-sourcesystem-out" with properties:
	| Field															| Value                                 |
	| sender     													| sendingWorkflowName                   |
	| messageType  													| demoMessageType                       |
	| someProperty													| someValue							    |
	Then The workflow "prc" executed these actions:
    | StepName														| Status    |
    | Transform source data type to target data type				| Succeeded |
	And Workflow step "Transform source data type to target data type" has transformed the data
	And The transformed data has "Status" with value "Open Sesame"
	And The transformed data has "Action" with value "OpenMagicGate()"

##### FILE BASED INPUT FOR TESTING #####
Scenario: Transform source message to destination message from a file

	Given A message with a data from the source system
	And It has content from a file named "sample-transformation-test-input.json"
	When The message payload is put in Storage Account container "demoworkload" with file name "rcv-source-data.json"
	And The claim-check is put on Service Bus topic "sbt-sourcesystem-out" with properties:
	| Field															| Value                                 |
	| sender     													| sendingWorkflowName                   |
	| messageType  													| demoMessageType                       |
	| someProperty													| someValue							    |
	Then The workflow "prc" executed these actions:
    | StepName														| Status    |
    | Transform source data type to target data type				| Succeeded |
	And Workflow step "Transform source data type to target data type" has transformed the data
	And The transformed data has "Status" with value "Open Sesame"
	And The transformed data has "Action" with value "OpenMagicGate()"

##### NESTED PROPERTIES AND ITERATIONS CAN BE TESTED THIS WAY #####
# And The transformed data has "SomeProperty[0].SomeSubProperty[0]" with values
#		| Field               | Value                              |
#		| Field1	          | Some value 1 |
#		| FIeld2			  | Some value 2 |
#		| FIeld3			  | Some value 3 |
#		| FIeld4			  | Some value 4 |
#		| FIeld5			  | Some value 5 |
#		| FIeld6			  | Some value 6 |
#		| FIeld7			  | Some value 7 |
#		| FIeld8			  | Some value 8 |
#		| FIeld9			  | Some value 9 |