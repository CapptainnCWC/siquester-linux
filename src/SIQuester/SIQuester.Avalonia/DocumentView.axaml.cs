using Avalonia.Controls;
using Avalonia.Interactivity;
using SIQuester.ViewModel;

namespace SIQuester.Avalonia;

public partial class DocumentView : UserControl
{
    private Editors? _editors;

    public DocumentView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BuildToolbar();
    }

    private QDocument? Doc => DataContext as QDocument;

    private PackageViewModel? Package => Doc?.Package;

    private void BuildToolbar()
    {
        Toolbar.Children.Clear();
        if (Doc is not { } doc) return;

        void add(string text, Action action, string tip)
        {
            var b = Ui.Btn(text, action, tip);
            b.Margin = new global::Avalonia.Thickness(0, 0, 4, 0);
            Toolbar.Children.Add(b);
        }

        add("💾", () => doc.Save.Execute(null), "Сохранить (Ctrl+S)");
        add("↶", () => doc.OperationsManager.Undo.Execute(null), "Отменить (Ctrl+Z)");
        add("↷", () => doc.OperationsManager.Redo.Execute(null), "Повторить (Ctrl+Y)");
        add("＋ Раунд", () => Package?.AddRound.Execute(null), "Добавить раунд");
        add("＋ Тема", AddTheme, "Добавить тему в текущий раунд");
        add("＋ Вопрос", AddQuestion, "Добавить вопрос в текущую тему");
        add("⧉ Клон", CloneNode, "Клонировать выбранное");
        add("🗑 Удалить", RemoveNode, "Удалить выбранное");
        add("⇲ Развернуть всё", () => SetExpanded(true), "Развернуть всё");
        add("⇱ Свернуть всё", () => SetExpanded(false), "Свернуть всё");
    }

    private void SetExpanded(bool value)
    {
        if (Package is not { } p) return;
        p.IsExpanded = true;
        foreach (var r in p.Rounds)
        {
            r.IsExpanded = value;
            foreach (var t in r.Themes) t.IsExpanded = value;
        }
    }

    private RoundViewModel? CurrentRound() => Tree.SelectedItem switch
    {
        RoundViewModel r => r,
        ThemeViewModel t => t.OwnerRound,
        QuestionViewModel q => q.OwnerTheme?.OwnerRound,
        _ => Package?.Rounds.LastOrDefault()
    };

    private ThemeViewModel? CurrentTheme() => Tree.SelectedItem switch
    {
        ThemeViewModel t => t,
        QuestionViewModel q => q.OwnerTheme,
        RoundViewModel r => r.Themes.LastOrDefault(),
        _ => null
    };

    private void AddTheme()
    {
        if (CurrentRound() is { } round) round.AddTheme.Execute(null);
        else PlatformSpecificMessage("Сначала создайте раунд.");
    }

    private void AddQuestion()
    {
        if (CurrentTheme() is { } theme) theme.AddQuestion.Execute(null);
        else PlatformSpecificMessage("Сначала создайте тему.");
    }

    private void CloneNode()
    {
        switch (Tree.SelectedItem)
        {
            case RoundViewModel r: r.Clone.Execute(null); break;
            case ThemeViewModel t: t.Clone.Execute(null); break;
            case QuestionViewModel q: q.Clone.Execute(null); break;
        }
    }

    private void RemoveNode()
    {
        switch (Tree.SelectedItem)
        {
            case RoundViewModel r: r.Remove?.Execute(null); break;
            case ThemeViewModel t: t.Remove?.Execute(null); break;
            case QuestionViewModel q: q.Remove?.Execute(null); break;
        }
    }

    private static void PlatformSpecificMessage(string message) =>
        SIQuester.ViewModel.PlatformSpecific.PlatformManager.Instance.Inform(message);

    private void Tree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Doc is not { } doc) return;
        _editors ??= new Editors(Editor);
        var node = Tree.SelectedItem as IItemViewModel;
        doc.ActiveNode = node;
        _editors.Show(node);
    }
}
