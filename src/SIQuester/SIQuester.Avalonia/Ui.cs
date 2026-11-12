using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.Specialized;
using System.Windows.Input;

namespace SIQuester.Avalonia;

/// <summary>Small helpers for building editor UI in code.</summary>
internal static class Ui
{
    public static TextBlock Caption(string text) => new() { Text = text, Opacity = 0.65, Margin = new Thickness(0, 6, 0, 0) };

    public static TextBlock Title(string text) =>
        new() { Text = text, FontSize = 20, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 4) };

    public static TextBlock Section(string text) =>
        new() { Text = text, FontSize = 15, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 14, 0, 2) };

    public static Button Btn(string text, Action onClick, string? tip = null)
    {
        var b = new Button { Content = text, Padding = new Thickness(8, 3) };
        b.Click += (_, _) => onClick();
        if (tip != null) ToolTip.SetTip(b, tip);
        return b;
    }

    public static Button Cmd(string text, ICommand? command, object? parameter = null, string? tip = null)
    {
        var b = new Button { Content = text, Padding = new Thickness(8, 3), Command = command, CommandParameter = parameter };
        if (tip != null) ToolTip.SetTip(b, tip);
        return b;
    }

    public static StackPanel Row(params Control[] children)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        p.Children.AddRange(children);
        return p;
    }

    public static WrapPanel Wrap(params Control[] children)
    {
        var p = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var c in children)
        {
            c.Margin = new Thickness(0, 0, 6, 4);
            p.Children.Add(c);
        }

        return p;
    }

    /// <summary>Two-way bound text box over a property of a (notifying) model.</summary>
    public static TextBox Text(object source, string property, bool multiline = false, string? watermark = null)
    {
        var box = new TextBox
        {
            AcceptsReturn = multiline,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            MinHeight = multiline ? 60 : 0,
            Watermark = watermark
        };

        box.Bind(TextBox.TextProperty, new Binding(property) { Source = source, Mode = BindingMode.TwoWay });
        return box;
    }

    /// <summary>Text box for a string-typed list item; commits on focus loss.</summary>
    public static TextBox ListText(IList<string> list, int index, bool multiline = false)
    {
        var box = new TextBox { Text = list[index], AcceptsReturn = multiline, TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap, MinWidth = 260 };
        box.LostFocus += (_, _) =>
        {
            if (index < list.Count && list[index] != (box.Text ?? ""))
            {
                list[index] = box.Text ?? "";
            }
        };

        return box;
    }

    public static ComboBox Combo(IEnumerable<(string Value, string Title)> items, string current, Action<string> onChange)
    {
        var list = items.ToList();
        if (list.All(i => i.Value != current)) list.Add((current, string.IsNullOrEmpty(current) ? "—" : current));
        var combo = new ComboBox { ItemsSource = list.Select(i => i.Title).ToList(), MinWidth = 220 };
        combo.SelectedIndex = list.FindIndex(i => i.Value == current);
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedIndex >= 0 && list[combo.SelectedIndex].Value != current)
            {
                current = list[combo.SelectedIndex].Value;
                onChange(current);
            }
        };

        return combo;
    }

    public static Border Card(Control content) => new()
    {
        Child = content,
        Padding = new Thickness(10),
        CornerRadius = new CornerRadius(6),
        Background = new SolidColorBrush(Color.Parse("#30302E")),
        Margin = new Thickness(0, 2)
    };
}
