using SIQuester.ViewModel.Helpers;

namespace SIQuester.Model;

/// <summary>
/// Represents a search match inside a file.
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// File name.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Found fragment with highlighted area.
    /// </summary>
    public required SearchMatch Fragment { get; set; }
}
