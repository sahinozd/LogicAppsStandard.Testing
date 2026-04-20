# Azure Logic Apps Workflow Testing Framework - Reference Guide

## Table of Contents
1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Path-Based Navigation](#path-based-navigation)
4. [Context-Aware Step Definitions](#context-aware-step-definitions)
5. [Performance Optimization](#performance-optimization)
6. [Testing Patterns](#testing-patterns)
7. [Advanced Examples](#advanced-examples)
8. [Migration Guide](#migration-guide)
9. [Best Practices](#best-practices)

---

## Overview

The Azure Logic Apps Workflow Testing Framework provides a comprehensive testing solution for Azure Logic Apps workflows using Reqnroll and SpecFlow. This framework enables automated validation of workflow execution, including complex nested structures, conditional logic, and loop iterations.

### Key Capabilities

**Performance**
- Cached workflow run instances reduce API calls by 90-97%
- Single API call per test scenario through intelligent caching
- Test execution time reduced from minutes to seconds

**Flexibility**
- Path-based navigation for intuitive access to nested workflow structures
- Context-aware step definitions eliminate repetitive workflow name references
- Deep action search finds any action regardless of nesting depth
- Support for all Logic Apps action types including scopes, conditions, switches, and loops
- Correlated multi-workflow validation with automatic context propagation

**Maintainability**
- Tests written in business-readable Gherkin syntax
- Comprehensive validation methods for both simple and complex workflows
- Backward compatibility with existing test suites

---

## Quick Start

### Basic Workflow Validation

The following example demonstrates basic workflow validation using context-aware step definitions:

```gherkin
Scenario: Validate basic workflow execution
    When Workflow "order-processing" is triggered

    Then The workflow executed these actions:
        | StepName             | Status    |
        | Recurrence           | Succeeded |
        | Initialize variables | Succeeded |
        | Process order        | Succeeded |
        | Send notification    | Succeeded |
```

### Nested Action Validation

Actions at any nesting depth are found automatically — no path navigation required:

```gherkin
Scenario: Validate action inside a Try scope
    When Workflow "prc" is triggered

    Then The workflow executed these actions:
        | StepName                              | Status    |
        | Transform source data to target type  | Succeeded |
```

Even though "Transform source data to target type" lives inside a "Try" scope, the step finds it automatically through a full depth-first search of the action tree.

### Loop Validation

Validate loop execution with iteration count and status verification:

```gherkin
Scenario: Validate loop execution
    When Workflow "batch-processor" is triggered

    Then The "For each item" loop ran 10 times with status "Succeeded"

    And Each iteration of "For each item" executed:
        | StepName         | Status    |
        | Validate item    | Succeeded |
        | Process item     | Succeeded |
        | Update inventory | Succeeded |
```

### Nested Structure Validation

Access deeply nested workflow structures using path navigation:

```gherkin
Scenario: Validate nested workflow structures
    When Workflow "customer-onboarding" is triggered

    Then In "Validate request.Check duplicates.else.For each customer[1]":
        | StepName         | Status    |
        | Validate data    | Succeeded |
        | Create account   | Succeeded |
```

---

## Path-Based Navigation

Path-based navigation provides an intuitive syntax for accessing nested workflow structures. This approach eliminates the need for complex table structures and reduces test verbosity.

### Path Syntax Components

| Component | Syntax | Example | Description |
|-----------|--------|---------|-------------|
| Action Name | "ActionName" | "Process order" | Specifies the action to navigate to |
| Iteration Index | [N] | [1] | 1-based iteration number for loop actions |
| Path Separator | . | "Scope.Action" | Separates path components |
| Condition TRUE Branch | .actions | "Check.actions" | Accesses the TRUE branch of a condition |
| Condition FALSE Branch | .else | "Check.else" | Accesses the FALSE branch of a condition |
| Switch Case | .CaseName | "Switch.Premium" | Accesses a specific switch case |

### Iteration Indexing

**Important:** All iteration indices use 1-based numbering, where the first iteration is represented as [1], not [0]. This convention aligns with business-oriented language and reduces cognitive load for non-technical stakeholders.

### Path Navigation Examples

**Scope Navigation**
```gherkin
Then In "Try":
    | StepName        | Status    |
    | Validate input  | Succeeded |
    | Process data    | Succeeded |
```

**Specific Loop Iteration**
```gherkin
Then In "For each customer[1]":
    | StepName       | Status    |
    | Validate email | Succeeded |
    | Create record  | Succeeded |
```

**Nested Loop Navigation**
```gherkin
Then In "For each department[2].For each employee[5]":
    | StepName         | Status    |
    | Calculate salary | Succeeded |
    | Update database  | Succeeded |
```

**Condition Branch in Loop**
```gherkin
Then In "For each order[3].Validate amount.actions":
    | StepName         | Status    |
    | Process payment  | Succeeded |
    | Send confirmation| Succeeded |
```

**Switch Case in Scope**
```gherkin
Then In "Process request.Route by priority.High":
    | StepName            | Status    |
    | Escalate to manager | Succeeded |
    | Send alert          | Succeeded |
```

**Complex Nested Path**
```gherkin
Then In "Try.For each region[1].Switch on type.Retail.For each store[2].Check inventory.actions":
    | StepName        | Status    |
    | Reorder stock   | Succeeded |
    | Update system   | Succeeded |
```

---

## Context-Aware Step Definitions

Context-aware step definitions automatically track the currently executing workflow and its run instance. This eliminates the need to specify the workflow name in every validation step, reducing test verbosity and improving maintainability.

### Caching Mechanism

The framework maintains a list of current workflow runs. This list is populated by any of the following steps:

- `When Workflow "name" is triggered` — sets one run in the list
- `When Workflow "name" is triggered with file "file"` — sets one run in the list
- `When Workflow "name" is triggered with json file "file"` — sets one run in the list
- `Then The workflow "name" executed these actions:` — sets one run in the list **and** validates
- `Then For all instances of "name" with the same correlation:` — sets multiple runs in the list **and** validates

Once the list is populated, all subsequent assertion steps in the scenario operate against **every run in the list** — whether that is one triggered run or several correlated instances. This means a `For all instances` step followed by further `Then` assertions will validate each of those correlated runs.

**Cache lifecycle:**
- **Populated** when any of the steps above executes
- **Reused** by all following `Then`/`And` assertion steps within the same scenario
- **Replaced** when a new trigger or context-setting step executes
- **Disposed** at scenario completion

### Deep Action Search

All action-level assertion steps use a **full depth-first search** of the action tree. An action inside a Try scope, a Condition branch, a ForEach iteration, or any other container is found without needing a path prefix.

```gherkin
# This finds "Transform data" even if it is nested inside "Try > Validate > actions"
Then The workflow executed these actions:
    | StepName       | Status    |
    | Transform data | Succeeded |
```

Use path navigation when you need to **target a specific structural location**, such as validating actions within a particular loop iteration or condition branch.

### Available Step Definitions

#### Workflow Trigger Steps

**Trigger Without Data**
```gherkin
When Workflow "workflow-name" is triggered
```

**Trigger With File Input**
```gherkin
When Workflow "workflow-name" is triggered with file "test-data.json"
```
Note: File must exist in the `TestData` directory relative to the test project.

**Trigger With JSON File Input**
```gherkin
When Workflow "workflow-name" is triggered with json file "test-data.json"
```
Equivalent to the previous step but automatically sets `Content-Type: application/json` on the request body. File must exist in the `TestData` directory.

#### Top-Level Action Validation

**Context-Aware Validation (Recommended)**
```gherkin
Then The workflow executed these actions:
    | StepName | Status    |
    | Action1  | Succeeded |
    | Action2  | Succeeded |
```
Validates against the currently cached workflow run(s). Actions are found at any nesting depth.

**Explicit Workflow Name**
```gherkin
Then The workflow "specific-workflow" executed these actions:
    | StepName | Status    |
    | Action1  | Succeeded |
```
Fetches the run for the named workflow, stores it in the cache, and validates. All subsequent steps in the scenario then operate against this workflow's run. Actions are found at any nesting depth.

#### Single Action Status Validation

```gherkin
Then The "Initialize variables" action has status "Succeeded"
Then The "HTTP request" action has status "Failed"
Then The "Send email" action has status "Skipped"
```

Uses a full depth-first search — the action can be at any nesting level.

#### Loop Iteration Count Validation

Validates that all loops with the specified name have executed the expected number of iterations. If multiple loops with the same name exist at different nesting levels, the framework validates all of them collectively.

```gherkin
Then The "For each item" loop ran 5 times with status "Succeeded"
Then The "Until complete" loop ran 3 times with status "Succeeded"
```

**Important:** If your workflow contains multiple loops with the same name (e.g., nested loops), the iteration count represents the total across all matching loops. Use path navigation to validate specific loop instances.

#### All Iterations Validation

Validates that specified actions exist in every iteration of the named loop(s).

```gherkin
Then Each iteration of "For each customer" executed:
    | StepName         | Status    |
    | Validate data    | Succeeded |
    | Create account   | Succeeded |
```

**Alternative Syntax:**
```gherkin
Then All iterations of "For each customer" executed:
    | StepName         | Status    |
    | Validate data    | Succeeded |
```

#### Specific Iteration Validation

Validates actions within a particular iteration of a loop.

```gherkin
Then In iteration 1 of "For each order":
    | StepName         | Status    |
    | Process payment  | Succeeded |
    | Update inventory | Succeeded |
```

**Alternative Syntax:**
```gherkin
Then Iteration 2 of "For each order" executed:
    | StepName         | Status    |
    | Process payment  | Succeeded |
```

#### Scope and Child Action Validation

**Scope Validation**
```gherkin
Then Within "Try scope":
    | StepName       | Status    |
    | Validate input | Succeeded |
    | Process data   | Succeeded |
```

**Alternative Syntax:**
```gherkin
Then In scope "Try scope":
    | StepName       | Status    |
    | Validate input | Succeeded |
```

#### Condition Branch Validation

**Using Explicit Step Definitions**
```gherkin
Then In the "actions" branch of "Check valid":
    | StepName           | Status    |
    | Process valid data | Succeeded |

Then In condition "Check valid" branch "else":
    | StepName             | Status    |
    | Handle invalid data  | Succeeded |
```

**Using Path Navigation (Recommended)**
```gherkin
Then In "Check valid.actions":
    | StepName           | Status    |
    | Process valid data | Succeeded |

Then In "Check valid.else":
    | StepName             | Status    |
    | Handle invalid data  | Succeeded |
```

Branch names:
- `actions` represents the TRUE branch (when condition evaluates to true)
- `else` represents the FALSE branch (when condition evaluates to false)

#### Switch Case Validation

**Using Explicit Step Definitions**
```gherkin
Then In the "Premium" branch of "Route by tier":
    | StepName            | Status    |
    | Apply premium logic | Succeeded |

Then In the "Default" branch of "Route by tier":
    | StepName            | Status    |
    | Apply default logic | Succeeded |
```

**Using Path Navigation (Recommended)**
```gherkin
Then In "Route by tier.Premium":
    | StepName            | Status    |
    | Apply premium logic | Succeeded |

Then In "Route by tier.Standard":
    | StepName             | Status    |
    | Apply standard logic | Succeeded |

Then In "Route by tier.Default":
    | StepName            | Status    |
    | Apply default logic | Succeeded |
```

#### Path-Based Navigation

The most flexible validation approach for complex nested structures.

```gherkin
Then In "path.to.action[iteration].branch":
    | StepName | Status    |
    | Action1  | Succeeded |
```

Examples:
```gherkin
Then In "Try.For each customer[1].Validate.actions.Process":
    | StepName      | Status    |
    | Create record | Succeeded |
```

#### Nested Loop Validation

Validates a nested loop within a specific iteration of a parent loop.

```gherkin
Then The nested "Process items" loop in iteration 1 of "For each customer" ran 5 times with status "Succeeded"
```

#### Correlated Workflow Validation

Validates all workflow instances that share the same correlation identifier. This is useful for testing multi-workflow integration scenarios where workflows communicate via messages or triggers.

```gherkin
Then For all instances of "processor-workflow" with the same correlation:
    | StepName                    | Status    |
    | Receive claim-check message | Succeeded |
    | Read from storage           | Succeeded |
    | Transform data              | Succeeded |
    | Send to downstream          | Succeeded |
```

The correlation identifier is automatically captured when the initial workflow is triggered. All subsequent workflows with matching correlation identifiers are validated together. After this step, the list of current workflow runs contains all correlated instances, so any following `Then`/`And` assertion steps will validate against **each of those runs**.

```gherkin
Then For all instances of "processor-workflow" with the same correlation:
    | StepName       | Status    |
    | Transform data | Succeeded |

# This validates "Transform data" in every correlated run
And The "Transform data" action has status "Succeeded"
```

---

## Performance Optimization

### Caching Architecture

The framework implements intelligent caching to minimize Azure API calls and reduce test execution time.

**Without Caching (Previous Approach):**
```gherkin
Scenario: Inefficient validation
    Then The workflow "my-workflow" executed these actions:  # API call + sets cache
        | StepName | Status |
    And The workflow "my-workflow" executed these actions:   # API call + replaces cache
        | StepName | Status |
    And The "For each item" loop ran 5 times...              # uses cache
    And Each iteration of "For each item"...                 # uses cache
```
Result: 2+ API calls, unnecessary re-fetches

**With Caching (Current Approach):**
```gherkin
Scenario: Optimized validation
    When Workflow "my-workflow" is triggered                  # 1 API call, sets cache
    Then The workflow executed these actions:                 # uses cache
        | StepName | Status |
    And The "For each item" loop ran 5 times...              # uses cache
    And Each iteration of "For each item"...                 # uses cache
    And In "Try.For each item[1]":                           # uses cache
```
Result: 1 API call, 3 seconds execution time

### Performance Comparison

| Test Complexity | Without Cache | With Cache | Improvement |
|-----------------|---------------|------------|-------------|
| 10 validation steps | 30 seconds, 30 API calls | 3 seconds, 1 API call | 90% faster |
| 20 validation steps | 60 seconds, 60 API calls | 3 seconds, 1 API call | 95% faster |
| 50 validation steps | 150 seconds, 150 API calls | 5 seconds, 1 API call | 97% faster |

### Cache Lifecycle

**Cache Populated:** When any of these steps executes — "When Workflow is triggered", "Then The workflow 'X' executed these actions:", or "Then For all instances of 'X' with the same correlation:"
**Cache Used:** All `Then`/`And` assertion steps within the same scenario that follow a cache-populating step
**Cache Replaced:** When a new cache-populating step executes
**Cache Disposed:** At scenario completion

### Optimization Guidelines

**Recommended Approach:**
```gherkin
Scenario: Single workflow, multiple validations
    When Workflow "order-processor" is triggered
    Then The workflow executed these actions:
        | StepName | Status |
    And The "For each item" loop ran 10 times
    And Each iteration of "For each item" executed:
        | StepName | Status |
    And In "Try.Validate order.actions":
        | StepName | Status |
```

**Also Efficient — Explicit Name Populates Cache Once:**
```gherkin
Scenario: Named workflow trigger, then context-aware assertions
    Then The workflow "order-processor" executed these actions:  # fetches + caches
        | StepName | Status |
    And The "For each item" loop ran 10 times                    # uses cache
    And In "Try.Validate order.actions":                         # uses cache
        | StepName | Status |
```

**Avoid This — Explicit Name Repeated, Replaces Cache Each Time:**
```gherkin
Scenario: Inefficient - multiple named workflow fetches
    Then The workflow "order-processor" executed these actions:  # fetches + caches
        | StepName | Status |
    And The workflow "order-processor" executed these actions:   # fetches again + replaces cache
        | StepName | Status |
```

**Group Related Validations:**
Group all validations for a single workflow run into one scenario to maximize cache efficiency.

**Minimize Explicit Workflow Names:**
Use context-aware steps (without explicit workflow names) to leverage caching automatically. When you do need an explicit name, use it once and let subsequent steps use the cache.

---

## Testing Patterns

This section provides proven patterns for testing common workflow scenarios.

### Pattern 1: Actions Inside Scopes

**Scenario:** Validate actions that are nested inside a Try scope without needing path navigation.

```gherkin
Scenario: Validate transformation inside a scope
    When Workflow "prc" is triggered

    Then The workflow executed these actions:
        | StepName                             | Status    |
        | Transform source data to target type | Succeeded |

    And The transformed data has "Status" with value "Open Sesame"
```

The workflow-level step finds "Transform source data to target type" through a full depth-first search, regardless of the scope it lives in. There is no need for a preceding `In "Try":` step to load context.

### Pattern 2: Condition Validation in Scopes

**Scenario:** Validate conditional logic within a scope action.

```gherkin
Scenario: Validate error handling with conditional logic
    When Workflow "error-handler" is triggered

    Then Within "Try scope":
        | StepName           | Status    |
        | Validate input     | Succeeded |
        | Check data quality | Succeeded |

    And In the "actions" branch of "Check data quality":
        | StepName           | Status    |
        | Process valid data | Succeeded |
        | Save to database   | Succeeded |

    And In condition "Check data quality" branch "else":
        | StepName            | Status    |
        | Log invalid data    | Succeeded |
        | Send error alert    | Succeeded |
```

**Alternative using path navigation:**
```gherkin
Scenario: Validate error handling with conditional logic
    When Workflow "error-handler" is triggered

    Then In "Try scope.Check data quality.actions":
        | StepName           | Status    |
        | Process valid data | Succeeded |
        | Save to database   | Succeeded |

    And In "Try scope.Check data quality.else":
        | StepName            | Status    |
        | Log invalid data    | Succeeded |
        | Send error alert    | Succeeded |
```

### Pattern 3: Switch Case Validation in Scopes

**Scenario:** Validate switch statement logic for customer tier routing.

```gherkin
Scenario: Validate customer tier routing
    When Workflow "customer-router" is triggered

    Then Within "Process customer":
        | StepName         | Status    |
        | Route by tier    | Succeeded |

    And In "Process customer.Route by tier.Premium":
        | StepName               | Status    |
        | Apply premium discount | Succeeded |
        | Assign priority queue  | Succeeded |

    And In "Process customer.Route by tier.Standard":
        | StepName                | Status    |
        | Apply standard discount | Succeeded |
        | Assign normal queue     | Succeeded |

    And In "Process customer.Route by tier.Default":
        | StepName          | Status    |
        | Apply no discount | Succeeded |
        | Assign low queue  | Succeeded |
```

### Pattern 4: Conditions in Loop Iterations

**Scenario:** Validate conditional logic that varies across loop iterations.

```gherkin
Scenario: Validate order processing with conditional approval
    When Workflow "order-processor" is triggered

    Then The "For each order" loop ran 5 times with status "Succeeded"

    # Iteration 1: High-value order (condition = true)
    And In "For each order[1].Check order value.actions":
        | StepName            | Status    |
        | Request approval    | Succeeded |
        | Wait for response   | Succeeded |

    # Iteration 2: Low-value order (condition = false)
    And In "For each order[2].Check order value.else":
        | StepName            | Status    |
        | Auto-approve        | Succeeded |
        | Process immediately | Succeeded |
```

### Pattern 5: Switch Cases in Loop Iterations

**Scenario:** Validate switch logic within loop iterations based on item priority.

```gherkin
Scenario: Validate task routing based on priority
    When Workflow "task-processor" is triggered

    Then The "For each task" loop ran 10 times with status "Succeeded"

    # High priority task (iteration 1)
    And In "For each task[1].Route by priority.High":
        | StepName              | Status    |
        | Assign to specialist  | Succeeded |
        | Set SLA to 2 hours    | Succeeded |

    # Medium priority task (iteration 5)
    And In "For each task[5].Route by priority.Medium":
        | StepName             | Status    |
        | Assign to generalist | Succeeded |
        | Set SLA to 8 hours   | Succeeded |

    # Low priority task (iteration 8)
    And In "For each task[8].Route by priority.Low":
        | StepName            | Status    |
        | Assign to queue     | Succeeded |
        | Set SLA to 24 hours | Succeeded |
```

### Pattern 6: Nested Loops with Conditional Logic

**Scenario:** Validate complex nested structures with conditional branches.

```gherkin
Scenario: Validate hierarchical data processing
    When Workflow "organization-processor" is triggered

    Then The "For each department" loop ran 3 times with status "Succeeded"

    And In "For each department[1]":
        | StepName          | Status    |
        | For each employee | Succeeded |

    And In "For each department[1].For each employee[2].Check performance.actions":
        | StepName           | Status    |
        | Calculate bonus    | Succeeded |
        | Schedule promotion | Succeeded |

    And In "For each department[1].For each employee[2].Check performance.else":
        | StepName                | Status    |
        | Schedule review meeting | Succeeded |
        | Create improvement plan | Succeeded |
```

### Pattern 7: Multiple Loops with Same Name

**Scenario:** Validate workflows containing multiple loops with identical names at different nesting levels.

```gherkin
Scenario: Validate nested processing loops
    When Workflow "data-processor" is triggered

    # Workflow structure:
    # - "Process batch" (outer loop) - 3 iterations
    #   └─ "Process batch" (inner loop) - 2 iterations per outer = 6 total

    # Validates ALL loops named "Process batch"
    Then The "Process batch" loop ran 9 times with status "Succeeded"

    # Validates all 9 iterations across both loops
    And Each iteration of "Process batch" executed:
        | StepName      | Status    |
        | Validate data | Succeeded |
        | Transform     | Succeeded |

    # Validate specific outer loop iteration
    And In "Process batch[1]":
        | StepName           | Status    |
        | Aggregate results  | Succeeded |

    # Validate specific inner loop iteration
    And In "Process batch[1].Process batch[1]":
        | StepName           | Status    |
        | Process individual | Succeeded |
```

### Pattern 8: Try-Catch-Finally Error Handling

**Scenario:** Validate complete error handling workflow with try-catch-finally pattern.

```gherkin
Scenario: Validate comprehensive error handling
    When Workflow "resilient-processor" is triggered

    Then In "Try":
        | StepName           | Status    |
        | Validate input     | Succeeded |
        | Process data       | Succeeded |
        | Check results      | Succeeded |

    And In "Try.Check results.actions":
        | StepName          | Status    |
        | Save results      | Succeeded |
        | Send confirmation | Succeeded |

    And In "Try.Check results.else":
        | StepName          | Status    |
        | Log warning       | Succeeded |
        | Retry processing  | Succeeded |

    And In "Catch":
        | StepName              | Status    |
        | Log error details     | Succeeded |
        | Send alert to admin   | Succeeded |
        | Create incident       | Succeeded |

    And In "Finally":
        | StepName            | Status    |
        | Cleanup temp files  | Succeeded |
        | Release resources   | Succeeded |
```

### Pattern 9: Correlated Multi-Workflow with Follow-Up Assertions

**Scenario:** After validating correlated instances, continue asserting on each of them.

```gherkin
Scenario: Validate processor instances and their transformations
    When Workflow "receiver" is triggered with file "messages.json"

    Then The workflow executed these actions:
        | StepName                 | Status    |
        | Parse incoming message   | Succeeded |
        | Write to blob storage    | Succeeded |
        | Send claim-check message | Succeeded |

    And For all instances of "processor" with the same correlation:
        | StepName                    | Status    |
        | Receive claim-check message | Succeeded |
        | Read from blob storage      | Succeeded |
        | Transform data              | Succeeded |

    # These assertions run against every correlated processor run
    And The "Transform data" action has status "Succeeded"
    And The "Read from blob storage" action has status "Succeeded"
```

---

## Advanced Examples

This section demonstrates validation strategies for complex, real-world workflow scenarios.

### Example 1: Multi-Level Nested Loop Validation

**Business Context:** Validate a hierarchical organization structure processor that handles departments, teams, and employees.

```gherkin
Scenario: Validate organization hierarchy processing
    When Workflow "organization-sync" is triggered

    # Level 1: Department loop
    Then The "For each department" loop ran 3 times with status "Succeeded"

    # Level 2: Team loop within department 1
    And The nested "For each team" loop in iteration 1 of "For each department" ran 4 times with status "Succeeded"

    # Level 3: Employee loop using path navigation
    And In "For each department[1].For each team[1]":
        | StepName             | Status    |
        | For each team member | Succeeded |

    # Validate specific employee processing (deepest level)
    And In "For each department[1].For each team[2].For each team member[3]":
        | StepName                | Status    |
        | Validate employee data  | Succeeded |
        | Calculate compensation  | Succeeded |
        | Update HR system        | Succeeded |
```

### Example 2: Parallel Branch Processing with Conditions

**Business Context:** Validate parallel processing where multiple branches execute simultaneously, each containing conditional logic.

```gherkin
Scenario: Validate parallel order fulfillment
    When Workflow "order-fulfillment" is triggered

    Then In "Parallel processing":
        | StepName          | Status    |
        | Inventory branch  | Succeeded |
        | Payment branch    | Succeeded |
        | Shipping branch   | Succeeded |

    # Validate inventory branch with stock check
    And In "Parallel processing.Inventory branch.Check stock level.actions":
        | StepName         | Status    |
        | Reserve items    | Succeeded |
        | Update inventory | Succeeded |

    And In "Parallel processing.Inventory branch.Check stock level.else":
        | StepName         | Status    |
        | Initiate backorder | Succeeded |
        | Notify customer  | Succeeded |

    # Validate payment branch with amount verification
    And In "Parallel processing.Payment branch.Verify amount.actions":
        | StepName         | Status    |
        | Process payment  | Succeeded |
        | Generate receipt | Succeeded |

    # Validate shipping branch with address validation
    And In "Parallel processing.Shipping branch.Validate address.actions":
        | StepName                | Status    |
        | Calculate shipping cost | Succeeded |
        | Generate label          | Succeeded |
```

### Example 3: Correlated Multi-Workflow Integration

**Business Context:** Validate a receive-process-send pattern where a receiver workflow triggers multiple processor instances that share correlation.

```gherkin
Scenario: Validate distributed customer data processing
    When Workflow "customer-receiver" is triggered with file "customers.json"

    # Validate receiver workflow
    Then The workflow executed these actions:
        | StepName                 | Status    |
        | Receive HTTP request     | Succeeded |
        | Validate JSON schema     | Succeeded |
        | Parse customer data      | Succeeded |

    And The "For each customer" loop ran 50 times with status "Succeeded"

    And Each iteration of "For each customer" executed:
        | StepName                 | Status    |
        | Write to blob storage    | Succeeded |
        | Send claim-check message | Succeeded |

    # Validate all correlated processor instances
    And For all instances of "customer-processor" with the same correlation:
        | StepName                    | Status    |
        | Receive claim-check message | Succeeded |
        | Read from blob storage      | Succeeded |
        | Validate customer data      | Succeeded |
        | Transform to CRM format     | Succeeded |
        | Send to CRM API             | Succeeded |
        | Archive processed data      | Succeeded |

    # Continue asserting against all correlated processor runs
    And The "Transform to CRM format" action has status "Succeeded"
```

### Example 4: Complex Business Logic with Multiple Decision Points

**Business Context:** Validate a customer onboarding workflow with multiple validation checks, routing decisions, and approval workflows.

```gherkin
Scenario: Validate enterprise customer onboarding
    When Workflow "customer-onboarding" is triggered

    # Initial validation phase
    Then In "Validate request":
        | StepName            | Status    |
        | Parse JSON          | Succeeded |
        | Validate schema     | Succeeded |
        | Check duplicates    | Succeeded |

    # No duplicates found - proceed with onboarding
    And In "Validate request.Check duplicates.else":
        | StepName                | Status    |
        | Route by customer type  | Succeeded |

    # Enterprise customer routing
    And In "Validate request.Check duplicates.else.Route by customer type.Enterprise":
        | StepName          | Status    |
        | For each contract | Succeeded |

    # Process first enterprise contract
    And In "Validate request.Check duplicates.else.Route by customer type.Enterprise.For each contract[1]":
        | StepName           | Status    |
        | Validate contract  | Succeeded |
        | Check credit score | Succeeded |

    # Good credit score - auto-approval path
    And In "Validate request.Check duplicates.else.Route by customer type.Enterprise.For each contract[1].Check credit score.actions":
        | StepName              | Status    |
        | Auto-approve contract | Succeeded |
        | Create account        | Succeeded |
        | Generate credentials  | Succeeded |
        | Send welcome email    | Succeeded |

    # Poor credit score - manual review path
    And In "Validate request.Check duplicates.else.Route by customer type.Enterprise.For each contract[1].Check credit score.else":
        | StepName                  | Status    |
        | Create review ticket      | Succeeded |
        | Assign to underwriter     | Succeeded |
        | Send pending notification | Succeeded |
```

### Example 5: Data Transformation Pipeline with Quality Checks

**Business Context:** Validate a data transformation pipeline that processes records through multiple stages with quality validation at each step.

```gherkin
Scenario: Validate data transformation pipeline
    When Workflow "data-transformation-pipeline" is triggered

    # Stage 1: Data ingestion
    Then In "Stage 1 - Ingest":
        | StepName                | Status    |
        | Read from source        | Succeeded |
        | Validate file format    | Succeeded |
        | For each record         | Succeeded |

    And The "For each record" loop ran 1000 times with status "Succeeded"

    # Stage 2: Data cleansing (validate sample records)
    And In "Stage 1 - Ingest.For each record[1].Cleanse data.Check data quality.actions":
        | StepName              | Status    |
        | Standardize format    | Succeeded |
        | Remove duplicates     | Succeeded |
        | Enrich data           | Succeeded |

    And In "Stage 1 - Ingest.For each record[1].Cleanse data.Check data quality.else":
        | StepName           | Status    |
        | Log quality issue  | Succeeded |
        | Move to quarantine | Succeeded |

    # Stage 3: Data transformation
    And In "Stage 2 - Transform":
        | StepName                  | Status    |
        | Apply business rules      | Succeeded |
        | Calculate derived fields  | Succeeded |
        | Validate transformed data | Succeeded |

    # Stage 4: Data loading with error handling
    And In "Stage 3 - Load":
        | StepName            | Status    |
        | Load to destination | Succeeded |

    And In "Stage 3 - Load.Load to destination.Catch":
        | StepName           | Status    |
        | Log load error     | Succeeded |
        | Retry with backoff | Succeeded |
```

---

## Migration Guide

This section provides guidance for migrating existing tests to the new framework syntax.

### Migration Approach

The framework maintains backward compatibility with existing test suites. Migration can proceed incrementally:

1. Continue running existing tests without modification
2. Create new tests using the modern syntax
3. Migrate existing tests gradually as time permits
4. No breaking changes to existing step definitions

### Step 1: Workflow Trigger Migration

**Before:**
```gherkin
And The following actions ran for "order-processor":
    | StepName | Status    |
    | Action1  | Succeeded |
```

**After:**
```gherkin
When Workflow "order-processor" is triggered

Then The workflow executed these actions:
    | StepName | Status    |
    | Action1  | Succeeded |
```

**Benefits:**
- Clear test structure with explicit workflow trigger
- Enables caching for improved performance
- Better separation of test setup and validation

### Step 2: Nested Action Validation Simplification

**Before — required a path prefix to find a nested action:**
```gherkin
When Workflow "prc" is triggered

Then In "Try":
    | StepName       | Status    |
    | Transform data | Succeeded |
```

**After — the workflow-level step finds nested actions automatically:**
```gherkin
When Workflow "prc" is triggered

Then The workflow executed these actions:
    | StepName       | Status    |
    | Transform data | Succeeded |
```

**Benefit:** No need to know or expose the containing scope in the feature file. Use path navigation only when you need to target a specific structural location (e.g., a specific loop iteration or condition branch).

### Step 3: Loop Validation Simplification

**Before:**
```gherkin
And The loop action "For each item" in "order-processor" ran "10" times with status "Succeeded"
And The following actions ran for loop "For each item" iteration "1" in "order-processor":
    | StepName         | Status    |
    | Validate item    | Succeeded |
    | Process item     | Succeeded |
And The following actions ran for loop "For each item" iteration "2" in "order-processor":
    | StepName         | Status    |
    | Validate item    | Succeeded |
    | Process item     | Succeeded |
# ... repeat for all iterations
```

**After:**
```gherkin
And The "For each item" loop ran 10 times with status "Succeeded"

And Each iteration of "For each item" executed:
    | StepName         | Status    |
    | Validate item    | Succeeded |
    | Process item     | Succeeded |
```

**Benefits:**
- Reduced test code by 70-80%
- Single assertion validates all iterations
- Easier to maintain as iteration count changes

### Step 4: Nested Structure Navigation

**Before:**
```gherkin
| StepName | Parent           | Type          | Iteration | Status    |
| Loop2    | Loop1            | ForEachAction | 1         | Succeeded |
| Action1  | Loop1.Loop2      | Action        | 1         | Succeeded |
| Action2  | Loop1.Loop2      | Action        | 1         | Succeeded |
```

**After:**
```gherkin
And In "Loop1[1].Loop2[1]":
    | StepName | Status    |
    | Action1  | Succeeded |
    | Action2  | Succeeded |
```

**Benefits:**
- Intuitive path syntax replaces complex table structure
- Eliminates Parent/Type/Iteration columns
- More readable for non-technical stakeholders

### Step 5: Mixed Syntax During Transition

You can combine old and new syntax during migration:

```gherkin
Scenario: Mixed approach during migration
    # New syntax
    When Workflow "order-processor" is triggered

    Then The workflow executed these actions:
        | StepName | Status    |
        | Action1  | Succeeded |

    # Old syntax (still supported)
    And The following actions ran for "order-processor":
        | StepName | Status    |
        | Action2  | Succeeded |

    # New syntax
    And The "For each item" loop ran 5 times with status "Succeeded"
```

### Migration Checklist

- [ ] Identify tests to migrate
- [ ] Update workflow trigger steps
- [ ] Remove unnecessary path prefixes for flat action assertions (deep search handles them)
- [ ] Simplify loop validation
- [ ] Convert nested structure validation to path syntax where structural targeting is needed
- [ ] Remove redundant workflow name references after the first context-setting step
- [ ] Verify all tests pass
- [ ] Update test documentation

---

## Best Practices

### Test Structure

**Do:** Use context-aware step definitions
```gherkin
When Workflow "order-processor" is triggered
Then The workflow executed these actions:
    | StepName | Status |
And The "Process order" action has status "Succeeded"
```

**Avoid:** Repeating explicit workflow names
```gherkin
Then The workflow "order-processor" executed these actions:
    | StepName | Status |
And The workflow "order-processor" executed these actions:
    | StepName | Status |
```

**Rationale:** Context-aware steps leverage caching, reducing API calls and test execution time by 90-97%. Repeating an explicit name re-fetches the run and replaces the cache on each occurrence.

### Action Assertions

**Do:** Assert actions directly at the workflow level when you only need to verify status
```gherkin
When Workflow "prc" is triggered
Then The workflow executed these actions:
    | StepName       | Status    |
    | Transform data | Succeeded |
```

**Use path navigation** only when you need to target a specific structural location
```gherkin
And In "For each order[3].Check value.actions":
    | StepName       | Status    |
    | Process order  | Succeeded |
```

**Rationale:** The workflow-level step uses a full depth-first search and finds actions at any nesting depth. Path navigation adds precision, not discovery power.

### Path Navigation

**Do:** Use path navigation for complex structures requiring structural precision
```gherkin
And In "Try.For each item[1].Validate.actions":
    | StepName | Status |
```

**Avoid:** Multiple separate steps for the same path
```gherkin
And Within "Try":
And In iteration 1 of "For each item":
And In the "actions" branch of "Validate":
    | StepName | Status |
```

**Rationale:** Path navigation provides clearer intent and reduces test verbosity.

### Iteration Validation

**Do:** Validate all iterations when content is identical
```gherkin
And Each iteration of "For each item" executed:
    | StepName | Status |
```

**Avoid:** Validating each iteration separately
```gherkin
And In iteration 1 of "For each item":
    | StepName | Status |
And In iteration 2 of "For each item":
    | StepName | Status |
And In iteration 3 of "For each item":
    | StepName | Status |
```

**Exception:** Validate specific iterations only when content varies between iterations.

**Rationale:** Bulk validation reduces test maintenance and improves readability.

### Test Organization

**Do:** Group related validations in single scenario
```gherkin
Scenario: Validate complete order processing
    When Workflow "order-processor" is triggered
    Then The workflow executed these actions:
    And The "For each item" loop ran 10 times
    And Each iteration of "For each item" executed:
    And In "Validate order.actions":
```

**Avoid:** Splitting into many small scenarios
```gherkin
Scenario: Validate actions
    When Workflow "order-processor" is triggered
    Then The workflow executed these actions:

Scenario: Validate loop
    When Workflow "order-processor" is triggered
    Then The "For each item" loop ran 10 times
```

**Rationale:** Grouping validations maximizes cache efficiency and reduces total test execution time.

### Documentation

**Do:** Add comments for complex paths
```gherkin
# Path: Validation scope → Customer loop (iteration 1) → Credit check (TRUE branch)
And In "Validate request.For each customer[1].Check credit.actions":
    | StepName | Status |
```

**Do:** Use meaningful action names in tests
```gherkin
And In "Customer validation.For each account[1].Verify status.actions":
    | StepName            | Status    |
    | Update CRM database | Succeeded |
```

**Rationale:** Clear documentation improves test maintainability and reduces onboarding time for new team members.

### Error Handling

**Do:** Validate error paths explicitly
```gherkin
And In "Try":
    | StepName | Status |

And In "Catch":
    | StepName   | Status    |
    | Log error  | Succeeded |
    | Send alert | Succeeded |

And In "Finally":
    | StepName | Status    |
    | Cleanup  | Succeeded |
```

**Rationale:** Comprehensive error validation ensures workflow resilience.

### Performance Considerations

**Do:** Trigger once, assert many times
```gherkin
When Workflow "processor" is triggered
Then The workflow executed these actions:
And The "Action" action has status "Succeeded"
```

**Acceptable:** Named workflow step used once, then context-aware assertions
```gherkin
Then The workflow "processor" executed these actions:
    | StepName | Status |
And The "Action" action has status "Succeeded"
And In "Try.Nested action":
    | StepName | Status |
```

**Avoid:** Explicit workflow names used repeatedly
```gherkin
Then The workflow "processor" executed these actions:
And The workflow "processor" executed these actions:
```

**Rationale:** Each repeated explicit workflow name forces a new API call and replaces the cache.

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: "No workflow runs are available"

**Cause:** Assertion step executed before any workflow trigger or context-setting step

**Solution:**
```gherkin
# Incorrect order
Then The workflow executed these actions:
When Workflow "processor" is triggered

# Correct order
When Workflow "processor" is triggered
Then The workflow executed these actions:
```

#### Issue: Action not found despite being in the workflow

**Cause:** Action name in the feature file does not exactly match the designer name in the workflow definition

**Solution:**
1. Check the debug output in the test results — available action names are printed for each run
2. Verify the action name matches exactly (the comparison is case-sensitive for `In "path"` steps)
3. For `Then The workflow executed these actions:` and `Then The "X" action has status "Y"`, the search is case-insensitive and searches all nesting depths

#### Issue: "Loop ran X times, but Y was expected"

**Cause:** Multiple loops with the same name exist at different nesting levels

**Solution:**

When the framework reports "Loop #1 'Process': Loop ran 5 times, but 10 was expected", this indicates multiple loops named "Process" exist.

Example workflow structure:
- "Process" loop (outer) — 5 iterations
- "Process" loop (nested) — 10 iterations total (2 per outer iteration)

To validate the total across all loops:
```gherkin
Then The "Process" loop ran 15 times with status "Succeeded"
```

To validate specific loop instances:
```gherkin
# Outer loop only
And In "Process[1]":
    | StepName | Status |

# Inner loop (first iteration of outer, first iteration of inner)
And In "Process[1].Process[1]":
    | StepName | Status |
```

#### Issue: "No actions found at path"

**Cause:** Path syntax error or action name mismatch

**Solution:**

1. Verify action names match exactly (path navigation is case-insensitive)
2. Confirm iteration indices are 1-based, not 0-based
3. Validate branch names:
   - Conditions: `actions` (TRUE) or `else` (FALSE)
   - Switches: Use the exact case name from the workflow
4. Review the console output in test results — available action names at the failing path segment are printed

#### Issue: Follow-up assertions target the wrong workflow after a correlated step

**Cause:** `For all instances of "X" with the same correlation:` was not used before the assertion steps

**Solution:**

After `For all instances of "X"` executes, all subsequent assertion steps operate against every matched correlated run. Ensure the correlated step precedes the assertions you want scoped to those runs:

```gherkin
And For all instances of "processor" with the same correlation:
    | StepName | Status |

# These now target every correlated processor run
And The "Transform data" action has status "Succeeded"
```

#### Issue: Tests executing slowly

**Cause:** Explicit workflow name repeated multiple times, forcing repeated API calls

**Solution:**
```gherkin
# Slow - fetches run twice
Then The workflow "processor" executed these actions:
And The workflow "processor" executed these actions:

# Fast - fetches once, uses cache for the second assertion
When Workflow "processor" is triggered
Then The workflow executed these actions:
And The workflow executed these actions:
```

---

## Appendix

### Supported Action Types

The framework supports validation of all Azure Logic Apps action types:

- Scope actions
- Condition actions (If/Then/Else)
- Switch actions (multiple cases)
- ForEach loop actions
- Until loop actions
- Standard actions (HTTP, Compose, etc.)
- Trigger actions

### Status Values

Valid status values for action validation:

- Succeeded
- Failed
- Skipped
- TimedOut
- Cancelled
- Waiting
- Running

### Index Conventions

All iteration indices in the framework use 1-based numbering:
- First iteration: [1]
- Second iteration: [2]
- Third iteration: [3]

This convention aligns with business-oriented language and reduces confusion for non-technical stakeholders.

### File References

For detailed implementation information, refer to:
- `ActionPathNavigator.cs` — Path navigation implementation
- `WorkflowRunValidation.cs` — Validation logic implementation
- `WorkflowRunNavigator.cs` — Workflow navigation implementation
- `BaseStepDefinition.cs` — Step definition implementations

---

**Document Version:** 1.1
**Last Updated:** June 2025
**Framework Version:** Compatible with .NET 10 and Reqnroll/SpecFlow

## Table of Contents
1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Path-Based Navigation](#path-based-navigation)
4. [Context-Aware Step Definitions](#context-aware-step-definitions)
5. [Performance Optimization](#performance-optimization)
6. [Testing Patterns](#testing-patterns)
7. [Advanced Examples](#advanced-examples)
8. [Migration Guide](#migration-guide)
9. [Best Practices](#best-practices)

---

## Overview

The Azure Logic Apps Workflow Testing Framework provides a comprehensive testing solution for Azure Logic Apps workflows using Reqnroll and SpecFlow. This framework enables automated validation of workflow execution, including complex nested structures, conditional logic, and loop iterations.

### Key Capabilities

**Performance**
- Cached workflow run instances reduce API calls by 90-97%
- Single API call per test scenario through intelligent caching
- Test execution time reduced from minutes to seconds

**Flexibility**
- Path-based navigation for intuitive access to nested workflow structures
- Context-aware step definitions eliminate repetitive workflow name references
- Support for all Logic Apps action types including scopes, conditions, switches, and loops

**Maintainability**
- Tests written in business-readable Gherkin syntax
- Comprehensive validation methods for both simple and complex workflows
- Backward compatibility with existing test suites

---

## Quick Start

### Basic Workflow Validation

The following example demonstrates basic workflow validation using context-aware step definitions:

```gherkin
Scenario: Validate basic workflow execution
    When Workflow "order-processing" is triggered
    
    Then The workflow executed these actions:
        | StepName             | Status    |
        | Recurrence           | Succeeded |
        | Initialize variables | Succeeded |
        | Process order        | Succeeded |
        | Send notification    | Succeeded |
```

### Loop Validation

Validate loop execution with iteration count and status verification:

```gherkin
Scenario: Validate loop execution
    When Workflow "batch-processor" is triggered
    
    Then The "For each item" loop ran 10 times with status "Succeeded"
    
    And Each iteration of "For each item" executed:
        | StepName         | Status    |
        | Validate item    | Succeeded |
        | Process item     | Succeeded |
        | Update inventory | Succeeded |
```

### Nested Structure Validation

Access deeply nested workflow structures using path navigation:

```gherkin
Scenario: Validate nested workflow structures
    When Workflow "customer-onboarding" is triggered
    
    Then In "Validate request.Check duplicates.else.For each customer[1]":
        | StepName         | Status    |
        | Validate data    | Succeeded |
        | Create account   | Succeeded |
```

---

## Path-Based Navigation

Path-based navigation provides an intuitive syntax for accessing nested workflow structures. This approach eliminates the need for complex table structures and reduces test verbosity.

### Path Syntax Components

| Component | Syntax | Example | Description |
|-----------|--------|---------|-------------|
| Action Name | "ActionName" | "Process order" | Specifies the action to navigate to |
| Iteration Index | [N] | [1] | 1-based iteration number for loop actions |
| Path Separator | . | "Scope.Action" | Separates path components |
| Condition TRUE Branch | .actions | "Check.actions" | Accesses the TRUE branch of a condition |
| Condition FALSE Branch | .else | "Check.else" | Accesses the FALSE branch of a condition |
| Switch Case | .CaseName | "Switch.Premium" | Accesses a specific switch case |

### Iteration Indexing

**Important:** All iteration indices use 1-based numbering, where the first iteration is represented as [1], not [0]. This convention aligns with business-oriented language and reduces cognitive load for non-technical stakeholders.

### Path Navigation Examples

**Scope Navigation**
```gherkin
Then In "Try":
    | StepName        | Status    |
    | Validate input  | Succeeded |
    | Process data    | Succeeded |
```

**Specific Loop Iteration**
```gherkin
Then In "For each customer[1]":
    | StepName       | Status    |
    | Validate email | Succeeded |
    | Create record  | Succeeded |
```

**Nested Loop Navigation**
```gherkin
Then In "For each department[2].For each employee[5]":
    | StepName         | Status    |
    | Calculate salary | Succeeded |
    | Update database  | Succeeded |
```

**Condition Branch in Loop**
```gherkin
Then In "For each order[3].Validate amount.actions":
    | StepName         | Status    |
    | Process payment  | Succeeded |
    | Send confirmation| Succeeded |
```

**Switch Case in Scope**
```gherkin
Then In "Process request.Route by priority.High":
    | StepName            | Status    |
    | Escalate to manager | Succeeded |
    | Send alert          | Succeeded |
```

**Complex Nested Path**
```gherkin
Then In "Try.For each region[1].Switch on type.Retail.For each store[2].Check inventory.actions":
    | StepName        | Status    |
    | Reorder stock   | Succeeded |
    | Update system   | Succeeded |
```

---

## Context-Aware Step Definitions

Context-aware step definitions automatically track the currently executing workflow and its run instance. This eliminates the need to specify the workflow name in every validation step, reducing test verbosity and improving maintainability.

### Caching Mechanism

When a workflow is triggered, the framework:
1. Executes the workflow trigger
2. Waits for workflow completion
3. Fetches the workflow run instance (one API call)
4. Caches the workflow run for all subsequent assertions

All validation steps within the same scenario reuse the cached workflow run, eliminating redundant API calls and significantly improving test execution performance.

### Available Step Definitions

#### Workflow Trigger Steps

**Trigger Without Data**
```gherkin
When Workflow "workflow-name" is triggered
```

**Trigger With File Input**
```gherkin
When Workflow "workflow-name" is triggered with file "test-data.json"
```
Note: File must exist in the TestData directory relative to the test project.

#### Top-Level Action Validation

**Context-Aware Validation (Recommended)**
```gherkin
Then The workflow executed these actions:
    | StepName | Status    |
    | Action1  | Succeeded |
    | Action2  | Succeeded |
```

**Explicit Workflow Name (For Cross-Workflow Validation)**
```gherkin
Then The workflow "specific-workflow" executed these actions:
    | StepName | Status    |
    | Action1  | Succeeded |
```

#### Single Action Status Validation

```gherkin
Then The "Initialize variables" action has status "Succeeded"
Then The "HTTP request" action has status "Failed"
Then The "Send email" action has status "Skipped"
```

#### Loop Iteration Count Validation

Validates that all loops with the specified name have executed the expected number of iterations. If multiple loops with the same name exist at different nesting levels, the framework validates all of them collectively.

```gherkin
Then The "For each item" loop ran 5 times with status "Succeeded"
Then The "Until complete" loop ran 3 times with status "Succeeded"
```

**Important:** If your workflow contains multiple loops with the same name (e.g., nested loops), the iteration count represents the total across all matching loops. Use path navigation to validate specific loop instances.

#### All Iterations Validation

Validates that specified actions exist in every iteration of the named loop(s).

```gherkin
Then Each iteration of "For each customer" executed:
    | StepName         | Status    |
    | Validate data    | Succeeded |
    | Create account   | Succeeded |
```

**Alternative Syntax:**
```gherkin
Then All iterations of "For each customer" executed:
    | StepName         | Status    |
    | Validate data    | Succeeded |
```

#### Specific Iteration Validation

Validates actions within a particular iteration of a loop.

```gherkin
Then In iteration 1 of "For each order":
    | StepName         | Status    |
    | Process payment  | Succeeded |
    | Update inventory | Succeeded |
```

**Alternative Syntax:**
```gherkin
Then Iteration 2 of "For each order" executed:
    | StepName         | Status    |
    | Process payment  | Succeeded |
```

#### Scope and Child Action Validation

**Scope Validation**
```gherkin
Then Within "Try scope":
    | StepName       | Status    |
    | Validate input | Succeeded |
    | Process data   | Succeeded |
```

**Alternative Syntax:**
```gherkin
Then In scope "Try scope":
    | StepName       | Status    |
    | Validate input | Succeeded |
```

#### Condition Branch Validation

**Using Explicit Step Definitions**
```gherkin
Then In the "actions" branch of "Check valid":
    | StepName           | Status    |
    | Process valid data | Succeeded |

Then In condition "Check valid" branch "else":
    | StepName             | Status    |
    | Handle invalid data  | Succeeded |
```

**Using Path Navigation (Recommended)**
```gherkin
Then In "Check valid.actions":
    | StepName           | Status    |
    | Process valid data | Succeeded |

Then In "Check valid.else":
    | StepName             | Status    |
    | Handle invalid data  | Succeeded |
```

Branch names:
- "actions" represents the TRUE branch (when condition evaluates to true)
- "else" represents the FALSE branch (when condition evaluates to false)

#### Switch Case Validation

**Using Explicit Step Definitions**
```gherkin
Then In the "Premium" branch of "Route by tier":
    | StepName            | Status    |
    | Apply premium logic | Succeeded |

Then In the "Default" branch of "Route by tier":
    | StepName          | Status    |
    | Apply default logic | Succeeded |
```

**Using Path Navigation (Recommended)**
```gherkin
Then In "Route by tier.Premium":
    | StepName            | Status    |
    | Apply premium logic | Succeeded |

Then In "Route by tier.Standard":
    | StepName             | Status    |
    | Apply standard logic | Succeeded |

Then In "Route by tier.Default":
    | StepName          | Status    |
    | Apply default logic | Succeeded |
```

#### Path-Based Navigation

The most flexible validation approach for complex nested structures.

```gherkin
Then In "path.to.action[iteration].branch":
    | StepName | Status    |
    | Action1  | Succeeded |
```

Examples:
```gherkin
Then In "Try.For each customer[1].Validate.actions.Process":
    | StepName      | Status    |
    | Create record | Succeeded |
```

#### Nested Loop Validation

Validates a nested loop within a specific iteration of a parent loop.

```gherkin
Then The nested "Process items" loop in iteration 1 of "For each customer" ran 5 times with status "Succeeded"
```

#### Correlated Workflow Validation

Validates all workflow instances that share the same correlation identifier. This is useful for testing multi-workflow integration scenarios where workflows communicate via messages or triggers.

```gherkin
Then For all instances of "processor-workflow" with the same correlation:
    | StepName                    | Status    |
    | Receive claim-check message | Succeeded |
    | Read from storage           | Succeeded |
    | Transform data              | Succeeded |
    | Send to downstream          | Succeeded |
```

The correlation identifier is automatically captured when the initial workflow is triggered. All subsequent workflows with matching correlation identifiers are validated together.

---

## Performance Optimization

### Caching Architecture

The framework implements intelligent caching to minimize Azure API calls and reduce test execution time.

**Without Caching (Previous Approach):**
```gherkin
Scenario: Inefficient validation
    Then The workflow "my-workflow" executed these actions:  # API call
        | StepName | Status |
    And The workflow "my-workflow" executed these actions:   # API call (duplicate)
        | StepName | Status |
    And The "For each item" loop ran 5 times...              # API call
    And Each iteration of "For each item"...                 # API call
```
Result: 10+ API calls, 30+ seconds execution time

**With Caching (Current Approach):**
```gherkin
Scenario: Optimized validation
    When Workflow "my-workflow" is triggered                  # 1 API call, cached
    Then The workflow executed these actions:                 # Uses cache
        | StepName | Status |
    And The "For each item" loop ran 5 times...              # Uses cache
    And Each iteration of "For each item"...                 # Uses cache
    And In "Try.For each item[1]":                           # Uses cache
```
Result: 1 API call, 3 seconds execution time

### Performance Comparison

| Test Complexity | Without Cache | With Cache | Improvement |
|-----------------|---------------|------------|-------------|
| 10 validation steps | 30 seconds, 30 API calls | 3 seconds, 1 API call | 90% faster |
| 20 validation steps | 60 seconds, 60 API calls | 3 seconds, 1 API call | 95% faster |
| 50 validation steps | 150 seconds, 150 API calls | 5 seconds, 1 API call | 97% faster |

### Cache Lifecycle

**Cache Initialization:** When a "When Workflow is triggered" step executes
**Cache Usage:** All "Then" and "And" validation steps within the same scenario
**Cache Refresh:** When a new "When Workflow is triggered" step executes
**Cache Disposal:** At scenario completion

### Optimization Guidelines

**Recommended Approach:**
```gherkin
Scenario: Single workflow, multiple validations
    When Workflow "order-processor" is triggered
    Then The workflow executed these actions:
        | StepName | Status |
    And The "For each item" loop ran 10 times
    And Each iteration of "For each item" executed:
        | StepName | Status |
    And In "Try.Validate order.actions":
        | StepName | Status |
```

**Avoid This Approach:**
```gherkin
Scenario: Inefficient - multiple workflow fetches
    Then The workflow "order-processor" executed these actions:  # Fetches workflow
        | StepName | Status |
    And The workflow "order-processor" executed these actions:   # Fetches workflow again
        | StepName | Status |
```

**Group Related Validations:**
Group all validations for a single workflow run into one scenario to maximize cache efficiency.

**Minimize Explicit Workflow Names:**
Use context-aware steps (without explicit workflow names) to leverage caching automatically.

---

## Testing Patterns

This section provides proven patterns for testing common workflow scenarios.

### Pattern 1: Condition Validation in Scopes

**Scenario:** Validate conditional logic within a scope action.

```gherkin
Scenario: Validate error handling with conditional logic
    When Workflow "error-handler" is triggered
    
    Then Within "Try scope":
        | StepName           | Status    |
        | Validate input     | Succeeded |
        | Check data quality | Succeeded |
    
    And In the "actions" branch of "Check data quality":
        | StepName           | Status    |
        | Process valid data | Succeeded |
        | Save to database   | Succeeded |
    
    And In condition "Check data quality" branch "else":
        | StepName            | Status    |
        | Log invalid data    | Succeeded |
        | Send error alert    | Succeeded |
```

**Alternative using path navigation:**
```gherkin
Scenario: Validate error handling with conditional logic
    When Workflow "error-handler" is triggered
    
    Then In "Try scope.Check data quality.actions":
        | StepName           | Status    |
        | Process valid data | Succeeded |
        | Save to database   | Succeeded |
    
    And In "Try scope.Check data quality.else":
        | StepName            | Status    |
        | Log invalid data    | Succeeded |
        | Send error alert    | Succeeded |
```

### Pattern 2: Switch Case Validation in Scopes

**Scenario:** Validate switch statement logic for customer tier routing.

```gherkin
Scenario: Validate customer tier routing
    When Workflow "customer-router" is triggered
    
    Then Within "Process customer":
        | StepName         | Status    |
        | Route by tier    | Succeeded |
    
    And In "Process customer.Route by tier.Premium":
        | StepName               | Status    |
        | Apply premium discount | Succeeded |
        | Assign priority queue  | Succeeded |
    
    And In "Process customer.Route by tier.Standard":
        | StepName                | Status    |
        | Apply standard discount | Succeeded |
        | Assign normal queue     | Succeeded |
    
    And In "Process customer.Route by tier.Default":
        | StepName          | Status    |
        | Apply no discount | Succeeded |
        | Assign low queue  | Succeeded |
```

### Pattern 3: Conditions in Loop Iterations

**Scenario:** Validate conditional logic that varies across loop iterations.

```gherkin
Scenario: Validate order processing with conditional approval
    When Workflow "order-processor" is triggered
    
    Then The "For each order" loop ran 5 times with status "Succeeded"
    
    # Iteration 1: High-value order (condition = true)
    And In "For each order[1].Check order value.actions":
        | StepName            | Status    |
        | Request approval    | Succeeded |
        | Wait for response   | Succeeded |
    
    # Iteration 2: Low-value order (condition = false)
    And In "For each order[2].Check order value.else":
        | StepName          | Status    |
        | Auto-approve      | Succeeded |
        | Process immediately | Succeeded |
```

### Pattern 4: Switch Cases in Loop Iterations

**Scenario:** Validate switch logic within loop iterations based on item priority.

```gherkin
Scenario: Validate task routing based on priority
    When Workflow "task-processor" is triggered
    
    Then The "For each task" loop ran 10 times with status "Succeeded"
    
    # High priority task (iteration 1)
    And In "For each task[1].Route by priority.High":
        | StepName              | Status    |
        | Assign to specialist  | Succeeded |
        | Set SLA to 2 hours    | Succeeded |
    
    # Medium priority task (iteration 5)
    And In "For each task[5].Route by priority.Medium":
        | StepName             | Status    |
        | Assign to generalist | Succeeded |
        | Set SLA to 8 hours   | Succeeded |
    
    # Low priority task (iteration 8)
    And In "For each task[8].Route by priority.Low":
        | StepName           | Status    |
        | Assign to queue    | Succeeded |
        | Set SLA to 24 hours| Succeeded |
```

### Pattern 5: Nested Loops with Conditional Logic

**Scenario:** Validate complex nested structures with conditional branches.

```gherkin
Scenario: Validate hierarchical data processing
    When Workflow "organization-processor" is triggered
    
    Then The "For each department" loop ran 3 times with status "Succeeded"
    
    And In "For each department[1]":
        | StepName          | Status    |
        | For each employee | Succeeded |
    
    And In "For each department[1].For each employee[2].Check performance.actions":
        | StepName           | Status    |
        | Calculate bonus    | Succeeded |
        | Schedule promotion | Succeeded |
    
    And In "For each department[1].For each employee[2].Check performance.else":
        | StepName                | Status    |
        | Schedule review meeting | Succeeded |
        | Create improvement plan | Succeeded |
```

### Pattern 6: Multiple Loops with Same Name

**Scenario:** Validate workflows containing multiple loops with identical names at different nesting levels.

```gherkin
Scenario: Validate nested processing loops
    When Workflow "data-processor" is triggered
    
    # Workflow structure:
    # - "Process batch" (outer loop) - 3 iterations
    #   └─ "Process batch" (inner loop) - 2 iterations per outer = 6 total
    
    # Validates ALL loops named "Process batch"
    Then The "Process batch" loop ran 9 times with status "Succeeded"
    
    # Validates all 9 iterations across both loops
    And Each iteration of "Process batch" executed:
        | StepName      | Status    |
        | Validate data | Succeeded |
        | Transform     | Succeeded |
    
    # Validate specific outer loop iteration
    And In "Process batch[1]":
        | StepName           | Status    |
        | Aggregate results  | Succeeded |
    
    # Validate specific inner loop iteration
    And In "Process batch[1].Process batch[1]":
        | StepName           | Status    |
        | Process individual | Succeeded |
```

### Pattern 7: Try-Catch-Finally Error Handling

**Scenario:** Validate complete error handling workflow with try-catch-finally pattern.

```gherkin
Scenario: Validate comprehensive error handling
    When Workflow "resilient-processor" is triggered
    
    Then In "Try":
        | StepName           | Status    |
        | Validate input     | Succeeded |
        | Process data       | Succeeded |
        | Check results      | Succeeded |
    
    And In "Try.Check results.actions":
        | StepName         | Status    |
        | Save results     | Succeeded |
        | Send confirmation| Succeeded |
    
    And In "Try.Check results.else":
        | StepName          | Status    |
        | Log warning       | Succeeded |
        | Retry processing  | Succeeded |
    
    And In "Catch":
        | StepName              | Status    |
        | Log error details     | Succeeded |
        | Send alert to admin   | Succeeded |
        | Create incident       | Succeeded |
    
    And In "Finally":
        | StepName            | Status    |
        | Cleanup temp files  | Succeeded |
        | Release resources   | Succeeded |
```

---

## Advanced Examples

This section demonstrates validation strategies for complex, real-world workflow scenarios.

### Example 1: Multi-Level Nested Loop Validation

**Business Context:** Validate a hierarchical organization structure processor that handles departments, teams, and employees.

```gherkin
Scenario: Validate organization hierarchy processing
    When Workflow "organization-sync" is triggered
    
    # Level 1: Department loop
    Then The "For each department" loop ran 3 times with status "Succeeded"
    
    # Level 2: Team loop within department 1
    And The nested "For each team" loop in iteration 1 of "For each department" ran 4 times with status "Succeeded"
    
    # Level 3: Employee loop using path navigation
    And In "For each department[1].For each team[1]":
        | StepName             | Status    |
        | For each team member | Succeeded |
    
    # Validate specific employee processing (deepest level)
    And In "For each department[1].For each team[2].For each team member[3]":
        | StepName                | Status    |
        | Validate employee data  | Succeeded |
        | Calculate compensation  | Succeeded |
        | Update HR system        | Succeeded |
```

### Example 2: Parallel Branch Processing with Conditions

**Business Context:** Validate parallel processing where multiple branches execute simultaneously, each containing conditional logic.

```gherkin
Scenario: Validate parallel order fulfillment
    When Workflow "order-fulfillment" is triggered
    
    Then In "Parallel processing":
        | StepName              | Status    |
        | Inventory branch      | Succeeded |
        | Payment branch        | Succeeded |
        | Shipping branch       | Succeeded |
    
    # Validate inventory branch with stock check
    And In "Parallel processing.Inventory branch.Check stock level.actions":
        | StepName                | Status    |
        | Reserve items           | Succeeded |
        | Update inventory        | Succeeded |
    
    And In "Parallel processing.Inventory branch.Check stock level.else":
        | StepName                | Status    |
        | Initiate backorder      | Succeeded |
        | Notify customer         | Succeeded |
    
    # Validate payment branch with amount verification
    And In "Parallel processing.Payment branch.Verify amount.actions":
        | StepName                | Status    |
        | Process payment         | Succeeded |
        | Generate receipt        | Succeeded |
    
    # Validate shipping branch with address validation
    And In "Parallel processing.Shipping branch.Validate address.actions":
        | StepName                | Status    |
        | Calculate shipping cost | Succeeded |
        | Generate label          | Succeeded |
```

### Example 3: Correlated Multi-Workflow Integration

**Business Context:** Validate a receive-process-send pattern where a receiver workflow triggers multiple processor instances that share correlation.

```gherkin
Scenario: Validate distributed customer data processing
    When Workflow "customer-receiver" is triggered with file "customers.json"
    
    # Validate receiver workflow
    Then The workflow executed these actions:
        | StepName                 | Status    |
        | Receive HTTP request     | Succeeded |
        | Validate JSON schema     | Succeeded |
        | Parse customer data      | Succeeded |
    
    And The "For each customer" loop ran 50 times with status "Succeeded"
    
    And Each iteration of "For each customer" executed:
        | StepName                 | Status    |
        | Write to blob storage    | Succeeded |
        | Send claim-check message | Succeeded |
    
    # Validate all correlated processor instances
    And For all instances of "customer-processor" with the same correlation:
        | StepName                    | Status    |
        | Receive claim-check message | Succeeded |
        | Read from blob storage      | Succeeded |
        | Validate customer data      | Succeeded |
        | Transform to CRM format     | Succeeded |
        | Send to CRM API             | Succeeded |
        | Archive processed data      | Succeeded |
```

### Example 4: Complex Business Logic with Multiple Decision Points

**Business Context:** Validate a customer onboarding workflow with multiple validation checks, routing decisions, and approval workflows.

```gherkin
Scenario: Validate enterprise customer onboarding
    When Workflow "customer-onboarding" is triggered
    
    # Initial validation phase
    Then In "Validate request":
        | StepName            | Status    |
        | Parse JSON          | Succeeded |
        | Validate schema     | Succeeded |
        | Check duplicates    | Succeeded |
    
    # No duplicates found - proceed with onboarding
    And In "Validate request.Check duplicates.else":
        | StepName                | Status    |
        | Route by customer type  | Succeeded |
    
    # Enterprise customer routing
    And In "Validate request.Check duplicates.else.Route by customer type.Enterprise":
        | StepName          | Status    |
        | For each contract | Succeeded |
    
    # Process first enterprise contract
    And In "Validate request.Check duplicates.else.Route by customer type.Enterprise.For each contract[1]":
        | StepName           | Status    |
        | Validate contract  | Succeeded |
        | Check credit score | Succeeded |
    
    # Good credit score - auto-approval path
    And In "Validate request.Check duplicates.else.Route by customer type.Enterprise.For each contract[1].Check credit score.actions":
        | StepName              | Status    |
        | Auto-approve contract | Succeeded |
        | Create account        | Succeeded |
        | Generate credentials  | Succeeded |
        | Send welcome email    | Succeeded |
    
    # Poor credit score - manual review path
    And In "Validate request.Check duplicates.else.Route by customer type.Enterprise.For each contract[1].Check credit score.else":
        | StepName                  | Status    |
        | Create review ticket      | Succeeded |
        | Assign to underwriter     | Succeeded |
        | Send pending notification | Succeeded |
```

### Example 5: Data Transformation Pipeline with Quality Checks

**Business Context:** Validate a data transformation pipeline that processes records through multiple stages with quality validation at each step.

```gherkin
Scenario: Validate data transformation pipeline
    When Workflow "data-transformation-pipeline" is triggered
    
    # Stage 1: Data ingestion
    Then In "Stage 1 - Ingest":
        | StepName                | Status    |
        | Read from source        | Succeeded |
        | Validate file format    | Succeeded |
        | For each record         | Succeeded |
    
    And The "For each record" loop ran 1000 times with status "Succeeded"
    
    # Stage 2: Data cleansing (validate sample records)
    And In "Stage 1 - Ingest.For each record[1].Cleanse data.Check data quality.actions":
        | StepName              | Status    |
        | Standardize format    | Succeeded |
        | Remove duplicates     | Succeeded |
        | Enrich data           | Succeeded |
    
    And In "Stage 1 - Ingest.For each record[1].Cleanse data.Check data quality.else":
        | StepName              | Status    |
        | Log quality issue     | Succeeded |
        | Move to quarantine    | Succeeded |
    
    # Stage 3: Data transformation
    And In "Stage 2 - Transform":
        | StepName                  | Status    |
        | Apply business rules      | Succeeded |
        | Calculate derived fields  | Succeeded |
        | Validate transformed data | Succeeded |
    
    # Stage 4: Data loading with error handling
    And In "Stage 3 - Load":
        | StepName              | Status    |
        | Load to destination   | Succeeded |
    
    And In "Stage 3 - Load.Load to destination.Catch":
        | StepName              | Status    |
        | Log load error        | Succeeded |
        | Retry with backoff    | Succeeded |
```

---

## Migration Guide

This section provides guidance for migrating existing tests to the new framework syntax.

### Migration Approach

The framework maintains backward compatibility with existing test suites. Migration can proceed incrementally:

1. Continue running existing tests without modification
2. Create new tests using the modern syntax
3. Migrate existing tests gradually as time permits
4. No breaking changes to existing step definitions

### Step 1: Workflow Trigger Migration

**Before:**
```gherkin
And The following actions ran for "order-processor":
    | StepName | Status    |
    | Action1  | Succeeded |
```

**After:**
```gherkin
When Workflow "order-processor" is triggered

Then The workflow executed these actions:
    | StepName | Status    |
    | Action1  | Succeeded |
```

**Benefits:**
- Clear test structure with explicit workflow trigger
- Enables caching for improved performance
- Better separation of test setup and validation

### Step 2: Loop Validation Simplification

**Before:**
```gherkin
And The loop action "For each item" in "order-processor" ran "10" times with status "Succeeded"
And The following actions ran for loop "For each item" iteration "1" in "order-processor":
    | StepName         | Status    |
    | Validate item    | Succeeded |
    | Process item     | Succeeded |
And The following actions ran for loop "For each item" iteration "2" in "order-processor":
    | StepName         | Status    |
    | Validate item    | Succeeded |
    | Process item     | Succeeded |
# ... repeat for all iterations
```

**After:**
```gherkin
And The "For each item" loop ran 10 times with status "Succeeded"

And Each iteration of "For each item" executed:
    | StepName         | Status    |
    | Validate item    | Succeeded |
    | Process item     | Succeeded |
```

**Benefits:**
- Reduced test code by 70-80%
- Single assertion validates all iterations
- Easier to maintain as iteration count changes

### Step 3: Nested Structure Navigation

**Before:**
```gherkin
| StepName | Parent           | Type          | Iteration | Status    |
| Loop2    | Loop1            | ForEachAction | 1         | Succeeded |
| Action1  | Loop1.Loop2      | Action        | 1         | Succeeded |
| Action2  | Loop1.Loop2      | Action        | 1         | Succeeded |
```

**After:**
```gherkin
And In "Loop1[1].Loop2[1]":
    | StepName | Status    |
    | Action1  | Succeeded |
    | Action2  | Succeeded |
```

**Benefits:**
- Intuitive path syntax replaces complex table structure
- Eliminates Parent/Type/Iteration columns
- More readable for non-technical stakeholders

### Step 4: Mixed Syntax During Transition

You can combine old and new syntax during migration:

```gherkin
Scenario: Mixed approach during migration
    # New syntax
    When Workflow "order-processor" is triggered
    
    Then The workflow executed these actions:
        | StepName | Status    |
        | Action1  | Succeeded |
    
    # Old syntax (still supported)
    And The following actions ran for "order-processor":
        | StepName | Status    |
        | Action2  | Succeeded |
    
    # New syntax
    And The "For each item" loop ran 5 times with status "Succeeded"
```

### Migration Checklist

- [ ] Identify tests to migrate
- [ ] Update workflow trigger steps
- [ ] Simplify loop validation
- [ ] Convert nested structure validation to path syntax
- [ ] Remove redundant workflow name references
- [ ] Verify all tests pass
- [ ] Update test documentation

---

## Best Practices

### Test Structure

**Do:** Use context-aware step definitions
```gherkin
When Workflow "order-processor" is triggered
Then The workflow executed these actions:
    | StepName | Status |
And The "Process order" action has status "Succeeded"
```

**Avoid:** Repeating explicit workflow names
```gherkin
Then The workflow "order-processor" executed these actions:
    | StepName | Status |
And The workflow "order-processor" executed these actions:
    | StepName | Status |
```

**Rationale:** Context-aware steps leverage caching, reducing API calls and test execution time by 90-97%.

### Path Navigation

**Do:** Use path navigation for complex structures
```gherkin
And In "Try.For each item[1].Validate.actions":
    | StepName | Status |
```

**Avoid:** Multiple separate steps for the same path
```gherkin
And Within "Try":
And In iteration 1 of "For each item":
And In the "actions" branch of "Validate":
    | StepName | Status |
```

**Rationale:** Path navigation provides clearer intent and reduces test verbosity.

### Iteration Validation

**Do:** Validate all iterations when content is identical
```gherkin
And Each iteration of "For each item" executed:
    | StepName | Status |
```

**Avoid:** Validating each iteration separately
```gherkin
And In iteration 1 of "For each item":
    | StepName | Status |
And In iteration 2 of "For each item":
    | StepName | Status |
And In iteration 3 of "For each item":
    | StepName | Status |
```

**Exception:** Validate specific iterations only when content varies between iterations.

**Rationale:** Bulk validation reduces test maintenance and improves readability.

### Test Organization

**Do:** Group related validations in single scenario
```gherkin
Scenario: Validate complete order processing
    When Workflow "order-processor" is triggered
    Then The workflow executed these actions:
    And The "For each item" loop ran 10 times
    And Each iteration of "For each item" executed:
    And In "Validate order.actions":
```

**Avoid:** Splitting into many small scenarios
```gherkin
Scenario: Validate actions
    When Workflow "order-processor" is triggered
    Then The workflow executed these actions:

Scenario: Validate loop
    When Workflow "order-processor" is triggered
    Then The "For each item" loop ran 10 times
```

**Rationale:** Grouping validations maximizes cache efficiency and reduces total test execution time.

### Documentation

**Do:** Add comments for complex paths
```gherkin
# Path: Validation scope → Customer loop (iteration 1) → Credit check (TRUE branch)
And In "Validate request.For each customer[1].Check credit.actions":
    | StepName | Status |
```

**Do:** Use meaningful action names in tests
```gherkin
And In "Customer validation.For each account[1].Verify status.actions":
    | StepName            | Status |
    | Update CRM database | Succeeded |
```

**Rationale:** Clear documentation improves test maintainability and reduces onboarding time for new team members.

### Error Handling

**Do:** Validate error paths explicitly
```gherkin
And In "Try":
    | StepName | Status |

And In "Catch":
    | StepName   | Status    |
    | Log error  | Succeeded |
    | Send alert | Succeeded |

And In "Finally":
    | StepName | Status    |
    | Cleanup  | Succeeded |
```

**Rationale:** Comprehensive error validation ensures workflow resilience.

### Performance Considerations

**Do:** Minimize workflow name specifications
```gherkin
When Workflow "processor" is triggered
Then The workflow executed these actions:
And The "Action" action has status "Succeeded"
```

**Avoid:** Explicit workflow names in assertions
```gherkin
Then The workflow "processor" executed these actions:
And The workflow "processor" executed these actions:
```

**Rationale:** Each explicit workflow name reference bypasses the cache and triggers an API call.

---

## Troubleshooting

### Common Issues and Solutions

#### Issue: "No workflow run is available"

**Cause:** Validation step executed before workflow trigger

**Solution:**
```gherkin
# Incorrect order
Then The workflow executed these actions:
When Workflow "processor" is triggered

# Correct order
When Workflow "processor" is triggered
Then The workflow executed these actions:
```

#### Issue: "Loop ran X times, but Y was expected"

**Cause:** Multiple loops with the same name exist at different nesting levels

**Solution:**

When the framework reports "Loop #1 'Process': Loop ran 5 times, but 10 was expected", this indicates multiple loops named "Process" exist.

Example workflow structure:
- "Process" loop (outer) - 5 iterations
- "Process" loop (nested) - 10 iterations total (2 per outer iteration)

To validate the total across all loops:
```gherkin
Then The "Process" loop ran 15 times with status "Succeeded"
```

To validate specific loop instances:
```gherkin
# Outer loop only
And In "Process[1]":
    | StepName | Status |

# Inner loop (first iteration of outer, first iteration of inner)
And In "Process[1].Process[1]":
    | StepName | Status |
```

#### Issue: "No actions found at path"

**Cause:** Path syntax error or action name mismatch

**Solution:**

1. Verify action names match exactly (comparison is case-insensitive)
2. Confirm iteration indices are 1-based, not 0-based
3. Validate branch names:
   - Conditions: "actions" (TRUE) or "else" (FALSE)
   - Switches: Use the exact case name from the workflow
4. Review debug output in test results for available action names

#### Issue: Tests executing slowly

**Cause:** Not using cached workflow run

**Solution:**
```gherkin
# Inefficient - forces API calls
Then The workflow "processor" executed these actions:
And The workflow "processor" executed these actions:

# Efficient - uses cache
When Workflow "processor" is triggered
Then The workflow executed these actions:
And The "Action" action has status "Succeeded"
```

---

## Appendix

### Supported Action Types

The framework supports validation of all Azure Logic Apps action types:

- Scope actions
- Condition actions (If/Then/Else)
- Switch actions (multiple cases)
- ForEach loop actions
- Until loop actions
- Standard actions (HTTP, Compose, etc.)
- Trigger actions

### Status Values

Valid status values for action validation:

- Succeeded
- Failed
- Skipped
- TimedOut
- Cancelled
- Waiting
- Running

### Index Conventions

All iteration indices in the framework use 1-based numbering:
- First iteration: [1]
- Second iteration: [2]
- Third iteration: [3]

This convention aligns with business-oriented language and reduces confusion for non-technical stakeholders.

### File References

For detailed implementation information, refer to:
- ActionPathNavigator.cs - Path navigation implementation
- WorkflowRunValidation.cs - Validation logic implementation
- WorkflowRunNavigator.cs - Workflow navigation implementation
- BaseStepDefinition.cs - Step definition implementations

---

**Document Version:** 1.0  
**Last Updated:** December 2024  
**Framework Version:** Compatible with .NET 10 and Reqnroll/SpecFlow
