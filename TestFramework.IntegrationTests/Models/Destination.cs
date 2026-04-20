using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace LogicApps.TestFramework.IntegrationTests.Models;

[ExcludeFromCodeCoverage(Justification = "This is a test model used for the transformation test")]
[XmlRoot(ElementName = "item")]
public record Destination
{
    [XmlElement(ElementName = "status")]
    public string? Status { get; set; }

    [XmlElement(ElementName = "action")]
    public string? Action { get; set; }
}