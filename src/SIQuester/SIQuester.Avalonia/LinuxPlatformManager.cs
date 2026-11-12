using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SIPackages;
using SIQuester.Model;
using SIQuester.ViewModel;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Model;
using SIQuester.ViewModel.PlatformSpecific;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace SIQuester.Avalonia;

/// <summary>Avalonia implementation of platform-specific services (dialogs, messages, files).</summary>
internal sealed class LinuxPlatformManager : PlatformManager, IPlatformService
{
    private readonly Window _owner;

    public LinuxPlatformManager(Window owner) => _owner = owner;

    public override string[] FontFamilies => new[] { "Sans", "Serif", "Monospace" };

    /// <summary>Runs an async UI operation synchronously by pumping a nested dispatcher frame.</summary>
    private static T RunSync<T>(Func<Task<T>> operation)
    {
        T result = default!;
        Exception? error = null;
        var frame = new DispatcherFrame();

        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try { result = await operation(); }
            catch (Exception exc) { error = exc; }
            finally { frame.Continue = false; }
        });

        Dispatcher.UIThread.PushFrame(frame);

        if (error != null)
        {
            throw error;
        }

        return result;
    }

    private static FilePickerFileType Siq => new("SI packages") { Patterns = new[] { "*.siq" } };

    public override string[]? ShowOpenUI() => RunSync(async () =>
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = new[] { Siq, FilePickerFileTypes.All }
        });

        return files.Count == 0 ? null : files.Select(f => f.Path.LocalPath).ToArray();
    });

    public override string[]? ShowMediaOpenUI(string mediaCategory, bool allowAnyFile, bool multiselect = true) => RunSync(async () =>
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = multiselect,
            Title = mediaCategory,
            FileTypeFilter = new[] { FilePickerFileTypes.All }
        });

        return files.Count == 0 ? null : files.Select(f => f.Path.LocalPath).ToArray();
    });

    public override bool ShowSaveUI(string? title, string defaultExtension, Dictionary<string, string>? filter, [NotNullWhen(true)] ref string? filename)
    {
        var suggested = filename;
        var ext = defaultExtension.TrimStart('.');

        var path = RunSync(async () =>
        {
            var file = await _owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                DefaultExtension = ext,
                SuggestedFileName = suggested == null ? null : Path.GetFileName(suggested),
                FileTypeChoices = new[] { new FilePickerFileType(ext) { Patterns = new[] { "*." + ext } } }
            });

            return file?.Path.LocalPath;
        });

        if (path == null)
        {
            return false;
        }

        filename = path;
        return true;
    }

    public override bool ShowExportUI(string title, Dictionary<string, string> filter, [NotNullWhen(true)] ref string? filename, ref int filterIndex, out Encoding encoding, out bool start)
    {
        encoding = Encoding.UTF8;
        start = false;
        var ext = filter.Keys.FirstOrDefault() ?? "txt";
        return ShowSaveUI(title, ext, filter, ref filename);
    }

    public override string? ShowImportUI(string fileExtension, string fileFilter) => RunSync(async () =>
    {
        var files = await _owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType(fileExtension) { Patterns = new[] { "*" + fileExtension } }, FilePickerFileTypes.All }
        });

        return files.Count == 0 ? null : files[0].Path.LocalPath;
    });

    public override string? SelectSearchFolder() => RunSync(async () =>
    {
        var folders = await _owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions());
        return folders.Count == 0 ? null : folders[0].Path.LocalPath;
    });

    public override Tuple<int, int, int>? GetCurrentItemSelectionArea() => null;

    public override void CreatePreview(SIDocument document) { }

    public override IMedia PrepareMedia(IMedia media, string type) => media;

    public override void ClearMedia(IEnumerable<string> media) { }

    public override string? AskText(string title, bool multiline = false) => RunSync(async () =>
    {
        var box = new TextBox { AcceptsReturn = multiline, MinWidth = 360, MinHeight = multiline ? 120 : 0 };
        string? result = null;
        var ok = new Button { Content = "OK", IsDefault = true };
        var cancel = new Button { Content = "Отмена", IsCancel = true };
        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new global::Avalonia.Thickness(12),
                Spacing = 8,
                Children =
                {
                    box,
                    new StackPanel
                    {
                        Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                        Children = { ok, cancel }
                    }
                }
            }
        };

        ok.Click += (_, _) => { result = box.Text; dialog.Close(); };
        cancel.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(_owner);
        return result;
    });

    public override IEnumerable<string>? AskTags(ItemsViewModel<string> tags)
    {
        var text = AskText("Теги через запятую", false);
        return text == null ? null : text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public override IFlowDocumentWrapper BuildDocument(SIDocument doc, ExportFormats format) =>
        throw new NotSupportedException("Document export is not available in the Linux build.");

    public override void ExportTable(SIDocument doc, string filename) =>
        throw new NotSupportedException("Table export is not available in the Linux build.");

    public override void ShowHelp()
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/VladimirKhil/SI/wiki") { UseShellExecute = true });
        }
        catch { }
    }

    public override void AddToRecentCategory(string fileName) { }

    private void Message(string message, string title) => RunSync(async () =>
    {
        var close = new Button { Content = "OK", IsDefault = true, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right };
        var dialog = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new global::Avalonia.Thickness(16),
                Spacing = 12,
                Children = { new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap }, close }
            }
        };

        close.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(_owner);
        return true;
    });

    public override void ShowErrorMessage(string message) => Message(message, "SIQuester");

    public override void ShowExclamationMessage(string message) => Message(message, "SIQuester");

    public override void Inform(string message, bool exclamation = false) => Message(message, "SIQuester");

    public override void ShowSelectOptionDialog(string message, params UserOption[] options) => RunSync(async () =>
    {
        var panel = new StackPanel { Margin = new global::Avalonia.Thickness(16), Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        var dialog = new Window
        {
            Title = "SIQuester",
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = panel
        };

        foreach (var option in options)
        {
            var button = new Button { Content = option.Title };
            ToolTip.SetTip(button, option.Description);
            button.Click += (_, _) => { dialog.Close(); option.Callback(); };
            panel.Children.Add(button);
        }

        await dialog.ShowDialog(_owner);
        return true;
    });

    private bool? Ask(string message, bool cancel) => RunSync(async () =>
    {
        bool? result = cancel ? null : false;
        var yes = new Button { Content = "Да", IsDefault = true };
        var no = new Button { Content = "Нет" };
        var buttons = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right };
        buttons.Children.Add(yes);
        buttons.Children.Add(no);

        if (cancel)
        {
            var cancelButton = new Button { Content = "Отмена", IsCancel = true };
            buttons.Children.Add(cancelButton);
            cancelButton.Click += (_, _) => { result = null; };
        }

        var dialog = new Window
        {
            Title = "SIQuester",
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new global::Avalonia.Thickness(16),
                Spacing = 12,
                Children = { new TextBlock { Text = message, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap }, buttons }
            }
        };

        yes.Click += (_, _) => { result = true; dialog.Close(); };
        no.Click += (_, _) => { result = false; dialog.Close(); };
        if (cancel)
        {
            ((Button)buttons.Children[2]).Click += (_, _) => dialog.Close();
        }

        await dialog.ShowDialog(_owner);
        return result;
    });

    public override bool Confirm(string message) => Ask(message, false) == true;

    public override bool? ConfirmWithCancel(string message) => Ask(message, true);

    public override bool ConfirmExclamationWithWindow(string message) => Ask(message, false) == true;

    public override void Exit() => _owner.Close();

    public override string CompressImage(string imageUri) => imageUri;

    private static Dictionary<string, JsonElement>? _info;

    public override void CopyInfo(object info) =>
        _info = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(info));

    public override Dictionary<string, JsonElement>? PasteInfo() => _info;

    private sealed class NoopDisposable : IDisposable { public void Dispose() { } }

    public override IDisposable ShowProgressDialog() => new NoopDisposable();
}
