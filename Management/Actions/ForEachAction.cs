using LogicApps.Management.Helper;
using LogicApps.Management.Models.Constants;
using LogicApps.Management.Models.Enums;
using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

namespace LogicApps.Management.Actions;

/// <summary>
/// Represents a ForEach action in a workflow. This action contains repetitions (historical/iteration instances)
/// and provides methods to load repetition information from the management API.
/// </summary>
public class ForEachAction(string name, ActionType actionType) : BaseAction(name, actionType)
{
    /// <summary>
    /// Historical repetitions for this ForEach action. Populated by calling <see cref="GetAllActionRepetitions"/>.
    /// </summary>
    public List<ForEachActionRepetition> Repetitions { get; set; } = [];

    /// <summary>
    /// Retrieve all repetitions for this ForEach action from the management API. Results are paged and the method will follow next links until all records are fetched.
    /// </summary>
    /// <param name="configuration">Application configuration containing subscription/resource identifiers.</param>
    /// <param name="azureManagementRepository">Repository used to call the management API.</param>
    /// <param name="actionHelper">Helper used to translate API models when necessary.</param>
    /// <param name="workflowName">The workflow name.</param>
    /// <param name="runId">The workflow run identifier.</param>
    /// <returns>List of <see cref="ForEachActionRepetition"/> instances representing each repetition.</returns>
    public new async Task<List<ForEachActionRepetition>> GetAllActionRepetitions(IConfiguration configuration, IAzureManagementRepository azureManagementRepository, IActionHelper actionHelper, string workflowName, string runId)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(azureManagementRepository);
        ArgumentNullException.ThrowIfNull(actionHelper);
        ArgumentException.ThrowIfNullOrEmpty(workflowName);
        ArgumentException.ThrowIfNullOrEmpty(runId);

        var repetitions = new List<ForEachActionRepetition>();
        var uriString = $"/subscriptions/{configuration[StringConstants.SubscriptionId]!}/resourceGroups/{configuration[StringConstants.ResourceGroup]!}/providers/Microsoft.Web/sites/{configuration[StringConstants.LogicAppName]!}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{workflowName}/runs/{runId}/actions/{Name}/scopeRepetitions?api-version={configuration[StringConstants.LogicAppApiVersion]!}";

        // Get action operation returns 250 records by default. When there are more actions, it will give a nextLink, which we use to get the next page of actions. Repeat till no nextLink is left.
        while (!string.IsNullOrEmpty(uriString))
        {
            var relativeUri = new Uri(uriString, UriKind.Relative);
            var result = await azureManagementRepository.GetObjectAsync<Response<WorkflowRunDetailsActionRepetition>>(relativeUri).ConfigureAwait(false);

            if (result!.Value != null)
            {
                repetitions.AddRange(result.Value.Select(resultRepetition => new ForEachActionRepetition(resultRepetition)));
            }

            uriString = !string.IsNullOrEmpty(result.NextLink) ? new Uri(result.NextLink).PathAndQuery : string.Empty;
        }
       
        return repetitions;
    }
}