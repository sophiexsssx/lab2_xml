using XmlAnalyzer.Model;
using XmlAnalyzer.Search;

namespace XmlAnalyzer.Tests;

internal static class Helpers
{
    public static string SamplePath => Path.Combine(AppContext.BaseDirectory, "Samples", "conferences.xml");
    public static string XslPath => Path.Combine(AppContext.BaseDirectory, "Samples", "conferences.xsl");

    public static IXmlSearchStrategy[] All => [new SaxSearchStrategy(), new DomSearchStrategy(), new LinqSearchStrategy()];

    public static TheoryData<string> StrategyNames => new() { "SAX", "DOM", "LINQ to XML" };

    public static IXmlSearchStrategy ByName(string name) => All.Single(s => s.Name == name);

    public static SearchCriteria Criteria(string? keyword = null, params (string Name, string Value)[] filters) =>
        new(keyword, filters.ToDictionary(f => f.Name, f => f.Value));

    public static string Signature(Conference c) =>
        string.Join("|", c.Attributes.OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key}={a.Value}"))
        + $"|dates={c.Dates}|deadline={c.SubmissionDeadline}";

    public static List<string> Signatures(IEnumerable<Conference> items) => items.Select(Signature).ToList();

    public sealed class TempFile : IDisposable
    {
        public TempFile(string extension = ".xml", string? content = null)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"lab2-test-{Guid.NewGuid():N}{extension}");
            if (content != null) File.WriteAllText(Path, content);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path)) File.Delete(Path);
        }
    }
}
