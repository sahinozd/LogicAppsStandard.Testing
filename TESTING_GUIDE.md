# Azure Logic Apps Standard  -  Workflow Testing Reference Guide

## Table of Contents
1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Complete Step Reference](#complete-step-reference)
4. [Path-Based Navigation](#path-based-navigation)
5. [Caching and Performance](#caching-and-performance)
6. [Integration Testing (Receive-Process-Send)](#integration-testing-receive-process-send)
7. [Transformation Testing](#transformation-testing)
8. [Advanced Scenarios](#advanced-scenarios)
9. [Best Practices](#best-practices)
10. [Troubleshooting](#troubleshooting)

---

## Overview

This guide covers every Gherkin step available in the framework, using the actual `.feature` files in this repository as the primary source for all examples.

The framework provides two classes of step definitions:

- **`BaseStepDefinition`**  -  workflow trigger steps, action assertion steps, loop, scope, condition, switch, and correlated multi-workflow validation
- **`BaseTransformationStepDefinition<TSource, TDestination>`**  -  extends `BaseStepDefinition` with steps for uploading payloads to Blob Storage, sending claim-check messages to Service Bus, and asserting on typed transformation output

All assertion steps use a **depth-first search** across the full action tree. An action inside a Try scope, a Condition branch, a ForEach iteration, or a nested loop is found without needing a path prefix. Use path navigation when you need to target a **specific structural location**, such as a particular loop iteration or a condition branch.

---

## Quick Start

### Triggering a Workflow and Validating Actions

The following is taken directly from `Foreach-Until-SampleStepDefinition.feature`:

```gherkin
Feature: Nested-Foreach-and-Until-Loops-Sample-StepDefinition

Scenario: Validate complete workflow with nested structures

    When Workflow "prc-nestedloops-and-do-until" is triggered

    Then The workflow executed these actions:
        | StepName             | Status    |
        | Recurrence           | Succeeded |
        | Initialize variables | Succeeded |
        | Set variable         | Succeeded |

    And The "Set variable" action has status "Succeeded"
```

`Then The workflow executed these actions:` uses depth-first search  -  any action at any nesting depth is found without needing a path prefix.

### Triggering with a File

```gherkin
When Workflow "rcvHttp" is triggered with file "sample-http-request-content.json"
```

The file must exist in the `TestData` directory relative to the test project. For a trigger that requires `Content-Type: application/json`:

```gherkin
When Workflow "rcvHttp" is triggered with json file "sample-http-request-content.json"
```

---

## Complete Step Reference

### Trigger Steps

| Step | Description |
|---|---|
| `When Workflow "name" is triggered` | Invokes the workflow trigger without a body. Works for Recurrence and HTTP triggers. Populates the run cache. |
| `When Workflow "name" is triggered with file "filename"` | Sends file contents as the trigger body. File must be in `TestData\`. Populates the run cache. |
| `When Workflow "name" is triggered with json file "filename"` | Same as above but sets `Content-Type: application/json`. Populates the run cache. |

### Action Assertion Steps

| Step | Description |
|---|---|
| `Then The workflow executed these actions:` | Validates actions against the cached run(s). Depth-first search at any nesting level. |
| `Then The workflow "name" executed these actions:` | Fetches the named workflow run, stores it in cache, then validates. |
| `Then The "action" action has status "status"` | Validates a single action status. Depth-first search. |

Table format (used by all action assertion steps):

```gherkin
| StepName         | Status    |
| Action name here | Succeeded |
```

Valid status values: `Succeeded`, `Failed`, `Skipped`, `TimedOut`, `Cancelled`, `Waiting`, `Running`.

### Scope Steps

| Step | Description |
|---|---|
| `Then Within "scope name":` | Validates actions that are direct children of the named scope. |
| `Then In scope "scope name":` | Alternative syntax for `Within`. |

### Path-Based Navigation Step

| Step | Description |
|---|---|
| `Then In "path":` | Navigates the action tree using dot-separated path syntax and validates actions at that location. Also available as `Given In "path":` and `When In "path":`. |

See [Path-Based Navigation](#path-based-navigation) for the full syntax.

### Condition Branch Steps

| Step | Description |
|---|---|
| `Then In the "branch" branch of "condition name":` | Validates actions in the named branch of a condition. |
| `Then In condition "condition name" branch "branch":` | Alternative syntax. |

Branch values: `actions` for the TRUE branch, `else` for the FALSE branch.

### Loop Steps

| Step | Description |
|---|---|
| `Then The "loop name" loop ran N times with status "status"` | Validates the total iteration count across all matching loops. |
| `Then Each iteration of "loop name" executed:` | Validates that the listed actions ran in every iteration. |
| `Then All iterations of "loop name" executed:` | Alternative syntax for `Each iteration of`. |
| `Then In iteration N of "loop name":` | Validates actions in a specific 1-based iteration. |
| `Then Iteration N of "loop name" executed:` | Alternative syntax for `In iteration N of`. |
| `Then The nested "inner loop" loop in iteration N of "outer loop" ran N times with status "status"` | Validates the iteration count of a nested loop within a specific parent iteration. |

### Correlated Workflow Step

| Step | Description |
|---|---|
| `Then For all instances of "name" with the same correlation:` | Finds all runs of the named workflow sharing the current correlation ID, stores them in cache, and validates each. |

After this step executes, all subsequent assertion steps in the scenario operate against **every** matched run.

### Transformation Steps

Available when your step definition class inherits from `BaseTransformationStepDefinition<TSource, TDestination>`.

| Step | Description |
|---|---|
| `Given A message with a data from the source system` | Initialises an empty `TSource` message object. |
| `Given A file with a data from the source system` | Alternative phrasing  -  same behaviour. |
| `Given It has the following source data:` | Fills properties on the message using a `Field`/`Value` table. Supports dot-notation and indexed list notation e.g. `Items[0].Name`. |
| `Given It has content from a file named "filename"` | Loads the source message by deserialising a JSON file from `TestData\`. |
| `When The message payload is put in Storage Account container "container" with file name "filename"` | Serialises the message to JSON and uploads it to blob storage. Generates a correlation ID. |
| `When The claim-check is put on Service Bus topic "topic"` | Sends a bare claim-check message to a topic. |
| `When The claim-check is put on Service Bus queue "queue"'` | Sends a bare claim-check message to a queue. |
| `When The claim-check is put on Service Bus topic "topic" with properties:` | Sends a claim-check to a topic with additional application properties from a `Field`/`Value` table. |
| `When The claim-check is put on Service Bus queue "queue" with properties:` | Same as above for a queue. |
| `Then Workflow step "step name" has transformed the data` | Finds the named action output, deserialises it via `DeserializeTransformedBody`, and stores it for subsequent assertions. |
| `Then The transformed data has "field" with value "value"` | Asserts a single field on the transformed output using dot-notation path. |
| `Then The transformed data has "field" with values` | Asserts multiple sub-fields using a `Field`/`Value` table. Useful for arrays and complex objects. |

---

## Path-Based Navigation

The `Then In "path":` step accepts a dot-separated path that navigates the action tree to a specific location before running the table assertion.

### Syntax Reference

| Component | Syntax | Example |
|---|---|---|
| Scope or action name | Plain text | `Try` |
| Path separator | `.` | `Try.For each number` |
| Loop iteration (1-based) | `[N]` | `For each number[1]` |
| Condition TRUE branch | `.actions` | `Condition in try.actions` |
| Condition FALSE branch | `.else` | `Condition in try.else` |
| Switch case | `.CaseName` | `Switch.Default` |

> **All iteration indices are 1-based.** The first iteration is `[1]`, not `[0]`.

### Path Examples from the Repository

**Navigate into a Try scope:**

```gherkin
And In "Try":
    | StepName        | Status    |
    | For each number | Succeeded |
    | Until           | Succeeded |
```

**ForEach  -  specific iteration:**

```gherkin
And In "Try.For each number[1]":
    | StepName                               | Status    |
    | For each letter                        | Succeeded |
    | Variable testString2 value less than 3 | Succeeded |
```

**ForEach  -  triple-nested, specific iterations:**

```gherkin
And In "Try.For each number[1].For each letter[2].For each character[3]":
    | StepName                | Status    |
    | Set variable testString | Succeeded |
```

**Until  -  specific iteration:**

```gherkin
And In "Try.Until[1]":
    | StepName                        | Status    |
    | Until2                          | Succeeded |
    | Reset variable counter2         | Succeeded |
    | Set variable teststring until   | Succeeded |
    | Increment variable counter      | Succeeded |
    | Condition                       | Succeeded |
```

**Until  -  triple-nested, specific iterations:**

```gherkin
And In "Try.Until[1].Until2[2].Until3[2]":
    | StepName                    | Status    |
    | Increment variable counter3 | Succeeded |
```

**Condition inside a Until iteration:**

```gherkin
And In "Try.Until[1].Condition":
    | StepName                    | Status  |
    | Set variable untilCompleted | Skipped |
```

**Condition TRUE branch  -  path syntax:**

```gherkin
And In "Try.Condition in try.actions":
    | StepName                             | Status    |
    | Set variable testString in condition | Succeeded |
```

**Condition TRUE branch inside a ForEach iteration:**

```gherkin
And In "Try.For each number[1].Variable testString2 value less than 3.actions":
    | StepName                      | Status    |
    | Set variable testString2 true | Succeeded |
```

**Condition FALSE branch (empty table  -  asserts the path is reachable):**

```gherkin
And In "Try.For each number[1].Variable testString2 value less than 3.else":
    | StepName | Status |
```

**Switch case  -  path syntax:**

```gherkin
And In "Try.Switch.Default":
    | StepName               | Status    |
    | Set variable in switch | Succeeded |
```

**Switch case in a correlated processor run:**

```gherkin
And In "Try.Check http status code.Default":
    | StepName                                        | Status    |
    | Set variable errorMessage to http response body | Succeeded |
    | Throw exception                                 | Succeeded |
```

---

## Caching and Performance

### How the Cache Works

Any of the following steps populates an internal list of current workflow runs. All subsequent assertion steps in the same scenario reuse that list without making additional API calls.

**Steps that populate the cache:**

| Step | Runs cached |
|---|---|
| `When Workflow "name" is triggered` | One run |
| `When Workflow "name" is triggered with file "..."` | One run |
| `When Workflow "name" is triggered with json file "..."` | One run |
| `Then The workflow "name" executed these actions:` | One run (also validates) |
| `Then For all instances of "name" with the same correlation:` | Multiple runs (also validates) |

**Cache lifecycle:**
- Populated by any of the steps above
- Reused by all following `Then`/`And` assertion steps in the same scenario
- Replaced when another cache-populating step executes
- Disposed at scenario completion

### Recommended Pattern  -  Trigger Once, Assert Many Times

```gherkin
When Workflow "prc-nestedloops-and-do-until" is triggered    # 1 API call  -  cached

Then The workflow executed these actions:                     # uses cache
    | StepName             | Status    |
    | Initialize variables | Succeeded |

And The "Set variable" action has status "Succeeded"          # uses cache

And In "Try":                                                 # uses cache
    | StepName        | Status    |
    | For each number | Succeeded |

And All iterations of "For each number" executed:             # uses cache
    | StepName        | Status    |
    | For each letter | Succeeded |
```

### Pattern to Avoid  -  Repeating Explicit Workflow Names

```gherkin
# Each line re-fetches the run and replaces the cache
Then The workflow "prc" executed these actions:
    | StepName | Status |
And The workflow "prc" executed these actions:
    | StepName | Status |
```

---

## Integration Testing (Receive-Process-Send)

### HTTP-Triggered Receive Workflow

Taken from `Rcv-Prc-Snd-SampleStepDefinition.feature`:

```gherkin
Scenario: Validate http request with file content

    When Workflow "rcvHttp" is triggered with file "sample-http-request-content.json"

    Then The workflow executed these actions:
        | StepName             | Status    |
        | Initialize variables | Succeeded |

    And In "Try":
        | StepName             | Status    |
        | Validate json schema | Succeeded |
```

### Full Receive-Process-Send Chain

```gherkin
Scenario: Validate complete workflow chain

    When Workflow "rcv" is triggered

    Then The workflow executed these actions:
        | StepName                          | Status    |
        | Initialize variable sampleMessage | Succeeded |
        | Initialize variable correlationId | Succeeded |
        | Initialize variable counter       | Succeeded |
        | Initialize variable errorMessage  | Succeeded |
        | Initialize variable               | Succeeded |

    And In "Try":
        | StepName             | Status    |
        | Validate json schema | Succeeded |

    And All iterations of "For each item in items" executed:
        | StepName                          | Status    |
        | Increment variable counter        | Succeeded |
        | Write message payload to storage  | Succeeded |
        | Send claim-check message to topic | Succeeded |

    And In "Try":
        | StepName                                         | Status    |
        | Send success tracked properties to table storage | Succeeded |
        | Send success tracked properties to log analytics | Succeeded |

    Then For all instances of "prc" with the same correlation:
        | StepName                                   | Status    |
        | Receive new claim-check message from queue | Succeeded |
        | Initialize variable correlationId          | Succeeded |
        | Initialize variable errorMessage           | Succeeded |

    And In "Try":
        | StepName                                        | Status    |
        | Retrieve message payload from storage           | Succeeded |
        | Transform source data type to target data type  | Succeeded |
        | Write message payload to storage                | Succeeded |
        | Send claim-check message to queue               | Succeeded |
        | Complete claim-check message in queue           | Succeeded |
        | Send success tracked properties to log analytics | Succeeded |

    Then For all instances of "snd" with the same correlation:
        | StepName                                   | Status    |
        | Receive new claim-check message from queue | Succeeded |
        | Initialize variable correlationId          | Succeeded |
        | Initialize variable errorMessage           | Succeeded |

    And In "Try":
        | StepName                              | Status    |
        | Retrieve message payload from storage | Succeeded |
```

### Validating an Error Path

Assert `Failed` or other statuses when a workflow is expected to hit an error path:

```gherkin
Then For all instances of "prc" with the same correlation:
    | StepName                                   | Status    |
    | Receive new claim-check message from queue | Succeeded |
    | Initialize variable correlationId          | Succeeded |
    | Initialize variable errorMessage           | Succeeded |

And In "Try":
    | StepName                              | Status    |
    | Retrieve message payload from storage | Succeeded |
    | Post to google.com api                | Failed    |

And In "Try.Check http status code.Default":
    | StepName                                        | Status    |
    | Set variable errorMessage to http response body | Succeeded |
    | Throw exception                                 | Succeeded |
```

---

## Transformation Testing

### Creating a Transformation Step Definition Class

Inherit from `BaseTransformationStepDefinition<TSource, TDestination>` and implement `DeserializeTransformedBody` for the format your workflow produces:

```csharp
// XSLT -> XML output
[Binding, Scope(Feature = "Transformation-Test-Sample-StepDefinition")]
public class SampleTransformationTestStepDefinition : BaseTransformationStepDefinition<Source, Destination>
{
    protected override Destination DeserializeTransformedBody(string body)
    {
        using TextReader reader = new StringReader(body);
        return (Destination)new XmlSerializer(typeof(Destination)).Deserialize(reader)!;
    }
}

// Liquid -> JSON output
[Binding, Scope(Feature = "My-Json-Transformation-Feature")]
public class MyJsonTransformationStepDefinition : BaseTransformationStepDefinition<MySourceModel, MyDestinationModel>
{
    protected override MyDestinationModel DeserializeTransformedBody(string body)
        => JsonConvert.DeserializeObject<MyDestinationModel>(body)!;
}
```

### Table-Driven Source Data

Taken from `Transformation-Test-SampleStepDefinition.feature`:

```gherkin
Scenario: Transform source message to destination message

    Given A message with a data from the source system
    And It has the following source data:
        | Field   | Value          |
        | Value   | Open Sesame    |
        | OnClick | OpenMagicGate()|

    When The message payload is put in Storage Account container "demoworkload" with file name "rcv-source-data.json"
    And The claim-check is put on Service Bus topic "sbt-sourcesystem-out" with properties:
        | Field        | Value               |
        | sender       | sendingWorkflowName |
        | messageType  | demoMessageType     |
        | someProperty | someValue           |

    Then The workflow "prc" executed these actions:
        | StepName                                       | Status    |
        | Transform source data type to target data type | Succeeded |

    And Workflow step "Transform source data type to target data type" has transformed the data
    And The transformed data has "Status" with value "Open Sesame"
    And The transformed data has "Action" with value "OpenMagicGate()"
```

### File-Based Source Data

Use a pre-prepared JSON file when the source payload is large or complex:

```gherkin
Scenario: Transform source message to destination message from a file

    Given A message with a data from the source system
    And It has content from a file named "sample-transformation-test-input.json"

    When The message payload is put in Storage Account container "demoworkload" with file name "rcv-source-data.json"
    And The claim-check is put on Service Bus topic "sbt-sourcesystem-out" with properties:
        | Field        | Value               |
        | sender       | sendingWorkflowName |
        | messageType  | demoMessageType     |
        | someProperty | someValue           |

    Then The workflow "prc" executed these actions:
        | StepName                                       | Status    |
        | Transform source data type to target data type | Succeeded |

    And Workflow step "Transform source data type to target data type" has transformed the data
    And The transformed data has "Status" with value "Open Sesame"
    And The transformed data has "Action" with value "OpenMagicGate()"
```

### Asserting Nested and Array Properties

`The transformed data has "field" with value "value"` resolves field paths using dot-notation. `The transformed data has "field" with values` asserts multiple sub-fields in a single step:

```gherkin
And The transformed data has "Status" with value "Open Sesame"

# Dot-notation for a nested object
And The transformed data has "Header.CorrelationId" with value "abc-123"

# Array index + multiple sub-field assertions
And The transformed data has "Items[0]" with values
    | Field    | Value      |
    | Quantity | 5          |
    | Sku      | WIDGET-001 |
    | Price    | 12.50      |
```

The field path uses the same syntax as the [Pather.CSharp](https://github.com/Meitinger/Pather.CSharp) library.

---

## Advanced Scenarios

### Complete Nested Loop, Condition, and Switch Example

This scenario is the full content of `Foreach-Until-SampleStepDefinition.feature` and covers every navigation path type in the framework:

```gherkin
Scenario: Validate complete workflow with nested structures

    When Workflow "prc-nestedloops-and-do-until" is triggered

    # ===== Top-level actions =====
    Then The workflow executed these actions:
        | StepName             | Status    |
        | Recurrence           | Succeeded |
        | Initialize variables | Succeeded |
        | Set variable         | Succeeded |

    And The "Set variable" action has status "Succeeded"

    # ===== Try scope =====
    And In "Try":
        | StepName        | Status    |
        | For each number | Succeeded |
        | Until           | Succeeded |

    # ===== Specific ForEach iteration =====
    And In "Try.For each number[1]":
        | StepName                               | Status    |
        | For each letter                        | Succeeded |
        | Variable testString2 value less than 3 | Succeeded |

    # ===== All iterations of a ForEach =====
    And All iterations of "For each number" executed:
        | StepName                               | Status    |
        | For each letter                        | Succeeded |
        | Variable testString2 value less than 3 | Succeeded |

    # ===== Triple-nested ForEach, specific iterations =====
    And In "Try.For each number[1].For each letter[2].For each character[3]":
        | StepName                | Status    |
        | Set variable testString | Succeeded |

    # ===== All iterations at the deepest ForEach level =====
    And All iterations of "For each character" executed:
        | StepName                | Status    |
        | Set variable testString | Succeeded |

    # ===== Specific Until iteration =====
    And In "Try.Until[1]":
        | StepName                        | Status    |
        | Until2                          | Succeeded |
        | Reset variable counter2         | Succeeded |
        | Set variable teststring until   | Succeeded |
        | Increment variable counter      | Succeeded |
        | Condition                       | Succeeded |

    # ===== Condition inside a Until iteration =====
    And In "Try.Until[1].Condition":
        | StepName                    | Status  |
        | Set variable untilCompleted | Skipped |

    # ===== Triple-nested Until, specific iterations =====
    And In "Try.Until[1].Until2[2].Until3[2]":
        | StepName                    | Status    |
        | Increment variable counter3 | Succeeded |

    # ===== All iterations of Until =====
    And All iterations of "Until" executed:
        | StepName                        | Status    |
        | Until2                          | Succeeded |
        | Reset variable counter2         | Succeeded |
        | Set variable teststring until   | Succeeded |
        | Increment variable counter      | Succeeded |
        | Condition                       | Succeeded |

    And All iterations of "Until3" executed:
        | StepName                    | Status    |
        | Increment variable counter3 | Succeeded |

    # ===== Condition in Try scope =====
    And In "Try":
        | StepName         | Status    |
        | Condition in try | Succeeded |

    # ===== Condition TRUE branch  -  explicit step =====
    And In the "actions" branch of "Condition in try":
        | StepName                             | Status    |
        | Set variable testString in condition | Succeeded |

    # ===== Condition TRUE branch  -  path syntax (equivalent) =====
    And In "Try.Condition in try.actions":
        | StepName                             | Status    |
        | Set variable testString in condition | Succeeded |

    # ===== Condition inside ForEach  -  TRUE branch =====
    And In "Try.For each number[1].Variable testString2 value less than 3.actions":
        | StepName                      | Status    |
        | Set variable testString2 true | Succeeded |

    # ===== Condition inside ForEach  -  FALSE branch =====
    And In "Try.For each number[1].Variable testString2 value less than 3.else":
        | StepName | Status |

    # ===== Switch in Try scope =====
    And In "Try":
        | StepName | Status    |
        | Switch   | Succeeded |

    # ===== Switch case  -  explicit step =====
    And In the "Default" branch of "Switch":
        | StepName               | Status    |
        | Set variable in switch | Succeeded |

    # ===== Switch case  -  path syntax (equivalent) =====
    And In "Try.Switch.Default":
        | StepName               | Status    |
        | Set variable in switch | Succeeded |
```

### Nested Loop Iteration Count

Assert how many times a nested loop ran within a specific iteration of its parent:

```gherkin
Then The "For each number" loop ran 3 times with status "Succeeded"
And The nested "For each letter" loop in iteration 1 of "For each number" ran 4 times with status "Succeeded"
```

---

## Best Practices

### Trigger Once, Assert Many Times

```gherkin
# Correct
When Workflow "prc-nestedloops-and-do-until" is triggered
Then The workflow executed these actions:
    | StepName | Status |
And In "Try":
    | StepName | Status |
And All iterations of "For each number" executed:
    | StepName | Status |
```

```gherkin
# Avoid: each explicit name re-fetches and replaces the cache
Then The workflow "prc" executed these actions:
    | StepName | Status |
And The workflow "prc" executed these actions:
    | StepName | Status |
```

### Use Depth-First Search for Status Checks

```gherkin
# Correct: no path needed when you only care about status
Then The workflow executed these actions:
    | StepName                                       | Status    |
    | Transform source data type to target data type | Succeeded |
```

Reserve `In "path":` for structural assertions where the iteration index, condition branch, or switch case matters.

### Validate All Iterations Together

```gherkin
# Correct
And All iterations of "For each item in items" executed:
    | StepName                          | Status    |
    | Write message payload to storage  | Succeeded |
    | Send claim-check message to topic | Succeeded |
```

```gherkin
# Avoid: repeating the assertion per iteration
And In iteration 1 of "For each item in items":
    | StepName                         | Status    |
    | Write message payload to storage | Succeeded |
And In iteration 2 of "For each item in items":
    | StepName                         | Status    |
    | Write message payload to storage | Succeeded |
```

Use specific iteration steps only when expected actions differ between iterations.

### Group Validations in One Scenario

```gherkin
# Correct
Scenario: Validate complete workflow
    When Workflow "prc" is triggered
    Then The workflow executed these actions:
        | StepName | Status |
    And The "action" action has status "Succeeded"
    And In "Try":
        | StepName | Status |
    And All iterations of "For each item" executed:
        | StepName | Status |
```

### Comment Deep Paths

```gherkin
# Path: Try -> ForEach number [1] -> Condition -> TRUE branch
And In "Try.For each number[1].Variable testString2 value less than 3.actions":
    | StepName                      | Status    |
    | Set variable testString2 true | Succeeded |
```

---

## Troubleshooting

### "No workflow run is available"

An assertion step executed before any trigger or context-setting step.

```gherkin
# Wrong
Then The workflow executed these actions:
When Workflow "prc" is triggered

# Correct
When Workflow "prc" is triggered
Then The workflow executed these actions:
```

### Action Not Found

- `Then The workflow executed these actions:` and `Then The "X" action has status "Y"` are **case-insensitive** and search the full depth of the tree
- `Then In "path":` resolves path segments case-insensitively, but the final action name must match `DesignerName` or `Name` exactly

The framework prints all available action names to the test console output on failure. Check there to find the correct name.

### "No actions found at path"

1. Confirm all iteration indices are **1-based**: `[1]` not `[0]`
2. Condition branches: `actions` (TRUE) and `else` (FALSE)
3. Switch case names must match exactly as they appear in the workflow designer
4. Check the test console output  -  available actions at the failing path segment are printed there

### "Loop ran X times, but Y was expected"

When multiple loops share the same name at different nesting depths, the count is the **total** across all matching loops. Use path syntax to target a specific instance:

```gherkin
# Total across all "For each item" loops
Then The "For each item" loop ran 9 times with status "Succeeded"

# Outer loop only, first iteration
And In "For each item[1]":
    | StepName | Status |

# Outer iteration 1, inner iteration 1
And In "For each item[1].For each item[1]":
    | StepName | Status |
```

### Tests Are Slow

Explicit workflow name steps re-fetch the run on every call. Use `When Workflow "name" is triggered` once:

```gherkin
# Slow
Then The workflow "prc" executed these actions:
And The workflow "prc" executed these actions:

# Fast
When Workflow "prc" is triggered
Then The workflow executed these actions:
And The "action" action has status "Succeeded"
And In "Try":
    | StepName | Status |
```

---

## Appendix

### Supported Action Types

- Scope actions (Try, Catch, Finally, named scopes)
- Condition actions (true/false branches)
- Switch actions (named cases and Default)
- ForEach loop actions
- Until loop actions
- Standard connector actions (HTTP, Compose, Service Bus, Storage, etc.)

### File Layout

| File | Purpose |
|---|---|
| `BaseStepDefinition.cs` | All trigger and assertion step definitions |
| `BaseTransformationStepDefinition.cs` | Transformation-specific step definitions |
| `ActionPathNavigator.cs` | Path-based tree navigation |
| `WorkflowRunValidation.cs` | Validation logic for runs, loops, scopes, conditions |
| `WorkflowRunNavigator.cs` | Low-level action tree traversal |

### Iteration Index Convention

All iteration indices throughout the framework use **1-based numbering**:

- First iteration: `[1]`
- Second iteration: `[2]`
- Third iteration: `[3]`

---

**Document Version:** 2.0
**Last Updated:** June 2025
**Compatible with:** .NET 10 and Reqnroll
