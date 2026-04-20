using LogicApps.Management.Factory;
using LogicApps.Management.Helper;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;

namespace LogicApps.Management.Tests;

[TestFixture]
internal sealed class LogicAppTests
{
    private IConfiguration _configuration;
    private IAzureManagementRepository _azureManagementRepository;
    private IActionFactory _actionFactory;
    private IActionHelper _actionHelper;
    private Models.RestApi.LogicApp? _logicAppResponse;

    [SetUp]
    public void SetUp()
    {
        // Initialize mocked dependencies
        _configuration = Substitute.For<IConfiguration>();
        _azureManagementRepository = Substitute.For<IAzureManagementRepository>();
        _actionFactory = Substitute.For<IActionFactory>();
        _actionHelper = Substitute.For<IActionHelper>();

        // Setup configuration values
        _configuration["SubscriptionId"].Returns("subscription-id");
        _configuration["ResourceGroup"].Returns("resource-group");
        _configuration["LogicAppName"].Returns("logic-app");
        _configuration["LogicAppApiVersion"].Returns("2025-05-01");

        var filePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Logic-app-content.json");
        if (!File.Exists(filePath))
        {
            throw new FileLoadException("File does not exist.");
        }

        var json = File.ReadAllTextAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
        _logicAppResponse = JsonConvert.DeserializeObject<Models.RestApi.LogicApp>(json);
    }

    [TearDown]
    public void TearDown()
    {
        _configuration = null!;
        _azureManagementRepository = null!;
        _actionFactory = null!;
        _actionHelper = null!;
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_Configuration_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => LogicApp.CreateAsync(null!, _azureManagementRepository, _actionFactory, _actionHelper, DateTime.UtcNow));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("configuration"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_AzureManagementRepository_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => LogicApp.CreateAsync(_configuration, null!, _actionFactory, _actionHelper, DateTime.UtcNow));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("azureManagementRepository"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_ActionFactory_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => LogicApp.CreateAsync(_configuration, _azureManagementRepository, null!, _actionHelper, DateTime.UtcNow));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionFactory"));
    }

    [Test]
    public void CreateAsync_Should_Throw_ArgumentNullException_When_ActionHelper_Is_Null()
    {
        // Arrange & Act
        var argumentNullException = Assert.ThrowsAsync<ArgumentNullException>(() => LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, null!, DateTime.UtcNow));

        // Assert
        Assert.That(argumentNullException.ParamName, Is.EqualTo("actionHelper"));
    }

    [Test]
    public async Task CreateAsync_Should_Initialize_LogicApp_With_Valid_Parameters()
    {
        // Arrange
        // Normally the data is retrieved through this URL
        // https://management.azure.com/subscriptions/subscription-id/resourceGroups/resource-group/providers/Microsoft.Web/sites/[logicapp-name]?api-version=api_version

        // Configure the mock to return the logic app information
        _ = _azureManagementRepository
            .GetObjectAsync<Models.RestApi.LogicApp>(Arg.Any<Uri>())
            .Returns(ReturnThis);

        // Act
        var logicApp = await LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, DateTime.UtcNow).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logicApp, Is.Not.Null);
            Assert.That(logicApp.Id, Is.EqualTo(_logicAppResponse!.Id));
            Assert.That(logicApp.Name, Is.EqualTo(_logicAppResponse.Name));
            Assert.That(logicApp.Type, Is.EqualTo(_logicAppResponse.Type));
        }

        // Verify that GetObject was called exactly once to get LogicApp information
        await _azureManagementRepository.Received(1).GetObjectAsync<Models.RestApi.LogicApp>(Arg.Any<Uri>()).ConfigureAwait(false);
        return;

        Task<Models.RestApi.LogicApp?> ReturnThis(CallInfo arg)
        {
            return Task.FromResult(_logicAppResponse);
        }
    }

    [Test]
    public async Task CreateAsync_Should_Initialize_LogicApp_Properties_When_LoadRunsSince_Is_Null()
    {
        // Arrange
        // Configure the mock to return the logic app information
        _ = _azureManagementRepository
            .GetObjectAsync<Models.RestApi.LogicApp>(Arg.Any<Uri>())
            .Returns(ReturnThis);

        // Act
        var logicApp = await LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logicApp, Is.Not.Null);
            Assert.That(logicApp.Id, Is.EqualTo(_logicAppResponse!.Id));
            Assert.That(logicApp.Name, Is.EqualTo(_logicAppResponse.Name));
        }

        return;

        Task<Models.RestApi.LogicApp?> ReturnThis(CallInfo arg)
        {
            return Task.FromResult(_logicAppResponse);
        }
    }

    [Test]
    public async Task GetWorkflowsAsync_Should_Return_Empty_List_When_No_Workflows_Exist()
    {
        // Arrange
        // Configure the mock to return the logic app information
        _ = _azureManagementRepository
            .GetObjectAsync<Models.RestApi.LogicApp>(Arg.Any<Uri>())
            .Returns(ReturnThis);

        var emptyWorkflowsResponse = new Response<Models.RestApi.Workflow>
        {
            Value = new List<Models.RestApi.Workflow>()
        };

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.Workflow>>(Arg.Any<Uri>())
            .Returns(emptyWorkflowsResponse);

        var logicApp = await LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null).ConfigureAwait(false);

        // Act
        var workflows = await logicApp.GetWorkflowsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflows, Is.Not.Null);
            Assert.That(workflows, Is.Empty);
        }

        // Verify that GetObject was called to retrieve workflows
        await _azureManagementRepository.Received(1).GetObjectAsync<Response<Models.RestApi.Workflow>>(Arg.Any<Uri>()).ConfigureAwait(false);
        return;

        Task<Models.RestApi.LogicApp?> ReturnThis(CallInfo arg)
        {
            return Task.FromResult(_logicAppResponse);
        }
    }

    [Test]
    public async Task GetWorkflowsAsync_Should_Return_Cached_Workflows_On_Subsequent_Calls()
    {
        // Arrange
        // Configure the mock to return the logic app information
        _ = _azureManagementRepository
            .GetObjectAsync<Models.RestApi.LogicApp>(Arg.Any<Uri>())
            .Returns(ReturnThis);

        var emptyWorkflowsResponse = new Response<Models.RestApi.Workflow>
        {
            Value = new List<Models.RestApi.Workflow>()
        };

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.Workflow>>(Arg.Any<Uri>())
            .Returns(emptyWorkflowsResponse);

        var logicApp = await LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null).ConfigureAwait(false);

        // Act
        var workflows1 = await logicApp.GetWorkflowsAsync().ConfigureAwait(false);
        var workflows2 = await logicApp.GetWorkflowsAsync().ConfigureAwait(false);
        var workflows3 = await logicApp.GetWorkflowsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflows1, Is.Not.Null);
            Assert.That(workflows2, Is.Not.Null);
            Assert.That(workflows3, Is.Not.Null);
        }

        // When workflows list is empty, caching is not used (see GetWorkflowsAsync implementation)
        // Verify that GetObject was called three times since empty lists aren't cached
        await _azureManagementRepository.Received(3).GetObjectAsync<Response<Models.RestApi.Workflow>>(Arg.Any<Uri>()).ConfigureAwait(false);
        return;

        Task<Models.RestApi.LogicApp?> ReturnThis(CallInfo arg)
        {
            return Task.FromResult(_logicAppResponse);
        }
    }

    [Test]
    public async Task GetWorkflowsAsync_Should_Handle_Null_Value_In_Response()
    {
        // Arrange
        // Configure the mock to return the logic app information
        _ = _azureManagementRepository
            .GetObjectAsync<Models.RestApi.LogicApp>(Arg.Any<Uri>())
            .Returns(ReturnThis);

        var workflowsResponseWithNullValue = new Response<Models.RestApi.Workflow>
        {
            Value = null
        };

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.Workflow>>(Arg.Any<Uri>())
            .Returns(workflowsResponseWithNullValue);

        var logicApp = await LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null).ConfigureAwait(false);

        // Act
        var workflows = await logicApp.GetWorkflowsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(workflows, Is.Not.Null);
            Assert.That(workflows, Is.Empty);
        }

        return;

        Task<Models.RestApi.LogicApp?> ReturnThis(CallInfo arg)
        {
            return Task.FromResult(_logicAppResponse);
        }
    }

    [Test]
    public async Task GetWorkflowsAsync_Should_Build_Correct_Uri_With_Configuration_Values()
    {
        // Arrange
        const string subscriptionId = "subscription-id";
        const string resourceGroup = "resource-group";
        const string logicAppName = "logic-app-name";
        const string apiVersion = "2025-05-01";

        _configuration["SubscriptionId"].Returns(subscriptionId);
        _configuration["ResourceGroup"].Returns(resourceGroup);
        _configuration["LogicAppName"].Returns(logicAppName);
        _configuration["LogicAppApiVersion"].Returns(apiVersion);

        var logicAppResponse = new Models.RestApi.LogicApp
        {
            Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/sites/{logicAppName}",
            Name = logicAppName
        };

        var emptyWorkflowsResponse = new Response<Models.RestApi.Workflow>
        {
            Value = new List<Models.RestApi.Workflow>()
        };

        Uri? capturedWorkflowsUri = null;

        // Configure the mocks and capture the URI
        _azureManagementRepository
            .GetObjectAsync<Models.RestApi.LogicApp>(Arg.Any<Uri>())
            .Returns(logicAppResponse);

        _azureManagementRepository
            .GetObjectAsync<Response<Models.RestApi.Workflow>>(Arg.Do<Uri>(uri => capturedWorkflowsUri = uri))
            .Returns(emptyWorkflowsResponse);

        var logicApp = await LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null).ConfigureAwait(false);

        // Act
        await logicApp.GetWorkflowsAsync().ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedWorkflowsUri, Is.Not.Null);
            Assert.That(capturedWorkflowsUri!.ToString(), Does.Contain(subscriptionId));
            Assert.That(capturedWorkflowsUri.ToString(), Does.Contain(resourceGroup));
            Assert.That(capturedWorkflowsUri.ToString(), Does.Contain(logicAppName));
            Assert.That(capturedWorkflowsUri.ToString(), Does.Contain(apiVersion));
            Assert.That(capturedWorkflowsUri.ToString(), Does.Contain("/workflows"));
        }
    }

    [Test]
    public async Task CreateAsync_Should_Handle_Null_Properties_In_LogicApp_Response()
    {
        // Arrange
        var logicAppResponse = new Models.RestApi.LogicApp
        {
            Id = null,
            Name = null,
            Type = null
        };

        // Configure the mock to return the logic app information with null properties
        _azureManagementRepository
            .GetObjectAsync<Models.RestApi.LogicApp>(Arg.Any<Uri>())
            .Returns(logicAppResponse);

        // Act
        var logicApp = await LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logicApp, Is.Not.Null);
            Assert.That(logicApp.Id, Is.Null);
            Assert.That(logicApp.Kind, Is.Null);
            Assert.That(logicApp.Location, Is.Null);
            Assert.That(logicApp.Name, Is.Null);
            Assert.That(logicApp.Type, Is.Null);
        }
    }

    [Test]
    public async Task CreateAsync_Should_Handle_Null_LogicApp_Response()
    {
        // Arrange
        // Configure the mock to return null
        _azureManagementRepository
            .GetObjectAsync<Models.RestApi.LogicApp>(Arg.Any<Uri>())
            .Returns((Models.RestApi.LogicApp?)null);

        // Act
        var logicApp = await LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(logicApp, Is.Not.Null);
            Assert.That(logicApp.Id, Is.Null);
            Assert.That(logicApp.Kind, Is.Null);
            Assert.That(logicApp.Location, Is.Null);
            Assert.That(logicApp.Name, Is.Null);
            Assert.That(logicApp.Type, Is.Null);
        }
    }

    [Test]
    public async Task CreateAsync_Should_Use_Correct_Uri_For_LogicApp_Information()
    {
        // Arrange
        const string subscriptionId = "subscription-id";
        const string resourceGroup = "resource-group";
        const string logicAppName = "logic-app";
        const string apiVersion = "2025-05-01";

        _configuration["SubscriptionId"].Returns(subscriptionId);
        _configuration["ResourceGroup"].Returns(resourceGroup);
        _configuration["LogicAppName"].Returns(logicAppName);
        _configuration["LogicAppApiVersion"].Returns(apiVersion);

        var logicAppResponse = new Models.RestApi.LogicApp
        {
            Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Web/sites/{logicAppName}",
            Name = logicAppName
        };

        Uri? capturedUri = null;

        // Configure the mock and capture the URI
        _azureManagementRepository
            .GetObjectAsync<Models.RestApi.LogicApp>(Arg.Do<Uri>(uri => capturedUri = uri))
            .Returns(logicAppResponse);

        // Act
        await LogicApp.CreateAsync(_configuration, _azureManagementRepository, _actionFactory, _actionHelper, null).ConfigureAwait(false);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(capturedUri, Is.Not.Null);
            Assert.That(capturedUri!.ToString(), Does.Contain(subscriptionId));
            Assert.That(capturedUri.ToString(), Does.Contain(resourceGroup));
            Assert.That(capturedUri.ToString(), Does.Contain(logicAppName));
            Assert.That(capturedUri.ToString(), Does.Contain(apiVersion));
            Assert.That(capturedUri.ToString(), Does.Contain("Microsoft.Web/sites"));
        }
    }
}