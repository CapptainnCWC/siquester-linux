namespace SIQuester.ViewModel.Contracts;

/// <summary>
/// Defines platform-specific logic.
/// </summary>
public interface IPlatformService
{
    string[] FontFamilies { get; }

    void ShowHelp();
}
