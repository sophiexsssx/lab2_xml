using XmlAnalyzer.Model;

namespace XmlAnalyzer.Search;

public interface IXmlSearchStrategy
{
    string Name { get; }

    List<Conference> Search(string path, SearchCriteria criteria);
}
