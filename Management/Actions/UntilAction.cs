using System.Globalization;
using LogicApps.Management.Helper;
using LogicApps.Management.Models.Constants;
using LogicApps.Management.Models.Enums;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;

namespace LogicApps.Management.Actions;

/// <summary>
/// Represents an Until loop action. Provides logic to compute repetitions based on iteration counts or to retrieve them
/// from the management API when appropriate.
/// </summary>
public class UntilAction(string name, ActionType actionType) : BaseAction(name, actionType)
{
    public List<UntilActionRepetition> Repetitions { get; set; } = [];

    /// <summary>
    /// Retrieve all repetitions for this Until action. When a repetitionIndex is not provided the method generates
    /// repetitions based on the action's IterationCount; otherwise the API can be queried for repetitions.
    /// </summary>
    /// <returns>List of <see cref="UntilActionRepetition"/> representing each repetition.</returns>
    public async Task<List<UntilActionRepetition>> GetAllActionRepetitions(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionHelper actionHelper, string workflowName, string runId, string? repetitionIndex)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(azureManagementRepository);
        ArgumentNullException.ThrowIfNull(actionHelper);
        ArgumentException.ThrowIfNullOrEmpty(workflowName);
        ArgumentException.ThrowIfNullOrEmpty(runId);

        var repetitions = new List<UntilActionRepetition>();

        WorkflowRunDetailsActionRepetition? baseRepetition = null;

        if (!string.IsNullOrEmpty(repetitionIndex))
        {
            var results = await GetRepetitionsFromApi(configuration, azureManagementRepository, workflowName, runId).ConfigureAwait(false);
            baseRepetition = results.FirstOrDefault(x => x.Name == repetitionIndex);
            
            if (baseRepetition == null)
            {
                return repetitions;
            }
        }

        for (var i = 0; i < IterationCount; i++)
        {
            var formattedIndex = i.ToString("D6", CultureInfo.InvariantCulture);
            var repetitionName = string.IsNullOrEmpty(repetitionIndex) ? formattedIndex : $"{repetitionIndex}-{formattedIndex}";
            var repetitionId = baseRepetition?.Id ?? $"/workflows/{workflowName}/runs/{runId}/actions/{Name}/repetitions/{repetitionName}";

            var workflowRepetition = new WorkflowRunDetailsActionRepetition
            {
                Name = repetitionName,
                Id = repetitionId,
                Type = baseRepetition?.Type ?? "workflows/run/actions/repetitions",
                Properties = new WorkflowRunDetailsActionRepetitionProperties
                {
                    RepetitionIndexes = baseRepetition?.Properties?.RepetitionIndexes ?? [],
                    TrackingId = baseRepetition?.Properties?.TrackingId ?? TrackingId,
                    IterationCount = baseRepetition?.Properties?.IterationCount ?? IterationCount,
                    CanResubmit = baseRepetition?.Properties?.CanResubmit ?? CanResubmit,
                    StartTime = baseRepetition?.Properties?.StartTime,
                    EndTime = baseRepetition?.Properties?.EndTime,
                    Correlation = baseRepetition?.Properties?.Correlation ?? Correlation,
                    Status = baseRepetition?.Properties?.Status,
                    Code = baseRepetition?.Properties?.Code ?? Code
                }
            };
            workflowRepetition.Properties.RepetitionIndexes.Add(new RepetitionIndex { ScopeName = Name, ItemIndex = i });


            repetitions.Add(new UntilActionRepetition(workflowRepetition));
        }

        return repetitions;
    }

    private async Task<List<WorkflowRunDetailsActionRepetition>> GetRepetitionsFromApi(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, string workflowName, string runId)
    {
        var uriString = $"/subscriptions/{configuration[StringConstants.SubscriptionId]!}/resourceGroups/{configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{configuration[StringConstants.LogicAppName]!}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{workflowName}/runs/{runId}/actions/{Name}/repetitions?api-version={configuration[StringConstants.LogicAppApiVersion]!}";

        var results = new List<WorkflowRunDetailsActionRepetition>();

        while (!string.IsNullOrEmpty(uriString))
        {
            var result = await azureManagementRepository
                .GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(new Uri(uriString, UriKind.Relative))
                .ConfigureAwait(false);

            if (result?.Value != null)
            {
                results.AddRange(result.Value);
            }

            uriString = !string.IsNullOrEmpty(result?.NextLink) ? new Uri(result.NextLink).PathAndQuery : string.Empty;
        }

        return results;
    }
}