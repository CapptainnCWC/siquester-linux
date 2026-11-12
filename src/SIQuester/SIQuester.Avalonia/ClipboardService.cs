using SIQuester.ViewModel.Contracts.Host;

namespace SIQuester.Avalonia;

/// <summary>In-process clipboard for internal copy/paste of package items.</summary>
internal sealed class ClipboardService : IClipboardService
{
    private readonly Dictionary<string, object> _data = new();

    public bool ContainsData(string format) => _data.ContainsKey(format);

    public object? GetData(string format) => _data.TryGetValue(format, out var v) ? v : null;

    public void SetData(string format, object data) => _data[format] = data;
}
