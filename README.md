# Logic Apps Standard — Integration Testing Framework

A .NET testing framework for **Azure Logic Apps Standard** that provides an object-oriented C# API for interacting with Logic Apps at runtime and a Gherkin-based acceptance testing layer built on top of [Reqnroll](https://reqnroll.net/). It is designed to run as part of your DevOps deployment pipeline — after a deployment to a Development or Test environment — to perform full end-to-end integration testing directly against Azure.

---

## Table of Contents

1. [What This Framework Does](#what-this-framework-does)
2. [How It Compares to Microsoft's Built-In Testing Options](#how-it-compares-to-microsofts-built-in-testing-options)
3. [Prerequisites and Configuration](#prerequisites-and-configuration)
4. [IsMockingEnabled — Putting an Environment Under Test](#ismockingenabled--putting-an-environment-under-test)
5. [Using the Management Framework Directly in .NET](#using-the-management-framework-directly-in-net)
6. [Writing Tests in Gherkin](#writing-tests-in-gherkin)
7. [Unit Test Coverage](#unit-test-coverage)

---

## What This Framework Does

### The Problem

Azure Logic Apps Standard provides no first-class .NET API for interacting with workflow runs at runtime. When you want to test your integration logic after deployment, you are left writing raw HTTP calls to the Azure Management REST API, manually deserialising complex JSON, and building your own polling and retry logic. Testing nested structures — actions inside scopes, conditions, loops, switch cases — requires understanding deeply nested raw JSON responses. There is no object model, no abstraction, and no way to write readable, maintainable acceptance tests.

### The Solution

This framework delivers two things:

---

### 1. The Management Library — Object-Oriented Access to Logic Apps Standard

The `LogicApps.Management` library is a full object-oriented .NET model of Azure Logic Apps Standard. It wraps the Azure Management REST API and exposes everything you would want to interact with as strongly typed C# objects. The entire library is fully asynchronous.

**What you can do with it:**

- Retrieve a `LogicApp` instance representing your deployed Logic Apps Standard resource
- List all `Workflow` objects within the app, retrieve their definitions, and invoke their triggers
- Fetch `WorkflowRun` instances filtered by time range, correlation ID, or status
- Navigate the complete action tree of any run — top-level actions, actions inside **Scope** containers (Try/Catch/Finally), branches of **Condition** actions (true and false), cases of **Switch** actions, and individual iterations of **ForEach** and **Until** loops — all through a clean object model
- Find any action by name anywhere in the tree using a depth-first search, regardless of how deeply nested it is
- Read action inputs, outputs, status, error information, and tracked properties

Each action type is modelled with its own class: `ScopeAction`, `ConditionAction`, `SwitchAction`, `ForEachAction`, `UntilAction`, and the base `Action` for standard connectors. Repetitions within loops are themselves modelled as typed objects (`ForEachActionRepetition`, `UntilActionRepetition`), each capable of loading their own child actions on demand.

This means that from C#, reading a run's data looks like navigating an object graph — not parsing raw JSON.

---

### 2. The Specifications Layer — Gherkin-Based Acceptance Testing

The `LogicApps.TestFramework.Specifications` library wraps the Management library in a full set of Reqnroll step definitions, enabling business-readable Gherkin scenarios that test complete workflow chains end-to-end.

**What this enables:**

- **Trigger workflows** from test scenarios — by invoking the HTTP trigger directly, by uploading a payload to Azure Blob Storage and sending a claim-check message to Azure Service Bus, or by file-based input
- **Wait for completion** — the framework polls automatically and caches the run once it completes, minimising API calls
- **Assert on workflow execution** at any level of the action tree — top-level actions, deeply nested actions inside scopes and conditions, specific loop iterations, all iterations of a loop, switch case branches, and nested loops
- **Validate data transformations** (XSLT, Liquid) by reading the action output and deserialising it into a strongly typed C# model for field-level assertions
- **Validate correlated multi-workflow chains** — trigger a receive workflow and then validate all correlated processor and sender instances using the same correlation identifier that was set during the receive run

The step definitions maintain an internal context (the current workflow and its run list) that is automatically set when a workflow is triggered or when a named workflow step executes. All subsequent assertion steps in the scenario reuse this cached context, resulting in minimal API calls and fast test execution.

For transformation testing specifically, the framework provides `BaseTransformationStepDefinition<TSource, TDestination>` — a generic base class that handles all the infrastructure (storage upload, service bus claim-check, run polling, output deserialisation) and requires the concrete test class only to implement a single method: how to deserialise the transformation output body into the destination type.

---

## How It Compares to Microsoft's Built-In Testing Options

Microsoft provides several tools for testing Logic Apps Standard. This framework takes a fundamentally different approach and addresses gaps that none of the Microsoft tools cover.

| Microsoft Offering | Scope | Limitation |
|---|---|---|
| **Built-in Automated Test Framework** | Visual Studio Code extension | Requires manual test case creation in VS Code, no programmatic API, no CI/CD integration without custom scripting, limited assertion capability |
| **Automated Test SDK** | .NET SDK for unit-style tests | Runs workflows locally in isolation, not against a live deployed environment, no real connector execution |
| **Unit Test Generation (VS Code)** | Code generation tool | Generates unit tests for local execution only, still requires manual assertion authoring, no integration-level testing |
| **Mocking / Static Results Testing** | Local mock execution | Replaces connector calls with static values locally, not a substitute for real deployed integration testing |
| **Data Mapper Test Executor** | XSLT/map validation | Validates data mapping logic in isolation only, no workflow context |
| **Local Debugging + Test Execution (VS Code)** | Developer-time tooling | Manual, interactive, not automatable in a pipeline |

**Where this framework is different:**

1. **It runs against the real deployed environment.** Not local emulation, not mocked connectors. The workflow runs in Azure, against the real infrastructure — Service Bus, Storage Account, API Management, and downstream systems. You are testing what is actually deployed.

2. **It is designed for your DevOps pipeline.** Every scenario is a NUnit test that produces a standard pass/fail result. Drop the test project into your Azure DevOps or GitHub Actions pipeline after the deployment step. If the tests pass, the deployment is verified. If they fail, the pipeline stops.

3. **It provides a proper object model.** The Microsoft SDK does not give you a navigable object graph of a live workflow run. This framework does. Finding an action at any nesting depth, reading its output, checking its status — these are single method calls on strongly typed objects.

4. **It supports real end-to-end chain validation.** Testing a receive-process-send chain across three workflows, where the processor has five correlated instances each with nested loops and condition branches, is expressed in a single readable Gherkin scenario. No Microsoft tool handles this.

5. **It enables transformation testing against live data.** You can trigger a real transformation workflow with a real message, wait for it to complete, deserialise the XSLT or Liquid output into a typed C# model, and assert on individual fields — in a single test scenario. This is not possible with any Microsoft-provided tool.

6. **It supports mocking for isolated integration testing.** By leveraging the `IsMockingEnabled` feature described below, you can put source and target systems into a mocked state during test runs. This means you can test the full workflow behaviour — including send actions to SFTP servers, calls to APIs through API Management — without polluting production or staging systems. The Microsoft tooling does not provide a mechanism for this in a deployed environment.

**Bottom line:** Microsoft's tools are useful during development, locally, in VS Code. This framework is what you use after deployment, in your pipeline, to verify that the integration works end to end in a real environment.

---

## Prerequisites and Configuration

### Azure Infrastructure

The following Azure resources are required:

- **Azure Logic Apps Standard** instance containing the workflows under test
- **Azure Service Bus** namespace, used to trigger workflows via the claim-check pattern and to validate send-side behaviour
- **Azure Storage Account**, used to store message payloads as part of the claim-check pattern

### Entra ID App Registration

Create an App Registration in Microsoft Entra ID with the following:

- A **client secret** (used for authentication)
- The application permission **`access_as_application`** (Application ID: `25c1e7fc-03c5-4fdd-b076-716ebcb74a84`) enabled

Assign the following **RBAC roles** to the App Registration on the respective resources:

| Resource | Role |
|---|---|
| Logic Apps Standard | **Website Contributor** |
| Storage Account | **Storage Blob Data Contributor** and **Storage Blob Data Reader** |
| Service Bus Namespace | **Azure Service Bus Data Sender** and **Azure Service Bus Data Receiver** |

### appsettings.json

The integration test project reads configuration from an `appsettings.json` file. The values shown use the Azure DevOps token replacement syntax (`#{...}#`) so that the file can be committed safely and filled in at pipeline execution time.

```json
{
  "ClientId": "#{testFramework_client_id}#",
  "ClientSecret": "#{testFramework_client_secret}#",
  "TenantId": "#{testFramework_tenant_id}#",
  "SubscriptionId": "#{testFramework_subscription_id}#",
  "ResourceGroup": "#{testFramework_resource_group_name}#",
  "LogicAppName": "#{testFramework_la_name}#",
  "LogicAppApiVersion": "#{testFramework_la_api_version}#",
  "ServiceBusNamespace": "#{testFramework_sb_namespace}#",
  "StorageAccount": "#{testFramework_sa_name}#",
  "CorrelationIdActionName": "#{testFramework_correlation_id_action_name}",
  "VariableActionName": "Initialize_variables",
  "CorrelationIdVariableName": "correlationId"
}
```

| Key | Purpose |
|---|---|
| `ClientId` | The Application (client) ID of the Entra ID App Registration. Used to authenticate against the Azure Management API, Service Bus, and Storage Account. |
| `ClientSecret` | The client secret of the App Registration. Used together with `ClientId` and `TenantId` to obtain an access token from Entra ID. |
| `TenantId` | The Entra ID tenant (directory) ID. Identifies which directory to authenticate against. |
| `SubscriptionId` | The Azure subscription ID. Used to construct the Azure Management API resource path for the Logic Apps instance. |
| `ResourceGroup` | The name of the Azure Resource Group containing the Logic Apps Standard instance. |
| `LogicAppName` | The name of the Logic Apps Standard resource. Used to scope all workflow queries and trigger calls to the correct app. |
| `LogicAppApiVersion` | The Azure Management REST API version to use for Logic Apps calls (e.g. `2024-04-01`). |
| `ServiceBusNamespace` | The Service Bus namespace hostname prefix (e.g. `my-servicebus`). The framework builds the full hostname as `{namespace}.servicebus.windows.net`. |
| `StorageAccount` | The Storage Account name (e.g. `mystorageaccount`). The framework builds the full endpoint as `{name}.blob.core.windows.net`. |
| `CorrelationIdActionName` | The name of the Logic Apps action that sets the correlation ID variable in the receive workflow. Used by the framework to capture the correlation ID from the run for use in correlated multi-workflow assertions. |
| `VariableActionName` | The name of the action that initialises variables (default: `Initialize_variables`). Used to locate the correlation ID variable within the run. |
| `CorrelationIdVariableName` | The name of the variable within the initialise-variables action that holds the correlation ID (default: `correlationId`). |

---

## IsMockingEnabled — Putting an Environment Under Test

When running integration tests against a deployed Development or Test environment, you often do not want your workflows to call real downstream systems. Sending messages to an SFTP server, calling a production CRM API, or writing to a live database pollutes target systems with test data and creates unwanted side effects.

The `IsMockingEnabled` flag is an application setting pattern that allows you to put the entire environment into a **test mode** without modifying any workflow definitions.

### How It Works

A Logic Apps Standard workflow parameter `IsMockingEnabled` (boolean) is read at runtime and used as a routing decision inside the workflow. Examples of how this is used in practice:

- **API Management**: the `IsMockingEnabled` value is passed as a request header. An API Management policy reads this header and returns a mocked response when it is `true`, bypassing the real backend entirely.
- **Send workflows**: a condition on `IsMockingEnabled` skips the actual SFTP write, HTTP call, or queue send and instead routes to a no-op branch.
- **Receive workflows**: a mocked trigger payload can be substituted when the flag is set.

This means your integration tests can exercise the full workflow logic — transformation, routing, error handling, tracked properties — without any data reaching source or target systems. The test environment is fully isolated from an external perspective, but the workflow runs exactly as it would in production from an internal perspective.

### Configuration

> This section is reserved for environment-specific setup examples, including Azure DevOps pipeline variable group configuration, pipeline YAML task samples, and per-environment appsettings overrides.
>
> *(Examples will be added here.)*

---

## Using the Management Framework Directly in .NET

The Management library can be used independently of the Gherkin layer, for example to write custom NUnit integration tests, diagnostic tools, or operational scripts.

### Setup

```csharp
using LogicApps.Management;
using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Repository;
using Microsoft.Extensions.DependencyInjection;

var configuration = AppSettings.Configuration;
var baseAddress = new Uri("https://management.azure.com");

var services = new ServiceCollection();
services.AddHttpClient("AzureManagementClient", client =>
{
    client.BaseAddress = baseAddress;
    client.DefaultRequestHeaders.Add("accept", "application/json");
    client.Timeout = TimeSpan.FromMinutes(5);
});
services.AddHttpClient("AzurePublicHttpClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
services.AddHttpClient("EntraTokenClient");

var serviceProvider = services.BuildServiceProvider();
var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

var tokenClient = new EntraTokenClient(httpClientFactory);
var azureHttpClient = new AzureHttpClient(
    httpClientFactory,
    tokenClient,
    baseAddress,
    configuration["TenantId"]!,
    configuration["ClientId"]!,
    configuration["ClientSecret"]!);

var repository = new AzureManagementRepository(azureHttpClient, baseAddress);
var actionHelper = new ActionHelper(repository);
var actionFactory = new ActionFactory(configuration, repository, actionHelper);

var logicApp = await LogicApp.CreateAsync(
    configuration,
    repository,
    actionFactory,
    actionHelper,
    loadRunsSince: DateTime.UtcNow.AddHours(-24));
```

### Retrieving Workflows and Runs

```csharp
// Get all workflows in the Logic App
var workflows = await logicApp.GetWorkflowsAsync();

// Find a specific workflow by name
var workflow = workflows.FirstOrDefault(w => w.Name == "prc");

// Get all runs for the workflow (cached after first call)
var runs = await workflow!.GetWorkflowRunsAsync();

// Filter to succeeded runs only
var succeededRuns = runs.Where(r => r.Status == "Succeeded").ToList();

// Get the most recent run
var latestRun = runs.MaxBy(r => r.StartTime);

// Get runs by correlation ID
var correlatedRun = runs.FirstOrDefault(r => r.CorrelationId == myCorrelationId);

// Clear the cache and reload from Azure
await workflow.ReloadAsync();
```

### Triggering a Workflow

```csharp
var trigger = await workflow!.GetTriggerAsync();

// Trigger without a body (e.g. recurrence workflows)
await trigger.Run(null);

// Trigger with a JSON body (e.g. HTTP-triggered workflows)
using var content = new StringContent("{\"key\":\"value\"}", new MediaTypeHeaderValue("application/json"));
await trigger.Run(content);

// Trigger with additional request headers
var headers = new Dictionary<string, string> { { "x-custom-header", "value" } };
await trigger.Run(content, headers);
```

### Navigating the Action Tree

Actions at every nesting depth are returned by `GetWorkflowRunActionsAsync` as typed objects. The returned list contains only top-level actions; nested actions are reachable through the properties of their parent.

```csharp
var run = runs.First();

// Get top-level actions for this run
var actions = await run.GetWorkflowRunActionsAsync();

foreach (var action in actions)
{
    Console.WriteLine($"{action.DesignerName}: {action.Status}");
    Console.WriteLine($"  Start: {action.StartTime}, End: {action.EndTime}");

    // Read the raw action output (JToken)
    if (action.Output != null)
        Console.WriteLine($"  Output: {action.Output}");

    // Read error information when the action failed
    if (action.Error != null)
        Console.WriteLine($"  Error: {action.Error.Code} - {action.Error.Message}");
}
```

### Finding an Action Anywhere in the Tree

`FindActionByNameAsync` performs a depth-first search across the entire action tree, including actions inside scopes, conditions, loop iterations, and switch cases.

```csharp
// Returns all actions matching the name, at any nesting depth
var matchingActions = await run.FindActionByNameAsync("Transform source data type to target data type");
var transformAction = matchingActions?.FirstOrDefault();

Console.WriteLine($"Status: {transformAction?.Status}");
Console.WriteLine($"Output: {transformAction?.Output}");
```

### Reading Actions Inside a Scope

A `ScopeAction` (Try, Catch, Finally, or any named scope) exposes its children through the `Actions` property, which is populated when the parent run's actions are loaded.

```csharp
var scopeAction = actions.OfType<ScopeAction>().FirstOrDefault(a => a.DesignerName == "Try");

foreach (var child in scopeAction!.Actions)
{
    Console.WriteLine($"  {child.DesignerName}: {child.Status}");
}
```

### Reading Condition Branches

A `ConditionAction` exposes its true branch via `DefaultActions` and its false branch via `ElseActions`.

```csharp
var conditionAction = actions.OfType<ConditionAction>().FirstOrDefault(a => a.DesignerName == "Check order value");

// True branch (condition evaluated to true)
foreach (var child in conditionAction!.DefaultActions)
    Console.WriteLine($"  True branch: {child.DesignerName}: {child.Status}");

// False branch (condition evaluated to false)
foreach (var child in conditionAction.ElseActions)
    Console.WriteLine($"  False branch: {child.DesignerName}: {child.Status}");
```

### Reading Switch Cases

A `SwitchAction` exposes its cases through the `Cases` property. Each `SwitchCase` has a `Name` and a list of `Actions`.

```csharp
var switchAction = actions.OfType<SwitchAction>().FirstOrDefault(a => a.DesignerName == "Route by tier");

foreach (var switchCase in switchAction!.Cases)
{
    Console.WriteLine($"Case: {switchCase.Name}");
    foreach (var child in switchCase.Actions)
        Console.WriteLine($"  {child.DesignerName}: {child.Status}");
}
```

### Reading ForEach Loop Iterations

A `ForEachAction` exposes its completed iterations through the `Repetitions` property. Each `ForEachActionRepetition` holds the actions executed in that iteration.

```csharp
var foreachAction = actions.OfType<ForEachAction>().FirstOrDefault(a => a.DesignerName == "For each item");

foreach (var repetition in foreachAction!.Repetitions)
{
    Console.WriteLine($"Iteration {repetition.Name}: {repetition.Status}");
    foreach (var child in repetition.Actions)
        Console.WriteLine($"  {child.DesignerName}: {child.Status}");
}
```

### Reading Until Loop Iterations

A `UntilAction` exposes its iterations through the `Repetitions` property. Each `UntilActionRepetition` holds the actions and iteration count for that cycle.

```csharp
var untilAction = actions.OfType<UntilAction>().FirstOrDefault(a => a.DesignerName == "Until");

foreach (var repetition in untilAction!.Repetitions)
{
    Console.WriteLine($"Iteration {repetition.Name}: {repetition.Status} (count: {repetition.IterationCount})");
    foreach (var child in repetition.Actions)
        Console.WriteLine($"  {child.DesignerName}: {child.Status}");
}
```

### Reading Workflow Run Trigger Information

```csharp
var runTrigger = await run.GetWorkflowRunTriggerAsync();

Console.WriteLine($"Trigger: {runTrigger?.DesignerName}");
Console.WriteLine($"Status:  {runTrigger?.Status}");
Console.WriteLine($"Start:   {runTrigger?.StartTime}");
Console.WriteLine($"Input:   {runTrigger?.Input}");
Console.WriteLine($"Output:  {runTrigger?.Output}");
```

---

## Writing Tests in Gherkin

The Gherkin layer is built on [Reqnroll](https://reqnroll.net/) and exposes a rich set of step definitions that cover all common Logic Apps Standard testing scenarios. Tests are written in `.feature` files and bound to step definitions through Reqnroll's standard discovery mechanism.

A full reference of all available steps, path syntax, and testing patterns is available in [TESTING_GUIDE.md](TESTING_GUIDE.md).

### Receive-Process-Send Chain Validation

The most common enterprise integration pattern is a receive-process-send chain across multiple workflows. The framework tracks correlation identifiers automatically and allows the entire chain to be validated in a single scenario.

```gherkin
Feature: Receive-Process-Send-Sample-StepDefinition

Scenario: Validate complete workflow chain
    When Workflow "rcv" is triggered
    Then The workflow executed these actions:
        | StepName                          | Status    |
        | Initialize variable sampleMessage | Succeeded |
        | Initialize variable correlationId | Succeeded |
    And In "Try":
        | StepName               | Status    |
        | Validate json schema   | Succeeded |
    And All iterations of "For each item in items" executed:
        | StepName                             | Status    |
        | Write message payload to storage     | Succeeded |
        | Send claim-check message to topic    | Succeeded |

    Then For all instances of "prc" with the same correlation:
        | StepName                                        | Status    |
        | Receive new claim-check message from queue      | Succeeded |
        | Initialize variable correlationId               | Succeeded |
    And In "Try":
        | StepName                                        | Status    |
        | Retrieve message payload from storage           | Succeeded |
        | Transform source data type to target data type  | Succeeded |
        | Send claim-check message to queue               | Succeeded |

    Then For all instances of "snd" with the same correlation:
        | StepName                                        | Status    |
        | Receive new claim-check message from queue      | Succeeded |
    And In "Try":
        | StepName                                        | Status    |
        | Retrieve message payload from storage           | Succeeded |
```

### Transformation Testing

Transformation tests upload a source payload, trigger the workflow via a claim-check message, and assert on the typed deserialized output.

```gherkin
Feature: Transformation-Test-Sample-StepDefinition

Scenario: Transform source message to destination message
    Given A message with a data from the source system
    And It has the following source data:
        | Field   | Value          |
        | Value   | Open Sesame    |
        | OnClick | OpenMagicGate()|
    When The message payload is put in Storage Account container "demoworkload"
    And The claim-check is put on Service Bus topic "sbt-sourcesystem-out" with properties:
        | Field       | Value               |
        | sender      | sendingWorkflowName |
        | messageType | demoMessageType     |
    Then The workflow "prc" executed these actions:
        | StepName                                       | Status    |
        | Transform source data type to target data type | Succeeded |
    And Workflow step "Transform source data type to target data type" has transformed the data
    And The transformed data has "Status" with value "Open Sesame"
    And The transformed data has "Action" with value "OpenMagicGate()"
```

### Complex Nested Loop and Condition Scenarios

```gherkin
Feature: Nested-Foreach-and-Until-Loops-Sample-StepDefinition

Scenario: Validate complete workflow with nested structures
    When Workflow "prc-nestedloops-and-do-until" is triggered

    Then The workflow executed these actions:
        | StepName             | Status    |
        | Recurrence           | Succeeded |
        | Initialize variables | Succeeded |
        | Set variable         | Succeeded |

    And In "Try":
        | StepName        | Status    |
        | For each number | Succeeded |
        | Until           | Succeeded |

    And In "Try.For each number[1]":
        | StepName                              | Status    |
        | For each letter                       | Succeeded |
        | Variable testString2 value less than 3| Succeeded |

    And In "Try.For each number[1].For each letter[2].For each character[3]":
        | StepName                 | Status    |
        | Set variable testString  | Succeeded |

    And In "Try.Until[1].Condition":
        | StepName                    | Status  |
        | Set variable untilCompleted | Skipped |
```

### Creating a New Test Class

To create a new integration test for a specific feature, create a Reqnroll feature file and a matching step definition class. For transformation tests, inherit from `BaseTransformationStepDefinition<TSource, TDestination>` and implement the deserialization method. For non-transformation tests, inherit directly from `BaseStepDefinition`.

```csharp
// Transformation test (XML output)
[Binding, Scope(Feature = "My-Transformation-Feature")]
public class MyTransformationStepDefinition : BaseTransformationStepDefinition<MySourceModel, MyDestinationModel>
{
    protected override MyDestinationModel DeserializeTransformedBody(string body)
    {
        using TextReader reader = new StringReader(body);
        return (MyDestinationModel)new XmlSerializer(typeof(MyDestinationModel)).Deserialize(reader)!;
    }
}

// Transformation test (JSON output)
[Binding, Scope(Feature = "My-Json-Transformation-Feature")]
public class MyJsonTransformationStepDefinition : BaseTransformationStepDefinition<MySourceModel, MyDestinationModel>
{
    protected override MyDestinationModel DeserializeTransformedBody(string body)
        => JsonConvert.DeserializeObject<MyDestinationModel>(body)!;
}

// Non-transformation integration test
[Binding, Scope(Feature = "My-Integration-Feature")]
public class MyIntegrationStepDefinition : BaseStepDefinition;
```

---

## Unit Test Coverage

The framework is fully covered by unit tests across two test projects.

**`LogicApps.Management.Repository.Tests`** covers the Service Bus message builder, HTTP client construction, authentication token handling, and blob storage request building.

**`LogicApps.TestFramework.Specifications.Tests`** covers:

- `WorkflowRunValidation` — all validation methods including loop iteration count, actions in all iterations, actions in a specific iteration, nested loop validation, scope and branch child action validation, and single action status validation
- `WorkflowRunNavigator` — depth-first action search, scope navigation, condition branch retrieval (true and false), switch case retrieval, ForEach and Until iteration access, and nested loop discovery
- `ActionPathNavigator` — the full path syntax parser including simple names, iteration indices, condition `.actions` and `.else` branches, switch cases, nested paths, case-insensitive matching, and invalid path handling
- `ClassHelper` — nested property setting with dot-notation paths, automatic instance creation for null intermediate objects, type conversion, nullable type handling, and list indexer support
- `StringHelper` and `FileNameResolver` — string null normalisation and filename placeholder resolution

The unit tests run entirely in memory against in-process mock data and do not require any Azure connectivity. They can be executed in a standard `dotnet test` step with no additional setup.
