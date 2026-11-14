using XmlAnalyzer.Model;

namespace XmlAnalyzer.Search;

public sealed class XmlSearchContext
{
    private IXmlSearchStrategy? _strategy;

    public void SetStrategy(IXmlSearchStrategy s) => _strategy = s ?? throw new ArgumentNullException(nameof(s));

    public List<Conference> Execute(string path, SearchCriteria c)
    {
        if (_strategy is null) throw new InvalidOperationException("Спосіб аналізу не обрано.");
        return _strategy.Search(path, c);
    }
}
