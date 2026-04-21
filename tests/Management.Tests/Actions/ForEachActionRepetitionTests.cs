using LogicApps.Management.Actions;
using LogicApps.Management.Models.RestApi;
using NUnit.Framework;

namespace LogicApps.Management.Tests.Actions;

[TestFixture]
internal sealed class ForEachActionRepetitionTests
{
    [Test]
    public void Constructor_Should_Throw_ArgumentNullException_When_Repetition_Is_Null()
    {
        // Arrange & Act
        var repetitionException = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new ForEachActionRepetition(null!);
        });

        // Assert
        Assert.That(repetitionException?.ParamName, Is.EqualTo("repetition"));
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_When_Valid_Repetition_Is_Provided()
    {
        // Arrange

        // Normally the data is retrieved through this URL
        // https://management.azure.com/subscriptions/subscription-id/resourceGroups/resource-group/providers/Microsoft.Web/sites/[logicapp-name]/hostruntime/runtime/webhooks/workflow/api/management/workflows/[workflow-name]/runs/[run-id]/actions/[foreach_action]/scopeRepetitions/[repetition-index]?api-version=api_version
        WorkflowRunDetailsActionRepetition? repetition = null;

        var filePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Foreach-action-scope-repetition.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllTextAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
            repetition = Newtonsoft.Json.JsonConvert.DeserializeObject<WorkflowRunDetailsActionRepetition>(json);
        }

        // Act
        var forEachActionRepetition = new ForEachActionRepetition(repetition!);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(forEachActionRepetition.Id, Is.EqualTo(repetition!.Id));
            Assert.That(forEachActionRepetition.Name, Is.EqualTo(repetition.Name));
            Assert.That(forEachActionRepetition.Type, Is.EqualTo(repetition.Type));
            Assert.That(forEachActionRepetition.Code, Is.EqualTo(repetition.Properties?.Code));
            Assert.That(forEachActionRepetition.Status, Is.EqualTo(repetition.Properties?.Status));
            Assert.That(forEachActionRepetition.StartTime, Is.EqualTo(repetition.Properties?.StartTime));
            Assert.That(forEachActionRepetition.EndTime, Is.EqualTo(repetition.Properties?.EndTime));
            Assert.That(forEachActionRepetition.ActionTrackingId, Is.EqualTo(repetition.Properties?.Correlation?.ActionTrackingId));
            Assert.That(forEachActionRepetition.ClientTrackingId, Is.EqualTo(repetition.Properties?.Correlation?.ClientTrackingId));
            Assert.That(forEachActionRepetition.RepetitionIndexes, Is.Not.Null);
            Assert.That(forEachActionRepetition.RepetitionIndexes, Has.Count.EqualTo(3));
            Assert.That(forEachActionRepetition.RepetitionIndexes![0].ScopeName, Is.EqualTo("For_each_action_1"));
            Assert.That(forEachActionRepetition.RepetitionIndexes[0].ItemIndex, Is.Zero);
            Assert.That(forEachActionRepetition.Actions, Is.Not.Null);
            Assert.That(forEachActionRepetition.Actions, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_With_Multiple_RepetitionIndexes()
    {
        // Arrange
        // Normally the data is retrieved through this URL
        // https://management.azure.com/subscriptions/subscription-id/resourceGroups/resource-group/providers/Microsoft.Web/sites/logicapp-name/hostruntime/runtime/webhooks/workflow/api/management/workflows/workflow-name/runs/wun-id/actions/For_each_action_3/scopeRepetitions/000000-000000-000000?api-version=api_version
        WorkflowRunDetailsActionRepetition? repetition = null;

        var filePath = Path.Combine(AppContext.BaseDirectory, "ManagementApiResponseMessages", "Foreach-action-scope-repetition.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllTextAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult();
            repetition = Newtonsoft.Json.JsonConvert.DeserializeObject<WorkflowRunDetailsActionRepetition>(json);
        }

        // Act
        var forEachActionRepetition = new ForEachActionRepetition(repetition!);
    
        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(forEachActionRepetition.RepetitionIndexes, Is.Not.Null);
            Assert.That(forEachActionRepetition.RepetitionIndexes, Has.Count.EqualTo(3));
            Assert.That(forEachActionRepetition.RepetitionIndexes![0].ScopeName, Is.EqualTo("For_each_action_1"));
            Assert.That(forEachActionRepetition.RepetitionIndexes[0].ItemIndex, Is.Zero);
            Assert.That(forEachActionRepetition.RepetitionIndexes[1].ScopeName, Is.EqualTo("For_each_action_2"));
            Assert.That(forEachActionRepetition.RepetitionIndexes[1].ItemIndex, Is.EqualTo(1));
            Assert.That(forEachActionRepetition.RepetitionIndexes[2].ScopeName, Is.EqualTo("For_each_action_3"));
            Assert.That(forEachActionRepetition.RepetitionIndexes[2].ItemIndex, Is.EqualTo(2));
        }
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_When_RepetitionIndexes_Is_Empty()
    {
        // Arrange
        var repetition = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/workflow-name/runs/run-id/actions/For_each_action_3/scopeRepetitions/000000-000000-000000",
            Name = "000000-000000-000000",
            Type = "workflows/run/actions/scoperepetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                RepetitionIndexes = []
            }
        };

        // Act
        var forEachActionRepetition = new ForEachActionRepetition(repetition);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(forEachActionRepetition.Id, Is.EqualTo(repetition.Id));
            Assert.That(forEachActionRepetition.Name, Is.EqualTo(repetition.Name));
            Assert.That(forEachActionRepetition.Type, Is.EqualTo(repetition.Type));
            Assert.That(forEachActionRepetition.Code, Is.EqualTo("OK"));
            Assert.That(forEachActionRepetition.Status, Is.EqualTo("Succeeded"));
            Assert.That(forEachActionRepetition.RepetitionIndexes, Is.Not.Null);
            Assert.That(forEachActionRepetition.RepetitionIndexes, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_Initialize_Properties_When_Correlation_Is_Null()
    {
        // Arrange
        var repetition = new WorkflowRunDetailsActionRepetition
        {
            Id = "/workflows/workflow-name/runs/run-id/actions/For_each_action_3/scopeRepetitions/000000-000000-000000",
            Name = "000000-000000-000000",
            Type = "workflows/run/actions/scoperepetitions",
            Properties = new WorkflowRunDetailsActionRepetitionProperties
            {
                Code = "OK",
                Status = "Succeeded",
                Correlation = null,
                RepetitionIndexes = []
            }
        };

        // Act
        var forEachActionRepetition = new ForEachActionRepetition(repetition);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(forEachActionRepetition.ActionTrackingId, Is.Null);
            Assert.That(forEachActionRepetition.ClientTrackingId, Is.Null);
        }
    }
}