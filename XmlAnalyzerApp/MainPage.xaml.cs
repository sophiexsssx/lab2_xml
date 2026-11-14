using System.Xml;
using XmlAnalyzer;
using XmlAnalyzerApp.Services;

namespace XmlAnalyzerApp;

public partial class MainPage : ContentPage
{
    private const string AnyValue = "(будь-яке)";
    private readonly MainViewModel _viewModel;
    private bool _rebuilding;

    public static bool ExitConfirmed { get; private set; }

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BuildStrategyList();
    }

    private async void OnOpenXmlClicked(object? sender, EventArgs e)
    {
        try
        {
            var file = await FileDialogs.PickXmlAsync();
            if (file is not null) await LoadFileAsync(file.FullPath, file.FileName);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Не вдалося відкрити файл", ex);
        }
    }

    private async void OnOpenSampleClicked(object? sender, EventArgs e)
    {
        try
        {
            await LoadFileAsync(await FileDialogs.ExtractAssetAsync("conferences.xml"), "conferences.xml (приклад)");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Не вдалося відкрити приклад", ex);
        }
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        if (_viewModel.XmlPath is null)
        {
            await DisplayAlertAsync("Файл не відкрито",
                "Спершу відкрийте XML-файл кнопкою «Відкрити XML…» або «Приклад». Пошук іде по конференціях з відкритого файлу.", "OK");
            return;
        }
        try
        {
            _viewModel.Search();
            ShowResults();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Помилка аналізу файлу", ex);
        }
    }

    private void OnClearClicked(object? sender, EventArgs e)
    {
        _viewModel.Clear();
        ShowParameters();
        ShowResults();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            string temp = FileDialogs.NewTempPath(".xml");
            _viewModel.SaveResults(temp);
            string? saved = await FileDialogs.SaveCopyAsync(temp, "conferences-filtered.xml");
            if (saved is not null) StatusLabel.Text = $"Результати збережено: {saved}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Не вдалося зберегти XML", ex);
        }
    }

    private async void OnTransformClicked(object? sender, EventArgs e)
    {
        try
        {
            string? xslPath = await ChooseXslAsync();
            if (xslPath is null) return;

            string temp = FileDialogs.NewTempPath(".html");
            _viewModel.TransformToHtml(xslPath, temp);
            string? saved = await FileDialogs.SaveCopyAsync(temp, "conferences.html", keepSource: true);
            if (saved is null) return;

            StatusLabel.Text = $"HTML збережено: {saved}";
            if (await DisplayAlertAsync("HTML збережено", saved, "Відкрити", "Закрити"))
                await OpenInDefaultAppAsync(temp);
        }
        catch (XmlException ex)
        {
            await ShowErrorAsync("XSL-файл некоректний", ex);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Не вдалося трансформувати в HTML", ex);
        }
    }

    private async void OnAboutClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("Про програму",
            "Аналіз XML: конференції — лабораторна робота № 2.\n\n" +
            "Програма шукає наукові конференції в XML-файлі трьома способами: " +
            "SAX (послідовне читання XmlReader), DOM (XmlDocument і запит XPath) та LINQ to XML. " +
            "Спосіб обирається перемикачем, а реалізований він шаблоном «Стратегія».\n\n" +
            "Як користуватися:\n" +
            "1. Відкрийте XML-файл або вбудований приклад.\n" +
            "2. Оберіть спосіб аналізу, введіть ключове слово та/або значення атрибутів. " +
            "Значення беруться з відкритого файлу.\n" +
            "3. Натисніть «Шукати». Без критеріїв буде показано всі конференції.\n" +
            "4. Результати можна зберегти у XML або трансформувати в HTML за XSL-файлом.\n" +
            "Clear очищає результати і параметри пошуку.",
            "OK");

    private async void OnExitClicked(object? sender, EventArgs e) => await RequestExitAsync();

    public async Task RequestExitAsync()
    {
        if (!await ConfirmExitAsync()) return;
        ExitConfirmed = true;
        Application.Current?.Quit();
    }

    private Task<bool> ConfirmExitAsync() =>
        DisplayAlertAsync("Вихід", "Чи дійсно ви хочете завершити роботу з програмою?", "Так", "Ні");

    private async Task LoadFileAsync(string path, string displayName)
    {
        try
        {
            _viewModel.LoadFile(path);
        }
        catch (XmlException ex)
        {
            await DisplayAlertAsync("Некоректний XML-файл", $"Рядок {ex.LineNumber}, позиція {ex.LinePosition}: {ex.Message}", "OK");
            return;
        }
        FileLabel.Text = displayName;
        ShowParameters();
        ShowResults();
    }

    private void BuildStrategyList()
    {
        foreach (var strategy in _viewModel.Strategies)
        {
            var radio = new RadioButton
            {
                Content = strategy.Name,
                GroupName = "Strategy",
                IsChecked = strategy == _viewModel.SelectedStrategy,
                TextColor = Color.FromArgb("#3B1F2B"),
            };
            radio.CheckedChanged += (_, args) =>
            {
                if (args.Value) _viewModel.SelectedStrategy = strategy;
            };
            StrategyList.Add(radio);
        }
    }

    private void ShowParameters()
    {
        _rebuilding = true;
        KeywordEntry.Text = _viewModel.Keyword;
        FilterList.Clear();
        foreach (var (attribute, values) in _viewModel.AttributeValues)
        {
            var picker = new Picker { Title = AttributeLabels.For(attribute), TextColor = Color.FromArgb("#3B1F2B") };
            picker.Items.Add(AnyValue);
            foreach (var v in values) picker.Items.Add(v);
            picker.SelectedIndex = _viewModel.SelectedFilters.TryGetValue(attribute, out var selected)
                ? picker.Items.IndexOf(selected)
                : 0;
            picker.SelectedIndexChanged += (_, _) =>
            {
                if (_rebuilding) return;
                if (picker.SelectedIndex <= 0) _viewModel.SelectedFilters.Remove(attribute);
                else _viewModel.SelectedFilters[attribute] = picker.Items[picker.SelectedIndex];
            };

            FilterList.Add(new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    new Label { Text = $"{AttributeLabels.For(attribute)}  ({values.Count})", FontSize = 13, TextColor = Color.FromArgb("#8A6878") },
                    picker,
                },
            });
        }
        _rebuilding = false;
    }

    private void OnKeywordChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_rebuilding) _viewModel.Keyword = e.NewTextValue ?? "";
    }

    private void ShowResults()
    {
        ResultsView.ItemsSource = _viewModel.Results.Select(ResultRow.From).ToList();
        SaveButton.IsEnabled = TransformButton.IsEnabled = _viewModel.HasSearched;

        if (!_viewModel.HasSearched)
        {
            EmptyLabel.Text = "Тут з'являться результати пошуку.";
            StatusLabel.Text = _viewModel.XmlPath is null
                ? "Відкрийте XML-файл («Відкрити XML…» або «Приклад»), щоб почати."
                : "Оберіть спосіб аналізу, задайте критерії й натисніть «Шукати». Без критеріїв буде показано всі конференції.";
            return;
        }

        EmptyLabel.Text = "Нічого не знайдено.";
        StatusLabel.Text = $"Знайдено: {_viewModel.Results.Count}  ·  спосіб: {_viewModel.SelectedStrategy.Name}";
    }

    private async Task<string?> ChooseXslAsync()
    {
        const string builtIn = "Вбудований conferences.xsl";
        const string pick = "Обрати файл…";
        string? choice = await DisplayActionSheetAsync("XSL-документ для трансформації", "Скасувати", null, builtIn, pick);
        return choice switch
        {
            builtIn => await FileDialogs.ExtractAssetAsync("conferences.xsl"),
            pick => await FileDialogs.PickXslAsync(),
            _ => null,
        };
    }

    private static async Task OpenInDefaultAppAsync(string path)
    {
#if MACCATALYST
        await Task.CompletedTask;
        if (!MacFileOpener.Open(path))
            throw new IOException($"Не вдалося відкрити {path}. Відкрийте файл через Finder.");
#else
        await Launcher.Default.OpenAsync(new OpenFileRequest("Конференції", new ReadOnlyFile(path)));
#endif
    }

    private Task ShowErrorAsync(string title, Exception ex) => DisplayAlertAsync(title, ex.Message, "OK");
}
