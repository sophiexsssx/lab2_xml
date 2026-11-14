using XmlAnalyzer.Model;
using XmlAnalyzer.Search;
using XmlAnalyzer.Services;

namespace XmlAnalyzer;

public sealed class MainViewModel
{
    private readonly XmlSearchContext _context;
    private readonly AttributeCatalog _catalog;
    private readonly ConferenceXmlWriter _writer;
    private readonly HtmlTransformer _transformer;

    public MainViewModel(IEnumerable<IXmlSearchStrategy> strategies, XmlSearchContext context,
                         AttributeCatalog catalog, ConferenceXmlWriter writer, HtmlTransformer transformer)
    {
        Strategies = strategies.ToList();
        if (Strategies.Count == 0) throw new ArgumentException("Потрібна хоча б одна стратегія.", nameof(strategies));
        SelectedStrategy = Strategies[0];
        _context = context;
        _catalog = catalog;
        _writer = writer;
        _transformer = transformer;
    }

    public static MainViewModel CreateDefault() => new(
        [new SaxSearchStrategy(), new DomSearchStrategy(), new LinqSearchStrategy()],
        new XmlSearchContext(), new AttributeCatalog(), new ConferenceXmlWriter(), new HtmlTransformer());

    public string? XmlPath { get; private set; }
    public IReadOnlyList<IXmlSearchStrategy> Strategies { get; }
    public IXmlSearchStrategy SelectedStrategy { get; set; }
    public Dictionary<string, List<string>> AttributeValues { get; private set; } = [];
    public string Keyword { get; set; } = "";
    public Dictionary<string, string> SelectedFilters { get; } = [];
    public List<Conference> Results { get; private set; } = [];

    public bool HasSearched { get; private set; }
    public SearchCriteria? LastCriteria { get; private set; }

    public void LoadFile(string path)
    {
        var values = _catalog.Load(path);
        XmlPath = path;
        AttributeValues = values;
        Clear();
    }

    public void Search()
    {
        if (XmlPath is null) throw new InvalidOperationException("Спершу відкрийте XML-файл.");
        var criteria = new SearchCriteria(Keyword, SelectedFilters);
        _context.SetStrategy(SelectedStrategy);
        Results = _context.Execute(XmlPath, criteria);
        LastCriteria = criteria;
        HasSearched = true;
    }

    public void Clear()
    {
        Keyword = "";
        SelectedFilters.Clear();
        Results = [];
        LastCriteria = null;
        HasSearched = false;
    }

    public void SaveResults(string path)
    {
        EnsureSearched();
        _writer.Write(Results, path);
    }

    public void TransformToHtml(string xslPath, string htmlPath)
    {
        EnsureSearched();
        string tempXmlPath = Path.Combine(Path.GetTempPath(), $"conferences-{Guid.NewGuid():N}.xml");
        try
        {
            _writer.Write(Results, tempXmlPath);
            _transformer.Transform(tempXmlPath, xslPath, htmlPath);
        }
        finally
        {
            File.Delete(tempXmlPath);
        }
    }

    private void EnsureSearched()
    {
        if (!HasSearched) throw new InvalidOperationException("Спершу виконайте пошук.");
    }
}
