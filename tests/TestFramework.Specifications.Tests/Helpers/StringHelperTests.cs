using LogicApps.TestFramework.Specifications.Helpers;
using NUnit.Framework;

namespace LogicApps.TestFramework.Specifications.Tests.Helpers;

[TestFixture]
public class StringHelperTests
{
    [Test]
    public void GetPossibleNullValue_WithNullInput_ReturnsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = StringHelper.GetPossibleNullValue(input);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPossibleNullValue_WithLowercaseNullString_ReturnsNull()
    {
        // Arrange
        const string input = "null";

        // Act
        var result = StringHelper.GetPossibleNullValue(input);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPossibleNullValue_WithUppercaseNullString_ReturnsNull()
    {
        // Arrange
        const string input = "NULL";

        // Act
        var result = StringHelper.GetPossibleNullValue(input);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPossibleNullValue_WithNormalString_ReturnsOriginalString()
    {
        // Arrange
        const string input = "SomeValue";

        // Act
        var result = StringHelper.GetPossibleNullValue(input);

        // Assert
        Assert.That(result, Is.EqualTo("SomeValue"));
    }

    [Test]
    public void GetPossibleNullValue_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        const string input = "";

        // Act
        var result = StringHelper.GetPossibleNullValue(input);

        // Assert
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void GetPossibleNullValue_WithMixedCaseNull_ReturnsOriginalString()
    {
        // Arrange
        const string input = "Null";

        // Act
        var result = StringHelper.GetPossibleNullValue(input);

        // Assert
        Assert.That(result, Is.EqualTo("Null"));
    }

    [Test]
    public void GetPossibleNullValue_WithWhitespace_ReturnsWhitespace()
    {
        // Arrange
        const string input = "   ";

        // Act
        var result = StringHelper.GetPossibleNullValue(input);

        // Assert
        Assert.That(result, Is.EqualTo("   "));
    }
}
