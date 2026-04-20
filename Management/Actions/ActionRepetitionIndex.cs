using System.Diagnostics.CodeAnalysis;

namespace LogicApps.Management.Actions;

/// <summary>
/// Represents an index within a ForEach/Scope repetition (item index and optional parent scope name).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ActionRepetitionIndex
{
    public int? ItemIndex { get; set; }

    public string? ScopeName { get; set; }
}