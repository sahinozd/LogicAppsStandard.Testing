using LogicApps.Management.Models.RestApi;
using Newtonsoft.Json.Linq;

namespace LogicApps.Management.Helper;

public interface IActionHelper
{
    /// <summary>
    /// Gets the action content based on the action content property that contains the Uri to retrieve data from.
    /// </summary>
    /// <param name="workflowRunActionContent">Workflow run action content property</param>
    /// <returns>A JToken with the content of the action.</returns>
    Task<JToken?> GetActionData(WorkflowRunDetailsActionContent workflowRunActionContent);

    WorkflowRunDetailsActionContent GetWorkflowRunActionContent(WorkflowRunDetailsActionContent? workflowRunActionContent);
}