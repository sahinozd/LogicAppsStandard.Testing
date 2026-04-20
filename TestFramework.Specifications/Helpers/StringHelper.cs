namespace LogicApps.TestFramework.Specifications.Helpers;

public static class StringHelper
{
    /// <summary>
    /// Returns the specified string value, or null if the input is null or represents a null value as the string "null" or "NULL".
    /// </summary>
    /// <remarks>This method treats the strings "null" and "NULL" as equivalent to a null reference, returning null in those cases.
    /// Use this method to normalize possible null representations in user input or data sources.</remarks>
    /// <param name="value">The input string to evaluate. May be null or a string representation of null ("null" or "NULL").</param>
    /// <returns>The original string value if it is not null and does not equal "null" or "NULL"; otherwise, null.</returns>
    public static string? GetPossibleNullValue(string? value)
    {
        return value is null or "null" or "NULL" ? null : value;
    }
}
