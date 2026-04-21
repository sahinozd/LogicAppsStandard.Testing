using LogicApps.Management.Helper;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Helper;

[TestFixture]
internal sealed class ActionHelperTests
{
    private IAzureManagementRepository _azureManagementRepository;

    [SetUp]
    public void SetUp()
    {
        // Initialize the mocked Azure Management repository
        _azureManagementRepository = Substitute.For<IAzureManagementRepository>();
    }

    [TearDown]
    public void TearDown()
    {
        _azureManagementRepository = null!;
    }

    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Arrange & Act
        var azureManagementRepositoryException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new ActionHelper(null!);
        });

        // Assert
        Assert.That(azureManagementRepositoryException?.ParamName, Is.EqualTo("azureManagementRepository"));
    }

    [Test]
    public async Task GetActionData_Should_Return_JToken_When_Valid_WorkflowRunActionContent_Is_Provided()
    {
        // Arrange
        var testUri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/Set_variable/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature");
        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = testUri,
        };

        JToken? expectedJToken = null;

        var filePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Action_Set-variable-input-content.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllTextAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
            expectedJToken = JToken.Parse(json);
        }

        // Configure the mock to return the expected JToken when GetObjectPublic is called
        _azureManagementRepository
            .GetObjectPublicAsync<JToken>(testUri)
            .Returns(expectedJToken);

        // Act
        var actionHelper = new ActionHelper(_azureManagementRepository);
        var result = await actionHelper.GetActionData(workflowRunActionContent).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedJToken));
            Assert.That(result!["body"]!["name"]!.ToString(), Is.EqualTo("correlationId"));
            Assert.That(result["body"]!["value"]!.ToString(), Is.EqualTo("This is a test."));
        }

        // Verify that GetObjectPublicAsync was called exactly once with the correct URI
        await _azureManagementRepository.Received(1).GetObjectPublicAsync<JToken>(testUri).ConfigureAwait(false);
    }

    [Test]
    public async Task GetActionData_Should_Return_Null_When_Repository_Returns_Null()
    {
        // Arrange
        var testUri = new Uri("https://host.azurewebsites.net:443/runtime/webhooks/workflow/scaleUnits/environment/workflows/workflow-id/runs/run-id/actions/Set_variable/contents/ActionOutputs?api-version=api-version&code=access-code&se=expiry-time&sp=permissions&sv=version&sig=signature");
        var workflowRunActionContent = new WorkflowRunDetailsActionContent
        {
            Uri = testUri,
        };

        JToken? expectedJToken = null;

        // Configure the mock to return the expected JToken when GetObjectPublicAsync is called
        _azureManagementRepository
            .GetObjectPublicAsync<JToken>(testUri)
            .Returns(expectedJToken);

        // Act
        var actionHelper = new ActionHelper(_azureManagementRepository);
        var result = await actionHelper.GetActionData(workflowRunActionContent).ConfigureAwait(false);

        // Assert
        Assert.That(result, Is.Null);

        // Verify that GetObjectPublicAsync was called exactly once with the correct URI
        await _azureManagementRepository.Received(1).GetObjectPublicAsync<JToken>(testUri).ConfigureAwait(false);
    }

    [Test]
    public void GetActionData_Should_Throw_ArgumentNullException_When_WorkflowRunActionContent_Is_Null()
    {
        // Arrange
        var actionHelper = new ActionHelper(_azureManagementRepository);

        // Act & Assert
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => actionHelper.GetActionData(null!));
        Assert.That(argumentNullException.ParamName, Is.EqualTo("workflowRunActionContent"));
    }

    [Test]
    public void GetWorkflowRunActionContent_Should_Return_New_Instance_With_Same_Values_When_Valid_Input_Is_Provided()
    {
        // Arrange
        var testUri = new Uri("https://management.azure.com/test/action/content");
        var originalContent = new WorkflowRunDetailsActionContent
        {
            Uri = testUri,
            ContentSize = 2048,
            Metadata = new WorkflowRunDetailsActionContentMetadata
            {
                ForeachItemsCount = 10
            }
        };

        var actionHelper = new ActionHelper(_azureManagementRepository);

        // Act
        var result = actionHelper.GetWorkflowRunActionContent(originalContent);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.SameAs(originalContent), "Result should be a new instance");
            Assert.That(result.Uri, Is.EqualTo(originalContent.Uri));
            Assert.That(result.ContentSize, Is.EqualTo(originalContent.ContentSize));
            Assert.That(result.Metadata, Is.Not.Null);
            Assert.That(result.Metadata, Is.Not.SameAs(originalContent.Metadata), "Metadata should be a new instance");
            Assert.That(result.Metadata!.ForeachItemsCount, Is.EqualTo(originalContent.Metadata!.ForeachItemsCount));
        }
    }

    [Test]
    public void GetWorkflowRunActionContent_Should_Return_New_Instance_With_Null_Values_When_Null_Input_Is_Provided()
    {
        // Arrange
        var actionHelper = new ActionHelper(_azureManagementRepository);

        // Act
        var result = actionHelper.GetWorkflowRunActionContent(null);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Uri, Is.Null);
            Assert.That(result.ContentSize, Is.Null);
            Assert.That(result.Metadata, Is.Not.Null);
            Assert.That(result.Metadata!.ForeachItemsCount, Is.Null);
        }
    }

    [Test]
    public void GetWorkflowRunActionContent_Should_Return_New_Instance_When_Metadata_Is_Null()
    {
        // Arrange
        var testUri = new Uri("https://management.azure.com/test/action/content");
        var originalContent = new WorkflowRunDetailsActionContent
        {
            Uri = testUri,
            ContentSize = 512,
            Metadata = null
        };

        var actionHelper = new ActionHelper(_azureManagementRepository);

        // Act
        var result = actionHelper.GetWorkflowRunActionContent(originalContent);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.SameAs(originalContent), "Result should be a new instance");
            Assert.That(result.Uri, Is.EqualTo(originalContent.Uri));
            Assert.That(result.ContentSize, Is.EqualTo(originalContent.ContentSize));
            Assert.That(result.Metadata, Is.Not.Null);
            Assert.That(result.Metadata!.ForeachItemsCount, Is.Null);
        }
    }

    [Test]
    public void GetWorkflowRunActionContent_Should_Return_New_Instance_When_Metadata_Is_Present()
    {
        // Arrange
        var testUri = new Uri("https://management.azure.com/test/action/content");
        var originalContent = new WorkflowRunDetailsActionContent
        {
            Uri = testUri,
            ContentSize = 1024,
            Metadata = new WorkflowRunDetailsActionContentMetadata
            {
                ForeachItemsCount = 5
            }
        };

        var actionHelper = new ActionHelper(_azureManagementRepository);

        // Act
        var result = actionHelper.GetWorkflowRunActionContent(originalContent);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.SameAs(originalContent), "Result should be a new instance");
            Assert.That(result.Uri, Is.EqualTo(originalContent.Uri));
            Assert.That(result.ContentSize, Is.EqualTo(originalContent.ContentSize));
            Assert.That(result.Metadata, Is.Not.Null);
            Assert.That(result.Metadata!.ForeachItemsCount, Is.EqualTo(originalContent.Metadata.ForeachItemsCount));
        }
    }
}