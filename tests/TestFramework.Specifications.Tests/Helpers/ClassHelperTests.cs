using LogicApps.TestFramework.Specifications.Helpers;
using LogicApps.TestFramework.Specifications.Tests.Helpers.Test_classes;
using NUnit.Framework;

namespace LogicApps.TestFramework.Specifications.Tests.Helpers;

[TestFixture]
public class ClassHelperTests
{
    [Test]
    public void SetProperty_WithNullCompoundProperty_ThrowsArgumentNullException()
    {
        // Arrange
        var target = new TestClass();
        string compoundProperty = null!;
        const string value = "test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ClassHelper.SetProperty(compoundProperty, target, value));
    }

    [Test]
    public void SetProperty_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        object target = null!;
        const string compoundProperty = "StringProperty";
        const string value = "test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ClassHelper.SetProperty(compoundProperty, target, value));
    }

    [Test]
    public void SetProperty_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var target = new TestClass();
        const string compoundProperty = "StringProperty";
        object value = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ClassHelper.SetProperty(compoundProperty, target, value));
    }

    [Test]
    public void SetProperty_WithSimpleStringProperty_SetsValue()
    {
        // Arrange
        var target = new TestClass();
        const string compoundProperty = "StringProperty";
        const string value = "TestValue";

        // Act
        ClassHelper.SetProperty(compoundProperty, target, value);

        // Assert
        Assert.That(target.StringProperty, Is.EqualTo("TestValue"));
    }

    [Test]
    public void SetProperty_WithSimpleIntProperty_SetsValue()
    {
        // Arrange
        var target = new TestClass();
        const string compoundProperty = "IntProperty";
        const int value = 42;

        // Act
        ClassHelper.SetProperty(compoundProperty, target, value);

        // Assert
        Assert.That(target.IntProperty, Is.EqualTo(42));
    }

    [Test]
    public void SetProperty_WithNullableIntProperty_SetsValue()
    {
        // Arrange
        var target = new TestClass();
        const string compoundProperty = "NullableIntProperty";
        const int value = 99;

        // Act
        ClassHelper.SetProperty(compoundProperty, target, value);

        // Assert
        Assert.That(target.NullableIntProperty, Is.EqualTo(99));
    }

    [Test]
    public void SetProperty_WithNestedProperty_CreatesInstanceAndSetsValue()
    {
        // Arrange
        var target = new TestClass();
        const string compoundProperty = "Nested.NestedStringProperty";
        const string value = "NestedValue";

        // Act
        ClassHelper.SetProperty(compoundProperty, target, value);

        // Assert
        Assert.That(target.Nested, Is.Not.Null);
        Assert.That(target.Nested!.NestedStringProperty, Is.EqualTo("NestedValue"));
    }

    [Test]
    public void SetProperty_WithDeepNestedProperty_CreatesAllInstancesAndSetsValue()
    {
        // Arrange
        var target = new TestClass();
        const string compoundProperty = "Nested.DeepNested.DeepValue";
        const string value = "DeepValue";

        // Act
        ClassHelper.SetProperty(compoundProperty, target, value);

        // Assert
        Assert.That(target.Nested, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(target.Nested!.DeepNested, Is.Not.Null);
            Assert.That(target.Nested.DeepNested!.DeepValue, Is.EqualTo("DeepValue"));
        }
    }

    [Test]
    public void SetProperty_WithExistingNestedInstance_DoesNotOverwriteInstance()
    {
        // Arrange
        var target = new TestClass
        {
            Nested = new NestedClass { NestedIntProperty = 100 }
        };
        const string compoundProperty = "Nested.NestedStringProperty";
        const string value = "NewValue";

        // Act
        ClassHelper.SetProperty(compoundProperty, target, value);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(target.Nested.NestedStringProperty, Is.EqualTo("NewValue"));
            Assert.That(target.Nested.NestedIntProperty, Is.EqualTo(100)); // Original value preserved
        }
    }

    [Test]
    public void SetProperty_WithStringToIntConversion_ConvertsAndSetsValue()
    {
        // Arrange
        var target = new TestClass();
        const string compoundProperty = "IntProperty";
        const string value = "123";

        // Act
        ClassHelper.SetProperty(compoundProperty, target, value);

        // Assert
        Assert.That(target.IntProperty, Is.EqualTo(123));
    }

    [Test]
    public void SetPropertyWithLists_WithNullCompoundProperty_ThrowsArgumentNullException()
    {
        // Arrange
        var target = new ClassWithList();
        string compoundProperty = null!;
        const string value = "test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ClassHelper.SetPropertyWithLists(compoundProperty, target, value));
    }

    [Test]
    public void SetPropertyWithLists_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        object target = null!;
        const string compoundProperty = "Items[0].Name";
        const string value = "test";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ClassHelper.SetPropertyWithLists(compoundProperty, target, value));
    }

    [Test]
    public void SetPropertyWithLists_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var target = new ClassWithList();
        const string compoundProperty = "Items[0].Name";
        object value = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ClassHelper.SetPropertyWithLists(compoundProperty, target, value));
    }

    [Test]
    public void SetPropertyWithLists_WithIndexedProperty_CreatesItemsAndSetsValue()
    {
        // Arrange
        var target = new ClassWithList();
        const string compoundProperty = "Items[0].Name";
        const string value = "FirstItem";

        // Act
        ClassHelper.SetPropertyWithLists(compoundProperty, target, value);

        // Assert
        Assert.That(target.Items, Has.Count.EqualTo(1));
        Assert.That(target.Items[0].Name, Is.EqualTo("FirstItem"));
    }

    [Test]
    public void SetPropertyWithLists_WithMultipleIndexes_CreatesAllItemsAndSetsValue()
    {
        // Arrange
        var target = new ClassWithList();
        const string compoundProperty = "Items[2].Name";
        const string value = "ThirdItem";

        // Act
        ClassHelper.SetPropertyWithLists(compoundProperty, target, value);

        // Assert
        Assert.That(target.Items, Has.Count.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(target.Items[0], Is.Not.Null); // Automatically created
            Assert.That(target.Items[1], Is.Not.Null); // Automatically created
            Assert.That(target.Items[2].Name, Is.EqualTo("ThirdItem"));
        }
    }

    [Test]
    public void SetPropertyWithLists_WithExistingItems_PreservesOtherItems()
    {
        // Arrange
        var target = new ClassWithList();
        target.Items.Add(new ListItem { Name = "Existing", Value = 100 });
        const string compoundProperty = "Items[1].Name";
        const string value = "NewItem";

        // Act
        ClassHelper.SetPropertyWithLists(compoundProperty, target, value);

        // Assert
        Assert.That(target.Items, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(target.Items[0].Name, Is.EqualTo("Existing"));
            Assert.That(target.Items[0].Value, Is.EqualTo(100));
            Assert.That(target.Items[1].Name, Is.EqualTo("NewItem"));
        }
    }

    [Test]
    public void SetPropertyWithLists_WithSimpleProperty_WorksLikeSetProperty()
    {
        // Arrange
        var target = new TestClass();
        const string compoundProperty = "StringProperty";
        const string value = "SimpleValue";

        // Act
        ClassHelper.SetPropertyWithLists(compoundProperty, target, value);

        // Assert
        Assert.That(target.StringProperty, Is.EqualTo("SimpleValue"));
    }

    [Test]
    public void SetPropertyWithLists_WithNestedProperty_WorksLikeSetProperty()
    {
        // Arrange
        var target = new TestClass();
        const string compoundProperty = "Nested.NestedStringProperty";
        const string value = "NestedValue";

        // Act
        ClassHelper.SetPropertyWithLists(compoundProperty, target, value);

        // Assert
        Assert.That(target.Nested, Is.Not.Null);
        Assert.That(target.Nested!.NestedStringProperty, Is.EqualTo("NestedValue"));
    }
}