using LogicApps.Management.Helper;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Tests;

[TestFixture]
internal sealed class WorkflowRunTriggerTests
{
    private IConfiguration _configuration;
    private IAzureManagementRepository _azureManagementRepository;
    private IActionHelper _actionHelper;
    private WorkflowRunDetails? _workflowRunDetails;

    [SetUp]
    public void SetUp()
    {
        // Initialize mocked dependencies
        _configuration = Substitute.For<IConfiguration>();
        _azureManagementRepository = Substitute.For<IAzureManagementRepository>();
        _actionHelper = Substitute.For<IActionHelper>();

        // Setup configuration values
        _configuration["SubscriptionId"].Returns("subscription-id");
        _configuration["ResourceGroup"].Returns("resource-group");
        _configuration["LogicAppName"].Returns("logic-app");

        // Load workflow run details from JSON file
        var filePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Workflow-run-content.json");
        if (!File.Exists(filePath))
        {
            throw new FileLoadException($"File does not exist: {filePath}");
        }

        var workflowRunDetailsJson = File.ReadAllText(filePath);
        _workflowRunDetails = JsonConvert.DeserializeObject<WorkflowRunDetails>(workflowRunDetailsJson);
    }

    [TearDown]
    public void TearDown()
    {
        _configuration = null!;
        _azureManagementRepository = null!;
        _actionHelper = null!;
        _workflowRunDetails = null;
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRunTrigger.CreateAsync(null!, _azureManagementRepository, _actionHelper, "workflow", "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("configuration"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRunTrigger.CreateAsync(_configuration, null!, _actionHelper, "workflow", "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("azureManagementRepository"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, null!, "workflow", "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_WorkflowName_Is_Null()
    {
        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, null!, "run123"));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("workflowName"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_RunId_Is_Null()
    {
        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() =>
            WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", null!));

        Assert.That(argumentNullException.ParamName, Is.EqualTo("runId"));
    }

    [Test]
    public async Task CreateAsync_Should_Initialize_WorkflowRunTrigger_With_Valid_Parameters()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(_workflowRunDetails));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger, Is.Not.Null);
            Assert.That(trigger.Name, Is.EqualTo("Recurrence"));
            Assert.That(trigger.DesignerName, Is.EqualTo("Recurrence"));
            Assert.That(trigger.Status, Is.EqualTo("Succeeded"));
        }

        // Verify API was called
        await _azureManagementRepository.Received(1).GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>()).ConfigureAwait(false);
    }

    [Test]
    public async Task CreateAsync_Should_Build_Correct_Uri_With_Configuration_Values()
    {
        // Arrange
        Uri? capturedUri = null;

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Do<Uri>(uri => capturedUri = uri))
            .Returns(Task.FromResult(_workflowRunDetails));

        // Act
        await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run-456").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedUri, Is.Not.Null);
            Assert.That(capturedUri!.ToString(), Does.Contain("subscription-id"));
            Assert.That(capturedUri.ToString(), Does.Contain("resource-group"));
            Assert.That(capturedUri.ToString(), Does.Contain("logic-app"));
            Assert.That(capturedUri.ToString(), Does.Contain("workflow"));
            Assert.That(capturedUri.ToString(), Does.Contain("run-456"));
            Assert.That(capturedUri.ToString(), Does.Contain("api-version=2025-05-01"));
        }
    }

    [Test]
    public async Task CreateAsync_Should_Use_2025_Api_Version()
    {
        // Arrange
        Uri? capturedUri = null;

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Do<Uri>(uri => capturedUri = uri))
            .Returns(Task.FromResult(_workflowRunDetails));

        // Act
        await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        Assert.That(capturedUri!.ToString(), Does.Contain("api-version=2025-05-01"));
    }

    [Test]
    public async Task CreateAsync_Should_Replace_Underscores_In_DesignerName()
    {
        // Arrange
        const string workflowRunDetailsJson = 
            """
              {
                  "properties": {
                      "trigger": {
                          "name": "When_a_HTTP_request_is_received",
                          "status": "Succeeded"
                      }
                  }
              }
            """;

        var workflowRunDetails = JsonConvert.DeserializeObject<WorkflowRunDetails>(workflowRunDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(workflowRunDetails));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        Assert.That(trigger.DesignerName, Is.EqualTo("When a HTTP request is received"));
    }

    [Test]
    public async Task CreateAsync_Should_Set_All_Trigger_Properties()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(_workflowRunDetails));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.Name, Is.EqualTo("Recurrence"));
            Assert.That(trigger.DesignerName, Is.EqualTo("Recurrence"));
            Assert.That(trigger.Status, Is.EqualTo("Succeeded"));
            Assert.That(trigger.StartTime, Is.Not.Null);
            Assert.That(trigger.EndTime, Is.Not.Null);
            Assert.That(trigger.OriginHistoryName, Is.EqualTo("123456"));
        }
    }

    [Test]
    public async Task CreateAsync_Should_Map_Correlation_When_Present()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(_workflowRunDetails));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.Correlation, Is.Not.Null);
            Assert.That(trigger.Correlation!.ClientTrackingId, Is.EqualTo("123456"));
        }
    }

    [Test]
    public async Task CreateAsync_Should_Handle_Null_Correlation()
    {
        // Arrange
        const string workflowRunDetailsJson = 
              """
              {
                  "properties": {
                      "trigger": {
                          "name": "Recurrence",
                          "status": "Succeeded",
                          "correlation": null
                      }
                  }
              }
              """;

        var workflowRunDetails = JsonConvert.DeserializeObject<WorkflowRunDetails>(workflowRunDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(workflowRunDetails));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        Assert.That(trigger.Correlation, Is.Null);
    }

    [Test]
    public async Task CreateAsync_Should_Load_InputsLink_When_Present()
    {
        // Arrange
        const string workflowRunDetailsJson = 
            """
            {
              "properties": {
                  "trigger": {
                      "name": "Recurrence",
                      "status": "Succeeded",
                      "inputsLink": {
                          "uri": "https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature",
                          "contentSize": 256,
                          "metadata": {
                              "foreachItemsCount": 5
                          }
                      }
                  }
              }
            }
            """;

        var workflowRunDetails = JsonConvert.DeserializeObject<WorkflowRunDetails>(workflowRunDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(workflowRunDetails));

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 256,
            Metadata = new WorkflowRunDetailsActionContentMetadata
            {
                ForeachItemsCount = 5
            }
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        var inputData = JToken.Parse(@"{""triggerInput"": ""value""}");

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(Task.FromResult<JToken?>(inputData));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.InputsLink, Is.Not.Null);
            Assert.That(trigger.InputsLink!.Uri, Is.EqualTo(new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature")));
            Assert.That(trigger.InputsLink.ContentSize, Is.EqualTo(256));
            Assert.That(trigger.InputsLink.Metadata, Is.Not.Null);
            Assert.That(trigger.InputsLink.Metadata!.ForeachItemsCount, Is.EqualTo(5));
            Assert.That(trigger.Input, Is.Not.Null);
            Assert.That(trigger.Input!["triggerInput"]?.ToString(), Is.EqualTo("value"));
        }
    }

    [Test]
    public async Task CreateAsync_Should_Load_OutputsLink_When_Present()
    {
        // Arrange
        const string workflowRunDetailsJson = 
            """
              {
                  "properties": {
                      "trigger": {
                          "name": "Recurrence",
                          "status": "Succeeded",
                          "outputsLink": {
                              "uri": "https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature",
                              "contentSize": 512,
                              "metadata": {
                                  "foreachItemsCount": 10
                              }
                          }
                      }
                  }
              }
              """;

        var workflowRunDetails = JsonConvert.DeserializeObject<WorkflowRunDetails>(workflowRunDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(workflowRunDetails));

        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 512,
            Metadata = new WorkflowRunDetailsActionContentMetadata
            {
                ForeachItemsCount = 10
            }
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(workflowRunActionContent);

        var outputData = JToken.Parse("""{"triggerOutput": "result"}""");

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(Task.FromResult<JToken?>(outputData));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.OutputsLink, Is.Not.Null);
            Assert.That(trigger.OutputsLink!.Uri, Is.EqualTo(new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature")));
            Assert.That(trigger.OutputsLink.ContentSize, Is.EqualTo(512));
            Assert.That(trigger.OutputsLink.Metadata, Is.Not.Null);
            Assert.That(trigger.OutputsLink.Metadata!.ForeachItemsCount, Is.EqualTo(10));
            Assert.That(trigger.Output, Is.Not.Null);
            Assert.That(trigger.Output!["triggerOutput"]?.ToString(), Is.EqualTo("result"));
        }
    }

    [Test]
    public async Task CreateAsync_Should_Handle_Null_InputsLink()
    {
        // Arrange
        const string workflowRunDetailsJson = 
            """
              {
                  "properties": {
                      "trigger": {
                          "name": "Recurrence",
                          "status": "Succeeded",
                          "inputsLink": null
                      }
                  }
              }
            """;

        var workflowRunDetails = JsonConvert.DeserializeObject<WorkflowRunDetails>(workflowRunDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(workflowRunDetails));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.InputsLink, Is.Null);
            Assert.That(trigger.Input, Is.Null);
        }
    }

    [Test]
    public async Task CreateAsync_Should_Handle_Null_OutputsLink()
    {
        // Arrange
        const string workflowRunDetailsJson = 
            """
              {
                  "properties": {
                      "trigger": {
                          "name": "Recurrence",
                          "status": "Succeeded",
                          "outputsLink": null
                      }
                  }
              }
            """;

        var workflowRunDetails = JsonConvert.DeserializeObject<WorkflowRunDetails>(workflowRunDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(workflowRunDetails));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.OutputsLink, Is.Null);
            Assert.That(trigger.Output, Is.Null);
        }
    }

    [Test]
    public async Task CreateAsync_Should_Handle_Null_WorkflowRunDetails_Response()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult<WorkflowRunDetails?>(null));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger, Is.Not.Null);
            Assert.That(trigger.Name, Is.Null);
            Assert.That(trigger.DesignerName, Is.Null);
            Assert.That(trigger.Status, Is.Null);
            Assert.That(trigger.Correlation, Is.Null);
            Assert.That(trigger.InputsLink, Is.Null);
            Assert.That(trigger.OutputsLink, Is.Null);
        }
    }

    [Test]
    public async Task CreateAsync_Should_Handle_Null_Trigger_In_Response()
    {
        // Arrange
        const string workflowRunDetailsJson =
            """
              {
                  "properties": {
                      "trigger": null
                  }
              }
            """;

        var workflowRunDetails = JsonConvert.DeserializeObject<WorkflowRunDetails>(workflowRunDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(workflowRunDetails));

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger, Is.Not.Null);
            Assert.That(trigger.Name, Is.Null);
            Assert.That(trigger.DesignerName, Is.Null);
            Assert.That(trigger.Status, Is.Null);
        }
    }

    [Test]
    public async Task CreateAsync_Should_Load_Both_InputsLink_And_OutputsLink()
    {
        // Arrange
        const string workflowRunDetailsJson = 
            """
              {
                  "properties": {
                      "trigger": {
                          "name": "Recurrence",
                          "status": "Succeeded",
                          "inputsLink": {
                              "uri": "https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature",
                              "contentSize": 100
                          },
                          "outputsLink": {
                              "uri": "https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature",
                              "contentSize": 200
                          }
                      }
                  }
              }
            """;

        var workflowRunDetails = JsonConvert.DeserializeObject<WorkflowRunDetails>(workflowRunDetailsJson);

        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(workflowRunDetails));

        var inputContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionInputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 100
        };

        var outputContent = new WorkflowRunDetailsActionContent
        {
            Uri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/action-name/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature"),
            ContentSize = 200
        };

        _actionHelper
            .GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(x =>
            {
                var input = x.Arg<WorkflowRunDetailsActionContent>();
                return input.Uri?.ToString().Contains("ActionInputs", StringComparison.Ordinal) == true ? inputContent : outputContent;
            });

        var inputData = JToken.Parse("""{"input": "data"}""");
        var outputData = JToken.Parse("""{"output": "result"}""");

        _actionHelper
            .GetActionData(Arg.Any<WorkflowRunDetailsActionContent>())
            .Returns(x =>
            {
                var input = x.Arg<WorkflowRunDetailsActionContent>();
                return Task.FromResult<JToken?>(input.Uri?.ToString().Contains("ActionInputs", StringComparison.Ordinal) == true ? inputData : outputData);
            });

        // Act
        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.InputsLink, Is.Not.Null);
            Assert.That(trigger.InputsLink!.ContentSize, Is.EqualTo(100));
            Assert.That(trigger.Input, Is.Not.Null);

            Assert.That(trigger.OutputsLink, Is.Not.Null);
            Assert.That(trigger.OutputsLink!.ContentSize, Is.EqualTo(200));
            Assert.That(trigger.Output, Is.Not.Null);
        }

        // Verify both helpers were called
        _actionHelper.Received(2).GetWorkflowRunActionContent(Arg.Any<WorkflowRunDetailsActionContent>());
        await _actionHelper.Received(2).GetActionData(Arg.Any<WorkflowRunDetailsActionContent>()).ConfigureAwait(false);
    }

    [Test]
    public async Task Properties_Should_Be_Settable()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(_workflowRunDetails));

        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Act
        trigger.Correlation = new Correlation { ClientTrackingId = "new-client-id", ActionTrackingId = "new-action-id" };
        trigger.EndTime = DateTime.UtcNow;
        trigger.Name = "NewTriggerName";
        trigger.OriginHistoryName = "new-origin";
        trigger.StartTime = DateTime.UtcNow;
        trigger.Status = "Failed";

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.Correlation, Is.Not.Null);
            Assert.That(trigger.Correlation!.ClientTrackingId, Is.EqualTo("new-client-id"));
            Assert.That(trigger.Correlation.ActionTrackingId, Is.EqualTo("new-action-id"));
            Assert.That(trigger.EndTime, Is.Not.Null);
            Assert.That(trigger.Name, Is.EqualTo("NewTriggerName"));
            Assert.That(trigger.OriginHistoryName, Is.EqualTo("new-origin"));
            Assert.That(trigger.StartTime, Is.Not.Null);
            Assert.That(trigger.Status, Is.EqualTo("Failed"));
        }
    }

    [Test]
    public async Task Properties_Should_Handle_Null_Values()
    {
        // Arrange
        _azureManagementRepository
            .GetObjectAsync<WorkflowRunDetails>(Arg.Any<Uri>())
            .Returns(Task.FromResult(_workflowRunDetails));

        var trigger = await WorkflowRunTrigger.CreateAsync(_configuration, _azureManagementRepository, _actionHelper, "workflow", "run123").ConfigureAwait(false);

        // Act
        trigger.Correlation = null;
        trigger.EndTime = null;
        trigger.Name = null;
        trigger.OriginHistoryName = null;
        trigger.StartTime = null;
        trigger.Status = null;

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(trigger.Correlation, Is.Null);
            Assert.That(trigger.EndTime, Is.Null);
            Assert.That(trigger.Name, Is.Null);
            Assert.That(trigger.OriginHistoryName, Is.Null);
            Assert.That(trigger.StartTime, Is.Null);
            Assert.That(trigger.Status, Is.Null);
        }
    }
}