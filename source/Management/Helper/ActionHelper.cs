using LogicApps.Management.Models.RestApi;
using LogicApps.Management.Repository;
using Newtonsoft.Json.Linq;

namespace LogicApps.Management.Helper;

/// <summary>
/// Helper that encapsulates logic to translate API action content payloads and to fetch linked action content from the management API.
/// </summary>
public class ActionHelper(IAzureManagementRepository azureManagementRepository) : IActionHelper
{
    private readonly IAzureManagementRepository _azureManagementRepository = azureManagementRepository ?? throw new ArgumentNullException(nameof(azureManagementRepository));

    /// <summary>
    /// Gets the action content by calling the provided URI from the workflow run action content. This method performs a non-authorized GET
    /// and returns the JSON payload as a <see cref="JToken"/>.
    /// </summary>
    /// <param name="workflowRunActionContent">Workflow run action content descriptor containing the Uri.</param>
    /// <returns>A <see cref="JToken"/> with the content fetched from the Uri, or null.</returns>
    public async Task<JToken?> GetActionData(WorkflowRunDetailsActionContent workflowRunActionContent)
    {
        ArgumentNullException.ThrowIfNull(workflowRunActionContent);

        var result = await _azureManagementRepository.GetObjectPublicAsync<JToken>(workflowRunActionContent.Uri!).ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Normalize or copy a <see cref="WorkflowRunDetailsActionContent"/> instance. This intentionally returns a new instance suitable for
    /// passing around in the code without modifying the original DTO.
    /// </summary>
    /// <param name="workflowRunActionContent">Source action content instance.</param>
    /// <returns>A copied <see cref="WorkflowRunDetailsActionContent"/> instance.</returns>
    public WorkflowRunDetailsActionContent GetWorkflowRunActionContent(WorkflowRunDetailsActionContent? workflowRunActionContent)
    {
        return new WorkflowRunDetailsActionContent
        {
            ContentSize = workflowRunActionContent?.ContentSize,
            Metadata = new WorkflowRunDetailsActionContentMetadata
            {
                ForeachItemsCount = workflowRunActionContent?.Metadata?.ForeachItemsCount,
            },
            Uri = workflowRunActionContent?.Uri
        };
    }
}