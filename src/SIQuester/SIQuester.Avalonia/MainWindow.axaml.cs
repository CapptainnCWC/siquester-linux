using Avalonia.Controls;
using Avalonia.Interactivity;
using SIQuester.ViewModel;
using SIQuester.ViewModel.PlatformSpecific;

namespace SIQuester.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var shot = Environment.GetEnvironmentVariable("SIQ_SCREENSHOT");
        if (shot != null)
        {
            Opened += async (_, _) =>
            {
                await Task.Delay(6000);
                var size = new global::Avalonia.PixelSize((int)Bounds.Width, (int)Bounds.Height);
                using var bmp = new global::Avalonia.Media.Imaging.RenderTargetBitmap(size);
                bmp.Render(this);
                bmp.Save(shot);
                Close();
            };
        }

        Tabs.SelectionChanged += (_, _) =>
        {
            if (DataContext is MainViewModel main)
            {
                main.ActiveDocument = Tabs.SelectedItem as QDocument;
            }
        };
    }

    private QDocument? Doc => (DataContext as MainViewModel)?.ActiveDocument;

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (Doc is { } d) await d.Save.ExecuteAsync(null);
    }

    private async void SaveAs_Click(object? sender, RoutedEventArgs e)
    {
        if (Doc is { } d) await d.SaveAs.ExecuteAsync(null);
    }

    private void Undo_Click(object? sender, RoutedEventArgs e) => Doc?.OperationsManager.Undo.Execute(null);

    private void Redo_Click(object? sender, RoutedEventArgs e) => Doc?.OperationsManager.Redo.Execute(null);

    private async void Update_Click(object? sender, RoutedEventArgs e) =>
        await Updater.CheckAndUpdateAsync(PlatformManager.Instance);

    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();
}
