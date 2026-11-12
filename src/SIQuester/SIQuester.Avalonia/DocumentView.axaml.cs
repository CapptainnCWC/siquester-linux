using Avalonia.Controls;
using Avalonia.Data;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel;

namespace SIQuester.Avalonia;

public partial class DocumentView : UserControl
{
    public DocumentView() => InitializeComponent();

    private void Tree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not QDocument doc)
        {
            return;
        }

        var item = Tree.SelectedItem;
        doc.ActiveNode = item as IItemViewModel;
        Editor.Children.Clear();

        switch (item)
        {
            case PackageViewModel p:
                AddText("Название пакета", p.Model, nameof(Package.Name));
                AddText("Издатель", p.Model, nameof(Package.Publisher));
                AddText("Дата", p.Model, nameof(Package.Date));
                AddText("Ограничения", p.Model, nameof(Package.Restriction));
                break;
            case RoundViewModel r:
                AddText("Название раунда", r.Model, nameof(Round.Name));
                break;
            case ThemeViewModel t:
                AddText("Название темы", t.Model, "Name");
                break;
            case QuestionViewModel q:
                BuildQuestion(q);
                break;
        }
    }

    private void AddText(string label, object source, string property, bool multiline = false)
    {
        Editor.Children.Add(new TextBlock { Text = label, Opacity = 0.7 });
        var box = new TextBox { AcceptsReturn = multiline, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap };
        box.Bind(TextBox.TextProperty, new Binding(property) { Source = source, Mode = BindingMode.TwoWay });
        Editor.Children.Add(box);
    }

    private void BuildQuestion(QuestionViewModel q)
    {
        Editor.Children.Add(new TextBlock { Text = "Вопрос", FontWeight = global::Avalonia.Media.FontWeight.Bold, FontSize = 18 });
        AddText("Цена", q.Model, nameof(Question.Price));
        AddText("Тип", q.Model, nameof(Question.TypeName));

        Editor.Children.Add(new TextBlock { Text = "Содержимое вопроса", Opacity = 0.7 });
        foreach (var content in q.Model.GetContent())
        {
            if (content.Type == ContentTypes.Text)
            {
                AddText("текст", content, nameof(ContentItem.Value), multiline: true);
            }
            else
            {
                Editor.Children.Add(new TextBlock { Text = $"[{content.Type}] {content.Value}" });
            }
        }

        Editor.Children.Add(new TextBlock { Text = "Правильные ответы", Opacity = 0.7 });
        for (var i = 0; i < q.Right.Count; i++)
        {
            var index = i;
            var box = new TextBox { Text = q.Right[index] };
            box.LostFocus += (_, _) => { if (index < q.Right.Count && q.Right[index] != box.Text) q.Right[index] = box.Text ?? ""; };
            Editor.Children.Add(box);
        }
    }
}
