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
                DocumentLoaderViewModel => new TextBlock { Text = "Загрузка…", Margin = new global::Avalonia.Thickness(16) },
                WorkspaceViewModel w => new TextBlock { Text = w.Header + "\n(в Linux-версии пока не поддерживается)", Margin = new global::Avalonia.Thickness(16) },
                _ => null
            };
    }
}
