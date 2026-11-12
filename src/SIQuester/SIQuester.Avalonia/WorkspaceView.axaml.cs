using Avalonia.Controls;
using SIQuester.ViewModel;

namespace SIQuester.Avalonia;

public partial class WorkspaceView : UserControl
{
    public WorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
            Host.Content = DataContext switch
            {
                QDocument doc => new DocumentView { DataContext = doc },
                NewViewModel n => BuildNew(n),
                DocumentLoaderViewModel => new TextBlock { Text = "Загрузка…", Margin = new global::Avalonia.Thickness(16) },
                WorkspaceViewModel w => new TextBlock { Text = w.Header + "\n(в Linux-версии пока не поддерживается)", Margin = new global::Avalonia.Thickness(16) },
                _ => null
            };
    }

    /// <summary>"New package" page: name, author, template and Create.</summary>
    private static Control BuildNew(NewViewModel vm)
    {
        var panel = new StackPanel { Margin = new global::Avalonia.Thickness(24), Spacing = 6, MaxWidth = 640, HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left };
        panel.Children.Add(Ui.Title("Новый пакет"));
        panel.Children.Add(Ui.Caption("Название")); panel.Children.Add(Ui.Text(vm, nameof(NewViewModel.PackageName)));
        panel.Children.Add(Ui.Caption("Автор")); panel.Children.Add(Ui.Text(vm, nameof(NewViewModel.PackageAuthor)));
        panel.Children.Add(Ui.Caption("Шаблон"));

        var description = new TextBlock { Opacity = 0.7, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap, MinHeight = 40 };
        var list = new ListBox { ItemsSource = vm.Templates.Select(t => t.Name).ToList(), MaxHeight = 240 };
        list.SelectedIndex = Math.Max(0, vm.Templates.IndexOf(vm.CurrentTemplate));
        description.Text = vm.CurrentTemplate.Description;
        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedIndex >= 0)
            {
                vm.CurrentTemplate = vm.Templates[list.SelectedIndex];
                description.Text = vm.CurrentTemplate.Description;
            }
        };

        panel.Children.Add(list);
        panel.Children.Add(description);
        panel.Children.Add(Ui.Cmd("Создать", vm.Create));
        return panel;
    }
}
