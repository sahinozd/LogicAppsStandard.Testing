using LogicApps.Management.Actions;
using LogicApps.Management.Models.RestApi;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Actions;

[TestFixture]
internal sealed class UntilActionRepetitionTests
{
    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_Repetition_Is_Null()
    {
        // Arrange & Act
        var repetitionException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new UntilActionRepetition(null!);
        });

        // Assert
        Assert.That(repetitionException?.ParamName, Is.EqualTo("repetition"));
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_When_Valid_Repetition_Is_Provided()
    {
        // Arrange
        // Normally the data is retrieved through this URL
        // https://management.azure.com/subscriptions/subscription-id/resourceGroups/resource-group/providers/Microsoft.Web/sites/[logicapp-name]/hostruntime/runtime/webhooks/workflow/api/management/workflows/[workflow-name]/runs/[run-id]/actions/[until_action]/repetitions/[repetition-index]?api-version=api_version
        WorkflowRunDetailsActionRepetition? repetition = null;

        var filePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Until-action-repetition.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllTextAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
            repetition = Newtonsoft.Json.JsonConvert.DeserializeObject<WorkflowRunDetailsActionRepetition>(json);
        }

        // Act
        var untilActionRepetition = new UntilActionRepetition(repetition!);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(untilActionRepetition.Id, Is.EqualTo(repetition?.Id));
            Assert.That(untilActionRepetition.Name, Is.EqualTo(repetition?.Name));
            Assert.That(untilActionRepetition.Type, Is.EqualTo(repetition?.Type));
            Assert.That(untilActionRepetition.Code, Is.EqualTo(repetition?.Properties?.Code));
            Assert.That(untilActionRepetition.Status, Is.EqualTo(repetition?.Properties?.Status));
            Assert.That(untilActionRepetition.StartTime, Is.EqualTo(repetition?.Properties?.StartTime));
            Assert.That(untilActionRepetition.EndTime, Is.EqualTo(repetition?.Properties?.EndTime));
            Assert.That(untilActionRepetition.TrackingId, Is.EqualTo(repetition?.Properties?.TrackingId));
            Assert.That(untilActionRepetition.CanResubmit, Is.EqualTo(repetition?.Properties?.CanResubmit));
            Assert.That(untilActionRepetition.IterationCount, Is.EqualTo(repetition?.Properties?.IterationCount));
            Assert.That(untilActionRepetition.ActionTrackingId, Is.EqualTo(repetition?.Properties?.Correlation?.ActionTrackingId));
            Assert.That(untilActionRepetition.ClientTrackingId, Is.EqualTo(repetition?.Properties?.Correlation?.ClientTrackingId));
            Assert.That(untilActionRepetition.RepetitionIndexes, Is.Not.Null);
            Assert.That(untilActionRepetition.RepetitionIndexes, Has.Count.EqualTo(2));
            Assert.That(untilActionRepetition.RepetitionIndexes![0].ScopeName, Is.EqualTo("Until"));
            Assert.That(untilActionRepetition.RepetitionIndexes[0].ItemIndex, Is.Zero);
            Assert.That(untilActionRepetition.Actions, Is.Not.Null);
            Assert.That(untilActionRepetition.Actions, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_With_Multiple_RepetitionIndexes()
    {
        // Arrange
        WorkflowRunDetailsActionRepetition? repetition = null;

        var filePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Until-action-repetition.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllTextAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
            repetition = Newtonsoft.Json.JsonConvert.DeserializeObject<WorkflowRunDetailsActionRepetition>(json);
        }

        // Act
        var untilActionRepetition = new UntilActionRepetition(repetition!);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(untilActionRepetition.RepetitionIndexes, Is.Not.Null);
            Assert.That(untilActionRepetition.RepetitionIndexes, Has.Count.EqualTo(2));
            Assert.That(untilActionRepetition.RepetitionIndexes![0].ScopeName, Is.EqualTo("Until"));
            Assert.That(untilActionRepetition.RepetitionIndexes[0].ItemIndex, Is.Zero);
            Assert.That(untilActionRepetition.RepetitionIndexes[1].ScopeName, Is.EqualTo("Until2"));
            Assert.That(untilActionRepetition.RepetitionIndexes[1].ItemIndex, Is.EqualTo(1));
        }
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_When_Properties_Is_Null()
    {
        // Arrange
        var repetition = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/[workflow-name]/runs/[run-id]/actions/Until2/repetitions/000000",
            Name = "000000",
            Type = "workflows/runs/actions/repetitions",
            Properties = null
        };

        // Act
        var untilActionRepetition = new UntilActionRepetition(repetition);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(untilActionRepetition.Id, Is.EqualTo(repetition.Id));
            Assert.That(untilActionRepetition.Name, Is.EqualTo(repetition.Name));
            Assert.That(untilActionRepetition.Type, Is.EqualTo(repetition.Type));
            Assert.That(untilActionRepetition.Code, Is.Null);
            Assert.That(untilActionRepetition.Status, Is.Null);
            Assert.That(untilActionRepetition.StartTime, Is.Null);
            Assert.That(untilActionRepetition.EndTime, Is.Null);
            Assert.That(untilActionRepetition.TrackingId, Is.Null);
            Assert.That(untilActionRepetition.CanResubmit, Is.Null);
            Assert.That(untilActionRepetition.IterationCount, Is.Null);
            Assert.That(untilActionRepetition.ActionTrackingId, Is.Null);
            Assert.That(untilActionRepetition.ClientTrackingId, Is.Null);
            Assert.That(untilActionRepetition.RepetitionIndexes, Is.Not.Null);
            Assert.That(untilActionRepetition.RepetitionIndexes, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_When_Correlation_Is_Null()
    {
        // Arrange
        var repetition = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/[workflow-name]/runs/[run-id]/actions/Until2/repetitions/000000",
            Name = "000000",
            Type = "workflows/runs/actions/repetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                IterationCount = 2,
                Correlation = null,
                RepetitionIndexes = []
            }
        };

        // Act
        var untilActionRepetition = new UntilActionRepetition(repetition);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(untilActionRepetition.ActionTrackingId, Is.Null);
            Assert.That(untilActionRepetition.ClientTrackingId, Is.Null);
            Assert.That(untilActionRepetition.IterationCount, Is.EqualTo(2));
        }
    }

    [Test]
    public void Constructor_Should_Handle_Empty_RepetitionIndexes_List()
    {
        // Arrange
        var repetition = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/[workflow-name]/runs/[run-id]/actions/Until2/repetitions/000000",
            Name = "000000",
            Type = "workflows/runs/actions/repetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                RepetitionIndexes = []
            }
        };

        // Act
        var untilActionRepetition = new UntilActionRepetition(repetition);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(untilActionRepetition.RepetitionIndexes, Is.Not.Null);
            Assert.That(untilActionRepetition.RepetitionIndexes, Is.Empty);
        }
    }
}