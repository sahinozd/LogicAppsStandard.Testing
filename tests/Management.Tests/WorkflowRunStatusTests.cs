using LogicApps.Management.Models.Constants;
using NUnit.Framework;

namespace LogicApps.Management.Tests;

[TestFixture]
internal sealed class WorkflowRunStatusTests
{
    [TestCase(WorkflowRunStatus.Succeeded, "Succeeded")]
    [TestCase(WorkflowRunStatus.Failed, "Failed")]
    [TestCase(WorkflowRunStatus.Running, "Running")]
    [TestCase(WorkflowRunStatus.Cancelled, "Cancelled")]
    [TestCase(WorkflowRunStatus.Waiting, "Waiting")]
    [TestCase(WorkflowRunStatus.Skipped, "Skipped")]
    [TestCase(WorkflowRunStatus.TimedOut, "Timed out")]
    [TestCase(WorkflowRunStatus.Aborted, "Aborted")]
    public void WorkflowRunStatus_Constants_Should_Have_Expected_String_Values(string constant, string expected)
    {
        Assert.That(constant, Is.EqualTo(expected));
    }

    [Test]
    public void WorkflowRunStatus_Constants_Should_Be_Distinct()
    {
        var allStatuses = new[]
        {
            WorkflowRunStatus.Succeeded,
            WorkflowRunStatus.Failed,
            WorkflowRunStatus.Running,
            WorkflowRunStatus.Cancelled,
            WorkflowRunStatus.Waiting,
            WorkflowRunStatus.Skipped,
            WorkflowRunStatus.TimedOut,
            WorkflowRunStatus.Aborted
        };

        Assert.That(allStatuses, Is.Unique);
    }
}
