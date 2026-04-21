using System.Globalization;
using System.Text.RegularExpressions;

namespace LogicApps.TestFramework.Specifications.Resolvers;

public static class FileNameResolver
{
    private const string PlaceholderStart = "%[";
    private const string PlaceholderEnd = "]%";
    
    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    private static readonly Regex Regex = new(@"%\[(.*?)\]%");

    /// <summary>
    /// Replaces a specified placeholder in the file name with the current date and time, formatted according to the provided date format string.
    /// </summary>
    /// <remarks>The replacement is performed using a case-insensitive comparison based on the invariant culture.
    /// If the placeholder does not exist in the file name, the original file name is returned unchanged.</remarks>
    /// <param name="filename">The file name in which to replace the placeholder. Cannot be null or empty.</param>
    /// <param name="placeholder">The placeholder string within the file name to be replaced by the current date and time.
    /// Cannot be null or empty.</param>
    /// <param name="dateFormat">A standard or custom date and time format string that determines how the current date and time will be formatted. Cannot be null or empty.</param>
    /// <returns>A new string representing the file name with the placeholder replaced by the current date and time, formatted as specified.</returns>
    public static string ReplaceFileNameWithCurrentDateTime(string filename, string placeholder, string dateFormat)
    {
        ArgumentException.ThrowIfNullOrEmpty(filename);
        ArgumentException.ThrowIfNullOrEmpty(placeholder);
        ArgumentException.ThrowIfNullOrEmpty(dateFormat);

        var dateString = DateTime.Now.ToString(dateFormat, CultureInfo.InvariantCulture);
        filename = filename.Replace(placeholder, dateString, StringComparison.InvariantCultureIgnoreCase);

        return filename;
    }

    /// <summary>
    /// Resolves all placeholders in the specified file name template and returns the resulting file name.
    /// </summary>
    /// <remarks>Placeholders in the template are identified and replaced according to the application's placeholder resolution logic.
    /// If the template contains no placeholders, the original string is returned unchanged.</remarks>
    /// <param name="inputTemplate">The file name template containing one or more placeholders to be resolved. Cannot be null or empty.</param>
    /// <returns>A string representing the file name with all placeholders replaced by their resolved values.</returns>
    public static string ResolveFileName(string inputTemplate)
    {
        ArgumentException.ThrowIfNullOrEmpty(inputTemplate);

        var placeholders = new List<string>();
        foreach (var match in Regex.Matches(inputTemplate))
        {
            var placeholder = match.ToString()?.Replace(PlaceholderStart, string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace(PlaceholderEnd, string.Empty, StringComparison.InvariantCultureIgnoreCase);

            if (placeholder != null)
            {
                placeholders.Add(placeholder);
            }
        }

        return placeholders.Aggregate(inputTemplate, ResolvePlaceholder);
    }

    /// <summary>
    /// Resolves a specified placeholder within the given input template and returns the resulting string.
    /// </summary>
    /// <remarks>Supported placeholders include "CorrelationId" and those starting with "DateTime" (case-insensitive).
    /// Other placeholders are ignored, and the template is returned as-is.</remarks>
    /// <param name="inputTemplate">The template string that may contain placeholders to be resolved. Cannot be null or empty.</param>
    /// <param name="placeholder">The name of the placeholder to resolve within the template. Cannot be null or empty.</param>
    /// <returns>A string with the specified placeholder resolved in the input template. If the placeholder is not recognized, the original template is returned unchanged.</returns>
    private static string ResolvePlaceholder(string inputTemplate, string placeholder)
    {
        ArgumentException.ThrowIfNullOrEmpty(inputTemplate);
        ArgumentException.ThrowIfNullOrEmpty(placeholder);

        return placeholder switch
        {
            // CorrelationId
            "CorrelationId" => ResolvePlaceholderCorrelationId(inputTemplate),

            // DateTime
            not null when placeholder.StartsWith("DateTime", StringComparison.InvariantCultureIgnoreCase) =>
                ResolvePlaceholderDateTime(inputTemplate, placeholder),

            // Default
            _ => inputTemplate
        };
    }

    /// <summary>
    /// Replaces the CorrelationId placeholder in the specified template with a newly generated GUID string.
    /// </summary>
    /// <remarks>The method performs a case-insensitive replacement of the CorrelationId placeholder. If the placeholder is not present,
    /// the original string is returned unchanged.</remarks>
    /// <param name="inputTemplate">The input string containing a CorrelationId placeholder to be replaced. The placeholder is case-insensitive.</param>
    /// <returns>A string with the CorrelationId placeholder replaced by a new GUID value.</returns>
    private static string ResolvePlaceholderCorrelationId(string inputTemplate)
    {
        var correlationId = Guid.NewGuid().ToString();

        return inputTemplate.Replace($"{PlaceholderStart}CorrelationId{PlaceholderEnd}", correlationId, StringComparison.InvariantCultureIgnoreCase);
    }
    
    /// <summary>
    /// Replaces a specified date and time placeholder in the input template with the current UTC date and time, formatted according to the placeholder's format string.
    /// </summary>
    /// <remarks>The method uses the invariant culture for formatting the date and time. The placeholder must follow the pattern 'DateTime(format)',
    /// where 'format' is a valid .NET date and time format string.</remarks>
    /// <param name="inputTemplate">The string containing the placeholder to be replaced with the formatted date and time.</param>
    /// <param name="placeholder">The placeholder specifying the date and time format to use for replacement. The format should be enclosed in 'DateTime(...)'.</param>
    /// <returns>A new string with the specified placeholder replaced by the current UTC date and time, formatted as indicated by the placeholder.</returns>
    private static string ResolvePlaceholderDateTime(string inputTemplate, string placeholder)
    {
        var dateTimeFormat = placeholder
            .Replace("DateTime(", string.Empty, StringComparison.InvariantCultureIgnoreCase)
            .Replace(")", string.Empty, StringComparison.InvariantCultureIgnoreCase);

        var dateTime = DateTime.UtcNow.ToString(dateTimeFormat, CultureInfo.InvariantCulture);

        return inputTemplate.Replace($"{PlaceholderStart}{placeholder}{PlaceholderEnd}", dateTime, StringComparison.InvariantCultureIgnoreCase);
    }
}