using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace SIQuester.Avalonia;

/// <summary>Builds the property editor panel for the selected tree node.</summary>
internal sealed class Editors
{
    private readonly Panel _host;
    private readonly List<Action> _unsubscribe = new();
    private IItemViewModel? _node;
    private bool _pending;

    public Editors(Panel host) => _host = host;

    public void Show(IItemViewModel? node)
    {
        _node = node;
        Rebuild();
    }

    private void RequestRebuild()
    {
        if (_pending) return;
        _pending = true;
        Dispatcher.UIThread.Post(() => { _pending = false; Rebuild(); }, DispatcherPriority.Background);
    }

    private void Watch(INotifyCollectionChanged collection)
    {
        void handler(object? s, NotifyCollectionChangedEventArgs e) => RequestRebuild();
        collection.CollectionChanged += handler;
        _unsubscribe.Add(() => collection.CollectionChanged -= handler);
    }

    private void WatchProperty(INotifyPropertyChanged source, params string[] names)
    {
        void handler(object? s, PropertyChangedEventArgs e)
        {
            if (names.Contains(e.PropertyName)) RequestRebuild();
        }

        source.PropertyChanged += handler;
        _unsubscribe.Add(() => source.PropertyChanged -= handler);
    }

    private void Rebuild()
    {
        foreach (var u in _unsubscribe) u();
        _unsubscribe.Clear();
        _host.Children.Clear();

        switch (_node)
        {
            case PackageViewModel p: BuildPackage(p); break;
            case RoundViewModel r: BuildRound(r); break;
            case ThemeViewModel t: BuildTheme(t); break;
            case QuestionViewModel q: BuildQuestion(q); break;
            default:
                _host.Children.Add(new TextBlock { Text = "Выберите пакет, раунд, тему или вопрос слева.", Opacity = 0.6 });
                break;
        }
    }

    private void Add(Control c) => _host.Children.Add(c);

    // ---------- Package ----------
    private void BuildPackage(PackageViewModel p)
    {
        Add(Ui.Title("Пакет"));
        Add(Ui.Caption("Название")); Add(Ui.Text(p.Model, nameof(Package.Name)));
        Add(Ui.Caption("Издатель")); Add(Ui.Text(p.Model, nameof(Package.Publisher)));
        Add(Ui.Caption("Дата")); Add(Ui.Text(p.Model, nameof(Package.Date)));
        Add(Ui.Caption("Сложность (1–10)")); Add(Ui.Text(p.Model, nameof(Package.Difficulty)));
        Add(Ui.Caption("Ограничения (возраст и т.п.)")); Add(Ui.Text(p.Model, nameof(Package.Restriction)));
        Add(Ui.Caption("Язык")); Add(Ui.Text(p.Model, nameof(Package.Language)));
        Add(Ui.Caption("Контакт (URL)")); Add(Ui.Text(p.Model, nameof(Package.ContactUri)));

        Add(Ui.Section("Теги"));
        Watch(p.Tags);
        Add(StringList(p.Tags, "тег", p.Tags.AddItem, p.Tags.RemoveItem));

        Add(Ui.Section("Структура"));
        Add(Ui.Wrap(Ui.Cmd("＋ Раунд", p.AddRound)));
        BuildInfo(p.Info, p);
    }

    // ---------- Round ----------
    private static readonly (string, string)[] RoundTypes =
    {
        (RoundTypes_Standart, "Обычный"), (RoundTypes_Final, "Финал")
    };

    private const string RoundTypes_Standart = "standart";
    private const string RoundTypes_Final = "final";

    private void BuildRound(RoundViewModel r)
    {
        Add(Ui.Title("Раунд"));
        Add(Ui.Caption("Название")); Add(Ui.Text(r.Model, nameof(Round.Name)));
        Add(Ui.Caption("Тип раунда"));
        Add(Ui.Combo(RoundTypes, r.Model.Type, v => r.Model.Type = v));
        Add(Ui.Section("Структура"));
        Add(Ui.Wrap(Ui.Cmd("＋ Тема", r.AddTheme), Ui.Cmd("⧉ Клонировать раунд", r.Clone), Ui.Cmd("🗑 Удалить раунд", r.Remove)));
        BuildInfo(r.Info, r);
    }

    // ---------- Theme ----------
    private void BuildTheme(ThemeViewModel t)
    {
        Add(Ui.Title("Тема"));
        Add(Ui.Caption("Название")); Add(Ui.Text(t.Model, "Name"));
        Add(Ui.Section("Вопросы"));
        Add(Ui.Wrap(
            Ui.Cmd("↕ Сортировать по цене", t.SortQuestions),
            Ui.Cmd("🔀 Перемешать", t.ShuffleQuestions),
            Ui.Cmd("⧉ Клонировать тему", t.Clone),
            Ui.Cmd("🗑 Удалить тему", t.Remove)));
        Add(new TextBlock { Text = $"Вопросов: {t.Questions.Count}, с текстом: {t.QuestionsWithTextCount}, с ответом: {t.QuestionsWithAnswerCount}", Opacity = 0.65 });
        BuildInfo(t.Info, t);
    }

    // ---------- Info (authors, sources, comments) ----------
    private void BuildInfo(InfoViewModel info, IItemViewModel owner)
    {
        Add(Ui.Section("Информация"));
        Add(Ui.Caption("Авторы"));
        Watch(info.Authors);
        Add(StringList(info.Authors, "автор", info.Authors.AddItem, info.Authors.RemoveItem));
        Add(Ui.Caption("Источники"));
        Watch(info.Sources);
        Add(StringList(info.Sources, "источник", info.Sources.AddItem, info.Sources.RemoveItem));
        Add(Ui.Caption("Комментарии")); Add(Ui.Text(info.Comments, nameof(CommentsViewModel.Text), multiline: true));
        Add(Ui.Caption("Комментарии ведущему")); Add(Ui.Text(info.ShowmanComments, nameof(ShowmanCommentsViewModel.Text), multiline: true));
    }

    private Control StringList(IList<string> list, string noun, System.Windows.Input.ICommand add, System.Windows.Input.ICommand remove)
    {
        var panel = new StackPanel { Spacing = 4 };
        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var row = new DockPanel();
            var del = Ui.Cmd("✕", remove, item, "Удалить");
            DockPanel.SetDock(del, Dock.Right);
            row.Children.Add(del);
            row.Children.Add(Ui.ListText(list, i));
            panel.Children.Add(row);
        }

        panel.Children.Add(Ui.Wrap(Ui.Cmd($"＋ {noun}", add, "")));
        return panel;
    }

    // ---------- Question ----------
    private static readonly (string, string)[] QuestionTypes =
    {
        ("", "Обычный (по умолчанию)"),
        ("simple", "Простой"),
        ("stake", "Аукцион"),
        ("secret", "Вопрос с секретом (кот в мешке)"),
        ("secretPublicPrice", "Секрет с открытой ценой"),
        ("secretNoQuestion", "Секрет без вопроса"),
        ("noRisk", "Без риска"),
        ("forAll", "Вопрос для всех"),
        ("stakeAll", "Ставки для всех"),
        ("?", "Другой")
    };

    private static readonly (string, string)[] AnswerTypes =
    {
        ("text", "Текст"), ("number", "Число"), ("point", "Точка на изображении"), ("select", "Выбор из вариантов")
    };

    private void BuildQuestion(QuestionViewModel q)
    {
        Add(Ui.Title("Вопрос"));
        Add(Ui.Wrap(Ui.Cmd("⧉ Клонировать", q.Clone), Ui.Cmd("🗑 Удалить", q.Remove)));

        Add(Ui.Caption("Цена")); Add(Ui.Text(q.Model, nameof(Question.Price)));

        Add(Ui.Caption("Тип вопроса"));
        var typeName = q.TypeName;
        Add(Ui.Combo(QuestionTypes, typeName, v => q.SetQuestionType.Execute(v)));
        WatchProperty(q, nameof(QuestionViewModel.TypeName));

        Add(Ui.Caption("Тип ответа"));
        Add(Ui.Combo(AnswerTypes, q.Parameters.AnswerType ?? "text", v => q.SetAnswerType.Execute(v)));

        // Parameters (question content, answer content, special-type parameters).
        Watch(q.Parameters);
        foreach (var record in q.Parameters.ToList())
        {
            BuildParameter(q, record.Key, record.Value, 0);
        }

        // Right / wrong answers
        Add(Ui.Section("Правильные ответы"));
        Watch(q.Right);
        Add(StringList(q.Right, "правильный ответ", q.Right.AddItem, q.Right.RemoveItem));

        Add(Ui.Section("Неправильные ответы"));
        Watch(q.Wrong);
        Add(StringList(q.Wrong, "неправильный ответ", q.Wrong.AddItem, q.Wrong.RemoveItem));

        Add(Ui.Wrap(
            Ui.Cmd("＋ Развёрнутый ответ (медиа)", q.AddComplexAnswer),
            Ui.Cmd("－ Убрать развёрнутый ответ", q.RemoveComplexAnswer)));

        BuildInfo(q.Info, q);
    }

    private static string ParamTitle(string key) => key switch
    {
        QuestionParameterNames.Question => "Содержимое вопроса",
        QuestionParameterNames.Answer => "Содержимое ответа",
        QuestionParameterNames.Theme => "Тема",
        QuestionParameterNames.Price => "Цена",
        QuestionParameterNames.SelectionMode => "Кто может быть выбран",
        QuestionParameterNames.AnswerType => "Тип ответа",
        QuestionParameterNames.AnswerOptions => "Варианты ответа",
        QuestionParameterNames.AnswerDeviation => "Допустимое отклонение",
        _ => key
    };

    private void BuildParameter(QuestionViewModel q, string key, StepParameterViewModel param, int depth)
    {
        if (key == QuestionParameterNames.AnswerType)
        {
            return; // edited via the answer type selector
        }

        var title = ParamTitle(key);

        if (param.ContentValue != null)
        {
            Add(Ui.Section(title));
            BuildContent(param.ContentValue);
        }
        else if (param.NumberSetValue != null)
        {
            Add(Ui.Caption(title));
            var ns = param.NumberSetValue;
            Add(Ui.Wrap(
                Ui.Row(new TextBlock { Text = "от", VerticalAlignment = VerticalAlignment.Center }, Ui.Text(ns, nameof(ns.Minimum))),
                Ui.Row(new TextBlock { Text = "до", VerticalAlignment = VerticalAlignment.Center }, Ui.Text(ns, nameof(ns.Maximum))),
                Ui.Row(new TextBlock { Text = "шаг", VerticalAlignment = VerticalAlignment.Center }, Ui.Text(ns, nameof(ns.Step)))));
        }
        else if (param.GroupValue != null)
        {
            Add(Ui.Section(title));
            Watch(param.GroupValue);
            foreach (var inner in param.GroupValue.ToList())
            {
                BuildParameter(q, inner.Key, inner.Value, depth + 1);
            }

            Add(Ui.Wrap(Ui.Cmd("＋ Добавить", param.GroupValue.AddItem, null)));
        }
        else
        {
            Add(Ui.Caption(title));
            Add(Ui.Text(param.Model, nameof(StepParameter.SimpleValue)));
        }
    }

    // ---------- Content items (text / image / audio / video) ----------
    private void BuildContent(ContentItemsViewModel items)
    {
        Watch(items);
        var panel = new StackPanel { Spacing = 4 };

        foreach (var item in items.ToList())
        {
            panel.Children.Add(ContentRow(items, item));
        }

        panel.Children.Add(Ui.Wrap(
            Ui.Cmd("＋ Текст", items.AddText),
            Ui.Cmd("＋ Реплика ведущего", items.AddVoice),
            Ui.Cmd("🖼 Картинка", items.AddFile, "image"),
            Ui.Cmd("🎵 Аудио", items.AddFile, "audio"),
            Ui.Cmd("🎬 Видео", items.AddFile, "video"),
            Ui.Cmd("🌐 HTML", items.AddFile, "html"),
            Ui.Cmd("🔗 Ссылка", items.LinkUri, "image")));
        _host.Children.Add(panel);
    }

    private Control ContentRow(ContentItemsViewModel items, ContentItemViewModel item)
    {
        var model = item.Model;
        var body = new StackPanel { Spacing = 4 };

        var placement = model.Placement == ContentPlacements.Replic ? " (реплика ведущего)"
            : model.Placement == ContentPlacements.Background ? " (фон)" : "";

        var head = new TextBlock { Text = TypeTitle(model.Type) + placement, Opacity = 0.65 };
        body.Children.Add(head);

        if (model.Type == ContentTypes.Text)
        {
            body.Children.Add(Ui.Text(model, nameof(ContentItem.Value), multiline: true));
        }
        else
        {
            body.Children.Add(new TextBlock { Text = model.IsRef ? model.Value : "↗ " + model.Value, TextWrapping = TextWrapping.Wrap });

            if (model.Type == ContentTypes.Image)
            {
                var image = new Image { MaxHeight = 240, MaxWidth = 420, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left };
                body.Children.Add(image);
                _ = LoadImageAsync(item, image);
            }
            else
            {
                body.Children.Add(Ui.Btn("▶ Открыть во внешнем плеере", () => _ = OpenExternallyAsync(item)));
            }
        }

        var tools = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
        tools.Children.Add(Ui.Btn("↑", () => { items.SetCurrentItem(item); items.MoveLeft.Execute(null); }, "Выше"));
        tools.Children.Add(Ui.Btn("↓", () => { items.SetCurrentItem(item); items.MoveRight.Execute(null); }, "Ниже"));
        if (model.Type == ContentTypes.Text)
        {
            tools.Children.Add(Ui.Btn("🗣", () => { items.SetCurrentItem(item); items.ChangePlacement.Execute(null); }, "Реплика ведущего / на экране"));
        }

        tools.Children.Add(Ui.Btn("✕", () => items.RemoveItem.Execute(item), "Удалить"));

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        grid.Children.Add(body);
        Grid.SetColumn(tools, 1);
        tools.Margin = new Thickness(6, 0, 0, 0);
        grid.Children.Add(tools);
        return Ui.Card(grid);
    }

    private static string TypeTitle(string type) => type switch
    {
        ContentTypes.Text => "Текст",
        ContentTypes.Image => "Картинка",
        ContentTypes.Audio => "Аудио",
        ContentTypes.Video => "Видео",
        ContentTypes.Html => "HTML",
        _ => type
    };

    private static async Task LoadImageAsync(ContentItemViewModel item, Image image)
    {
        try
        {
            var media = await item.LoadMediaAsync();
            var info = media?.GetStream?.Invoke();
            if (info?.Stream == null) return;

            var ms = new MemoryStream();
            await info.Stream.CopyToAsync(ms);
            info.Stream.Dispose();
            ms.Position = 0;
            var bmp = new Bitmap(ms);
            Dispatcher.UIThread.Post(() => image.Source = bmp);
        }
        catch
        {
            // Unsupported or missing image: keep the text description only.
        }
    }

    private static async Task OpenExternallyAsync(ContentItemViewModel item)
    {
        try
        {
            var media = await item.LoadMediaAsync();
            var info = media?.GetStream?.Invoke();
            if (info?.Stream == null) return;

            var path = Path.Combine(Path.GetTempPath(), "siquester-" + Path.GetFileName(item.Model.Value));
            using (var file = File.Create(path))
            {
                await info.Stream.CopyToAsync(file);
            }

            info.Stream.Dispose();
            Process.Start(new ProcessStartInfo("xdg-open", new[] { path }) { UseShellExecute = false });
        }
        catch
        {
        }
    }
}
