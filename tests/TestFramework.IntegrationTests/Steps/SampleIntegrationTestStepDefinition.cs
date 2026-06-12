using LogicApps.TestFramework.Specifications;
using Reqnroll;

namespace LogicApps.TestFramework.IntegrationTests.Steps;

[Binding, Scope(Feature = "Receive-Process-Send-Sample-StepDefinition")]
[Scope(Feature = "Nested-Foreach-and-Until-Loops-Sample-StepDefinition")]
[Scope(Feature = "Receive-Other-Trigger-StepDefinition")]
public class SampleIntegrationTestStepDefinition : BaseStepDefinition;