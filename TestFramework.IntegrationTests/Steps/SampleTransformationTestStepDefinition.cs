using LogicApps.TestFramework.IntegrationTests.Models;
using LogicApps.TestFramework.Specifications;
using Reqnroll;
using System.Xml.Serialization;

namespace LogicApps.TestFramework.IntegrationTests.Steps;

[Binding, Scope(Feature = "Transformation-Test-Sample-StepDefinition")]
public class SampleTransformationTestStepDefinition : BaseTransformationStepDefinition<Source, Destination>
{
    protected override Destination DeserializeTransformedBody(string body)
    {
        using TextReader reader = new StringReader(body);
        return (Destination)new XmlSerializer(typeof(Destination)).Deserialize(reader)!;
    }
}