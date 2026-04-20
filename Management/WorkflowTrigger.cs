using LogicApps.Management.Models.Constants;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace LogicApps.Management;

/// <summary>
/// Represents trigger configuration and metadata for a Logic App workflow. Provides methods to construct trigger URLs and execute the trigger.
/// </summary>
public sealed class WorkflowTrigger
{
    private readonly IConfiguration _configuration;
    private readonly IAzureManagementRepository _azureManagementRepository;
    private readonly string _workflowName;
    private const string RecurrenceTriggerName = "Recurrence";

    public DateTime? ChangedTime { get; private set; }

    public DateTime? CreatedTime { get; private set; }

    public string? DesignerName { get; set; }

    public string? Id { get; set; }

    public string? LastExecutionTime { get; private set; }

    public string? Name { get; set; }

    public string? NextExecutionTime { get; private set; }

    public string? ProvisioningState { get; private set; }

    public WorkflowTriggerRecurrence? Recurrence { get; private set; }

    public string? State { get; private set; }

    public Uri? TriggerUrl { get; private set; }

    public string? Type { get; set; }

    private WorkflowTrigger(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, string workflowName)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _azureManagementRepository = azureManagementRepository ?? throw new ArgumentNullException(nameof(azureManagementRepository));
        _workflowName = workflowName ?? throw new ArgumentNullException(nameof(workflowName));
    }

    /// <summary>
    /// Create and initialize a <see cref="WorkflowTrigger"/> for the specified workflow.
    /// </summary>
    /// <param name="configuration">Configuration settings.</param>
    /// <param name="azureManagementRepository">Azure management repository used to query trigger details.</param>
    /// <param name="workflowName">Workflow name.</param>
    /// <returns>Initialized <see cref="WorkflowTrigger"/> instance.</returns>
    public static Task<WorkflowTrigger> CreateAsync(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, string workflowName)
    {
        var ret = new WorkflowTrigger(configuration, azureManagementRepository, workflowName);
        return ret.InitializeAsync();
    }

    /// <summary>
    /// Execute the trigger endpoint for the workflow. For recurrence triggers a POST without content is performed; other triggers may require
    /// a GET or a POST with content depending on the trigger type.
    /// </summary>
    /// <param name="content">Optional request content to post to the trigger endpoint.</param>
    /// <param name="requestHeaders">Optional headers for the request.</param>
    /// <returns>Execution response wrapped in a <see cref="WorkflowTriggerExecutionResponse"/>.</returns>
    public async Task<WorkflowTriggerExecutionResponse> Run(HttpContent? content, Dictionary<string, string>? requestHeaders = null)
    {
        HttpResponseMessage? response;

        if (Name == RecurrenceTriggerName)
        {
            response = (await _azureManagementRepository.PostAsync(TriggerUrl, null).ConfigureAwait(false)).EnsureSuccessStatusCode();
        }
        else
        {
            // When content is empty, do a GET instead.
            if (content == null)
            {
                response = await _azureManagementRepository.GetPublicAsync(TriggerUrl!).ConfigureAwait(false);
            }
            else
            {
                response = await _azureManagementRepository.PostPublicAsync(TriggerUrl!, content, requestHeaders).ConfigureAwait(false);
            }
        }

        return new WorkflowTriggerExecutionResponse(response);
    }

    /// <summary>
    /// Determine the execution URL for this trigger. Attempts to call the listCallbackUrl API for non-recurrence triggers
    /// and falls back to a constructed URL when necessary.
    /// </summary>
    /// <returns>The trigger URL as a relative <see cref="Uri"/>, or null if unavailable.</returns>
    private async Task<Uri?> GetTriggerUrlAsync()
    {
        var baseUrl = $"/subscriptions/{_configuration[StringConstants.SubscriptionId]!}/resourceGroups/{_configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{_configuration[StringConstants.LogicAppName]!}/hostruntime/runtime/webhooks/workflow/api/management/workflows";

        Uri? triggerUri;
        if (Name == RecurrenceTriggerName)
        {
            // Recurrence trigger url cannot be retrieved via listCallbackUrl api. It's defined as follows:
            var triggerUrl = $"{baseUrl}/{_workflowName}/triggers/{Name}/run?api-version={_configuration[StringConstants.LogicAppApiVersion]!}";
            triggerUri = new Uri(triggerUrl, UriKind.Relative);
        }
        else
        {
            // In most other cases (e.g. currently used http trigger, the listCallbackUrl api needs to called to retrieve the url
            var listCallbackUrl = new Uri($"{baseUrl}/{_workflowName}/triggers/{Name}/listCallbackUrl?api-version={_configuration[StringConstants.LogicAppApiVersion]!}", UriKind.Relative);
            using var listCallbackUrlContent = new StringContent(string.Empty);
            var triggerUrl = string.Empty;

            try
            {
                var result = (await _azureManagementRepository.PostAsync(listCallbackUrl, listCallbackUrlContent).ConfigureAwait(false)).EnsureSuccessStatusCode();
                var resultContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                
                triggerUrl = JObject.Parse(resultContent)["value"]?.ToString().Replace(":443", "", StringComparison.InvariantCulture);
            }
            catch (HttpRequestException)
            {
                // If there is no URL, the default should be set to the one of the recurrence (e.g. with the service bus trigger)
                triggerUrl = $"{baseUrl}/{_workflowName}/triggers/{Name}/run?api-version={_configuration[StringConstants.LogicAppApiVersion]!}";
            }
            finally
            {
                triggerUri = new Uri(triggerUrl!);
            }
        }

        return triggerUri;
    }

    /// <summary>
    /// Initialize trigger properties and compute the trigger URL.
    /// </summary>
    /// <returns>The initialized <see cref="WorkflowTrigger"/> instance.</returns>
    private async Task<WorkflowTrigger> InitializeAsync()
    {
        await SetPropertiesAsync().ConfigureAwait(false);
        TriggerUrl = await GetTriggerUrlAsync().ConfigureAwait(false);

        return this;
    }

    /// <summary>
    /// Load trigger properties from the management API and update instance fields such as recurrence and scheduling.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    private async Task SetPropertiesAsync()
    {
        var relativeUri = BuildTriggersUri();
        var result = await _azureManagementRepository.GetObjectAsync<Models.RestApi.Response<Models.RestApi.Trigger>>(relativeUri).ConfigureAwait(false);
        var responseItem = result?.Value?.FirstOrDefault();

        if (responseItem is null)
        {
            return;
        }

        ApplyBasicProperties(responseItem);
        Recurrence = MapRecurrence(responseItem.Properties?.Recurrence);
    }

    /// <summary>
    /// Builds a relative URI for accessing the triggers of a specific Logic App workflow within the configured Azure subscription and resource group.
    /// </summary>
    /// <remarks>The returned URI is constructed using configuration values for subscription ID, resource group, Logic App name, workflow name,
    /// and API version. Ensure that all required configuration values are set before calling this method.</remarks>
    /// <returns>A relative <see cref="Uri"/> that identifies the triggers endpoint for the specified Logic App workflow.</returns>
    private Uri BuildTriggersUri()
    {
        return new Uri($"/subscriptions/{_configuration[StringConstants.SubscriptionId]!}/resourceGroups/{_configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{_configuration[StringConstants.LogicAppName]!}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{_workflowName}/triggers?api-version={_configuration[StringConstants.LogicAppApiVersion]!}", UriKind.Relative);
    }

    /// <summary>
    /// Applies basic property values from the specified trigger response to the current instance.
    /// </summary>
    /// <remarks>This method updates the current instance's properties such as name, ID, type, and various state and timing values based on the provided response.
    /// Existing values are overwritten.</remarks>
    /// <param name="responseItem">The trigger response object containing property values to apply. Cannot be null.</param>
    private void ApplyBasicProperties(Models.RestApi.Trigger responseItem)
    {
        DesignerName = responseItem.Name?.Replace("_", " ", StringComparison.OrdinalIgnoreCase);
        Id = responseItem.Id;
        Name = responseItem.Name;
        Type = responseItem.Type;

        var properties = responseItem.Properties;

        ChangedTime = properties?.ChangedTime;
        CreatedTime = properties?.CreatedTime;
        LastExecutionTime = properties?.LastExecutionTime;
        NextExecutionTime = properties?.NextExecutionTime;
        ProvisioningState = properties?.ProvisioningState;
        State = properties?.State;
    }

    /// <summary>
    /// Maps a REST API recurrence model to a workflow trigger recurrence model.
    /// </summary>
    /// <param name="recurrence">The recurrence model received from the REST API to be mapped. Can be null.</param>
    /// <returns>A mapped workflow trigger recurrence model if the input is not null; otherwise, null.</returns>
    private static WorkflowTriggerRecurrence? MapRecurrence(Models.RestApi.Recurrence? recurrence)
    {
        if (recurrence is null)
        {
            return null;
        }

        var mapped = new WorkflowTriggerRecurrence
        {
            Frequency = recurrence.Frequency,
            Interval = recurrence.Interval,
            TimeZone = recurrence.TimeZone
        };

        if (recurrence.Schedule is not null)
        {
            mapped.Schedule = new WorkflowTriggerSchedule
            {
                Hours = recurrence.Schedule.Hours
            };
        }

        return mapped;
    }

}