using System.Diagnostics.CodeAnalysis;
using LogicApps.Management.Repository.ServiceBus;
using LogicApps.Management.Repository.StorageAccount;
using LogicApps.TestFramework.Specifications.Helpers;
using LogicApps.TestFramework.Specifications.Models;
using Newtonsoft.Json;
using NUnit.Framework;
using Reqnroll;

namespace LogicApps.TestFramework.Specifications;

/// <summary>
/// Generic base class for transformation workflow test step definitions.
/// </summary>
/// <typeparam name="TSource">The type representing the source message that is sent into the workflow.</typeparam>
/// <typeparam name="TDestination">The type representing the transformed output produced by the workflow.</typeparam>
/// <remarks>
/// Inherit from this class and implement <see cref="DeserializeTransformedBody"/> to provide
/// a concrete step definition class scoped to a specific feature. All shared Gherkin steps
/// for building, uploading and sending messages as well as capturing the transformation result
/// are provided here.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "This file is tightly coupled with the Azure infrastructure and therefore cannot be tested in unit test.")]
public abstract class BaseTransformationStepDefinition<TSource, TDestination> : BaseStepDefinition
    where TSource : class, new()
    where TDestination : class
{
    protected TSource? Message { get; private set; }
    protected TDestination? TransformedBody { get; private set; }
    protected string? FileName { get; private set; }
    protected string? CorrelationId { get; private set; }

    #region Gherkin Steps - Given (Message Setup)

    [Given("A message with a data from the source system")]
    [Given("A file with a data from the source system")]
    public void GivenAMessage()
    {
        Message = new TSource();
    }

    [Given("It has the following source data:")]
    public void GivenSourceData(Table table)
    {
        Assert.That(Message, Is.Not.Null, "Message is null.");

        foreach (var row in (table ?? throw new ArgumentNullException(nameof(table))).Rows)
        {
            ClassHelper.SetProperty(row["Field"], Message, row["Value"]);
        }
    }

    [Given("It has content from a file named \"(.*)\"")]
    public async Task GivenFileContent(string filename)
    {
        Assert.That(Message, Is.Not.Null, "File message is null.");
        var filePath = $"TestData\\{filename}";

        var fileExists = File.Exists(filePath);
        Assert.That(fileExists, Is.True, "File doesn't exist.");
        if (fileExists)
        {
            var content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            Message = JsonConvert.DeserializeObject<TSource>(content);
        }
    }

    #endregion

    #region Gherkin Steps - When (Message Publishing)

    [When("The message payload is put in Storage Account container \"(.*)\" with file name \"(.*)\"")]
    public async Task PublishStorageAccountMessagePayload(string container, string filename)
    {
        CorrelationId = Guid.NewGuid().ToString();

        //FileName = $"{CorrelationId}-rcv-source-data-{DateTime.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture)}.json";
        FileName = filename;
        var json = JsonConvert.SerializeObject(Message, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        });

        var content = BlobRequestBuilder.Build(json, FileName);
        await BlobStorageSender.UploadAsync(container, FileName, content).ConfigureAwait(false);
    }

    [When("The claim-check is put on Service Bus topic \"(.*)\""),
     When("The claim-check is put on Service Bus queue \"(.*)\"'")]
    public async Task PublishServiceBusMessageClaimCheck(string topicOrQueue)
    {
        var (message, properties) = ServiceBusMessageBuilder
            .Create()
            .WithClaimCheck(FileName!)
            .WithCorrelationId(CorrelationId!)
            .Build();

        await ServiceBusMessageSender.SendAsync(topicOrQueue, message, properties).ConfigureAwait(false);
    }

    [When("The claim-check is put on Service Bus topic \"(.*)\" with properties:"),
     When("The claim-check is put on Service Bus queue \"(.*)\" with properties:")]
    public async Task PublishServiceBusMessageClaimCheckWithAdditionalProperties(string topicOrQueue, Table table)
    {
        var messageProperties = new Dictionary<string, string?>();

        foreach (var row in (table ?? throw new ArgumentNullException(nameof(table))).Rows)
        {
            messageProperties.Add(row["Field"], row["Value"]);
        }

        var builder = ServiceBusMessageBuilder
            .Create()
            .WithClaimCheck(FileName!)
            .WithCorrelationId(CorrelationId!);

        foreach (var (key, value) in messageProperties)
        {
            builder = builder.AddProperty(key, value ?? string.Empty);
        }

        var (message, properties) = builder.Build();

        await ServiceBusMessageSender.SendAsync(topicOrQueue, message, properties).ConfigureAwait(false);
    }

    #endregion

    #region Gherkin Steps - Then (Transformation Assertions)

    [Then("Workflow step \"(.*)\" has transformed the data")]
    public async Task ThenStepHasTransformedData(string stepName)
    {
        EnsureHasWorkflowRuns();

        foreach (var workflowRun in CurrentWorkflowRuns)
        {
            Assert.That(workflowRun, Is.Not.Null, "The run has not been validated yet.");

            var transformationActions = await workflowRun.FindActionByNameAsync(stepName).ConfigureAwait(false);
            var transformationAction = transformationActions?.FirstOrDefault();

            var outputMessage = JsonConvert.DeserializeObject<TransformationOutput<string?>>(transformationAction!.Output!.ToString());
            Assert.That(outputMessage, Is.Not.Null, "The transformation resulted in Null.");

            TransformedBody = DeserializeTransformedBody(outputMessage!.Body!);
            SetTransformedBody(TransformedBody);
        }
    }

    #endregion

    /// <summary>
    /// Deserializes the raw transformation output body string into a <typeparamref name="TDestination"/> instance.
    /// </summary>
    /// <param name="body">The raw body string from the transformation action output.</param>
    /// <returns>The deserialized destination object.</returns>
    /// <remarks>
    /// Implement this method to handle the serialization format used by the workflow transformation,
    /// for example XML for XSLT transformations or JSON for Liquid transformations.
    /// </remarks>
    protected abstract TDestination DeserializeTransformedBody(string body);
}