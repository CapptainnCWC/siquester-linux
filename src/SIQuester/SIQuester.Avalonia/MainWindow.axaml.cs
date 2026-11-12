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
                await Task.Delay(4000);
                if (Environment.GetEnvironmentVariable("SIQ_NEWTEST") == "1" && DataContext is MainViewModel mn)
                {
                    mn.New.Execute(null);
                    await Task.Delay(1000);
                    var shotNew = shot.Replace(".png", "_new.png");
                    { using var b0 = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new global::Avalonia.PixelSize((int)Bounds.Width, (int)Bounds.Height)); b0.Render(this); b0.Save(shotNew); }
                    ((NewViewModel)mn.DocList.Last()).Create.Execute(null);
                    await Task.Delay(1500);
                    Console.WriteLine($"docs={mn.DocList.Count} last={mn.DocList.Last().GetType().Name} active={mn.ActiveDocument?.Header}");
                    if (mn.ActiveDocument is { } nd) { nd.Package.AddRound.Execute(null); var r0 = nd.Package.Rounds.Last(); r0.AddTheme.Execute(null); r0.Themes.Last().AddQuestion.Execute(null); r0.IsExpanded = true; r0.Themes.Last().IsExpanded = true; }
                }
                if (Environment.GetEnvironmentVariable("SIQ_SELFTEST") == "1" && DataContext is MainViewModel m && m.ActiveDocument is { } d)
                {
                    d.Package.AddRound.Execute(null);
                    var round = d.Package.Rounds.Last();
                    round.Model.Name = "Раунд из теста";
                    round.AddTheme.Execute(null);
                    var theme = round.Themes.Last();
                    theme.Model.Name = "Тема из теста";
                    theme.AddQuestion.Execute(null);
                    theme.AddQuestion.Execute(null);
                    var q = theme.Questions.Last();
                    q.Right.AddItem.Execute("Ответ из теста");
                    Console.WriteLine($"rounds={d.Package.Rounds.Count} themes={round.Themes.Count} questions={theme.Questions.Count}");
                    round.IsExpanded = true; theme.IsExpanded = true; q.IsSelected = true;
                    d.ActiveNode = q;
                    await d.Save.ExecuteAsync(null);
                    Console.WriteLine("saved");
                    await Task.Delay(1500);
                }
                if (Environment.GetEnvironmentVariable("SIQ_SELECT") is { } sel && DataContext is MainViewModel m2 && m2.ActiveDocument is { } d2)
                {
                    var ix = sel.Split(',').Select(int.Parse).ToArray();
                    var rr = d2.Package.Rounds[ix[0]]; rr.IsExpanded = true;
                    var tt = rr.Themes[ix[1]]; tt.IsExpanded = true;
                    tt.Questions[ix[2]].IsSelected = true;
                    await Task.Delay(1500);
                }
                await Task.Delay(2000);
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
