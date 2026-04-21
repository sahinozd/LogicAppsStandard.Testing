using System.Globalization;
using System.Text.RegularExpressions;

namespace LogicApps.TestFramework.Specifications.Helpers;

public static class ClassHelper
{
    private static readonly Regex IndexerRegex = new(@"^(\w+)\[(\d+)\]$");

    /// <summary>
    /// This method set the value for nested properties (compounds) that are split by a dot.
    /// </summary>
    /// <param name="compoundProperty">The path to a nested property. E.g. Root.Sub.Property </param>
    /// <param name="target">The object to set the property for</param>
    /// <param name="value">the property value</param>
    public static void SetProperty(string compoundProperty, object? target, object value)
    {
        ArgumentNullException.ThrowIfNull(compoundProperty);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(value);

        var bits = compoundProperty.Split('.');

        for (var i = 0; i < bits.Length - 1; i++)
        {
            var propertyToGet = target!.GetType().GetProperty(bits[i]);
            var propertyValue = propertyToGet!.GetValue(target, null);
            if (propertyValue == null)
            {
                propertyValue = Activator.CreateInstance(propertyToGet.PropertyType); propertyToGet.SetValue(target, propertyValue);
            }

            target = propertyToGet.GetValue(target, null);
        }

        var propertyToSet = target!.GetType().GetProperty(bits.Last());
        var propertyType = propertyToSet!.PropertyType;
        var realPropertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var newValue = Convert.ChangeType(value, realPropertyType, CultureInfo.CurrentCulture);

        propertyToSet.SetValue(target, newValue, null);
    }

    /// <summary>
    /// This method set the value for nested properties (compounds) that are split by a dot. This version supports arrays and lists.
    /// </summary>
    /// <param name="compoundProperty">The path to a nested property. E.g. Root.Sub.Property </param>
    /// <param name="target">The object to set the property for</param>
    /// <param name="value">the property value</param>
    public static void SetPropertyWithLists(string compoundProperty, object? target, object value)
    {
        ArgumentNullException.ThrowIfNull(compoundProperty);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(value);

        var bits = compoundProperty.Split('.');

        for (var i = 0; i < bits.Length - 1; i++)
        {
            target = GetPropertyOrIndexedValue(target!, bits[i]);
        }

        // Set the final property
        SetFinalPropertyOrIndexedValue(target!, bits[^1], value);
    }

    /// <summary>
    /// Retrieves the value of a property or an indexed element from the specified target object. If the property or collection does not exist, it is instantiated automatically.
    /// </summary>
    /// <remarks>If the requested property or collection is null, a new instance is created and assigned to the property before returning the value.
    /// This method supports both simple property access and indexed collection access using the format "PropertyName[Index]".</remarks>
    /// <param name="target">The object from which to retrieve the property or indexed value. Cannot be null.</param>
    /// <param name="part">The name of the property or the property with an indexer (e.g., "Items[0]") to retrieve from the target object. Cannot be null or empty.</param>
    /// <returns>The value of the specified property or the element at the given index within a collection property.
    /// If the property or collection does not exist, a new instance is created and returned.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified property is not a list when an indexed value is requested.</exception>
    private static object? GetPropertyOrIndexedValue(object target, string part)
    {
        var match = IndexerRegex.Match(part);
        if (match.Success)
        {
            var propertyName = match.Groups[1].Value;
            var index = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

            var collectionProp = target.GetType().GetProperty(propertyName)!;
            var collection = collectionProp.GetValue(target);

            if (collection == null)
            {
                // Instantiate collection if it's null
                collection = Activator.CreateInstance(collectionProp.PropertyType);
                collectionProp.SetValue(target, collection);
            }

            if (collection is not System.Collections.IList list)
            {
                throw new InvalidOperationException($"Property '{propertyName}' is not a list.");
            }

            // Expand the list if needed
            while (list.Count <= index)
            {
                var elementType = collectionProp.PropertyType.IsArray
                    ? collectionProp.PropertyType.GetElementType()!
                    : collectionProp.PropertyType.GetGenericArguments()[0];
                list.Add(Activator.CreateInstance(elementType));
            }

            return list[index];

        }

        var propInfo = target.GetType().GetProperty(part)!;
        var value = propInfo.GetValue(target);
        if (value != null)
        {
            return value;
        }

        value = Activator.CreateInstance(propInfo.PropertyType);
        propInfo.SetValue(target, value);

        return value;
    }

    /// <summary>
    /// Sets the value of a property or an indexed element on the specified target object, creating intermediate collections or elements as needed.
    /// </summary>
    /// <remarks>If the target property is a collection and the specified index is out of range, the collection is automatically expanded and new elements are created as needed.
    /// The method performs type conversion for the assigned value based on the property's or element's type.</remarks>
    /// <param name="target">The object whose property or indexed value is to be set. Cannot be null.</param>
    /// <param name="part">The property name or indexed property accessor (e.g., "PropertyName" or "CollectionProperty[0]") indicating which member to set.</param>
    /// <param name="value">The value to assign to the property or indexed element. The value will be converted to the appropriate type if necessary.</param>
    /// <exception cref="InvalidOperationException">Thrown if the specified property is not a list when an indexed assignment is attempted.</exception>
    private static void SetFinalPropertyOrIndexedValue(object target, string part, object value)
    {
        var match = IndexerRegex.Match(part);
        if (match.Success)
        {
            var propertyName = match.Groups[1].Value;
            var index = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

            var collectionProp = target.GetType().GetProperty(propertyName)!;
            var collection = collectionProp.GetValue(target);

            if (collection == null)
            {
                collection = Activator.CreateInstance(collectionProp.PropertyType);
                collectionProp.SetValue(target, collection);
            }

            if (collection is not System.Collections.IList list)
            {
                throw new InvalidOperationException($"Property '{propertyName}' is not a list.");
            }

            var elementType = collectionProp.PropertyType.IsArray
                ? collectionProp.PropertyType.GetElementType()!
                : collectionProp.PropertyType.GetGenericArguments()[0];

            while (list.Count <= index)
            {
                list.Add(Activator.CreateInstance(elementType));
            }

            var realType = Nullable.GetUnderlyingType(elementType) ?? elementType;
            var convertedValue = Convert.ChangeType(value, realType, CultureInfo.CurrentCulture);
            list[index] = convertedValue;
        }
        else
        {
            var propInfo = target.GetType().GetProperty(part)!;
            var propType = propInfo.PropertyType;
            var realType = Nullable.GetUnderlyingType(propType) ?? propType;
            var convertedValue = Convert.ChangeType(value, realType, CultureInfo.CurrentCulture);
            propInfo.SetValue(target, convertedValue);
        }
    }
}