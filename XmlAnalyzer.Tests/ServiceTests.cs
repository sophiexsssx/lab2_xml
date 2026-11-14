using XmlAnalyzer.Services;
using static XmlAnalyzer.Tests.Helpers;

namespace XmlAnalyzer.Tests;

public class ServiceTests
{
    [Fact]
    public void Catalog_CollectsAllAttributesAndDistinctSortedValues()
    {
        var values = new AttributeCatalog().Load(SamplePath);

        Assert.Equal(["title", "topic", "organizer", "city", "format", "fee", "language"], values.Keys);
        Assert.Equal(["Київ", "Львів", "Одеса", "Харків"], values["city"]);
        Assert.Equal(["змішано", "онлайн", "очно"], values["format"]);
        Assert.Equal(12, values["title"].Count);
    }

    [Fact]
    public void Catalog_BrokenFile_Throws()
    {
        using var file = new TempFile(".xml", "<Conferences>");
        Assert.ThrowsAny<System.Xml.XmlException>(() => new AttributeCatalog().Load(file.Path));
    }

    [Fact]
    public void Writer_RoundTrip_KeepsEveryField()
    {
        var all = new Search.LinqSearchStrategy().Search(SamplePath, Criteria());
        using var file = new TempFile();

        new ConferenceXmlWriter().Write(all, file.Path);

        foreach (var s in All)
            Assert.Equal(Signatures(all), Signatures(s.Search(file.Path, Criteria())));
    }

    [Fact]
    public void Writer_EmptyList_WritesEmptyRoot()
    {
        using var file = new TempFile();
        new ConferenceXmlWriter().Write([], file.Path);
        Assert.Empty(new Search.SaxSearchStrategy().Search(file.Path, Criteria()));
    }

    [Fact]
    public void Transformer_ProducesHtmlTable()
    {
        using var html = new TempFile(".html");
        new HtmlTransformer().Transform(SamplePath, XslPath, html.Path);
        string text = File.ReadAllText(html.Path);

        Assert.Contains("Знайдено конференцій: 12", text);
        Assert.Contains("<td>Сучасні інформаційні технології</td>", text);
        Assert.Contains("12.03.2026 – 14.03.2026", text);
        Assert.Contains("безкоштовно", text);
        Assert.Contains("—", text);
        Assert.StartsWith("<!DOCTYPE html", text, StringComparison.OrdinalIgnoreCase);
    }
}
