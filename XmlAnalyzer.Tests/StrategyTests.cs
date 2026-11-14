using System.Xml;
using static XmlAnalyzer.Tests.Helpers;

namespace XmlAnalyzer.Tests;

public class StrategyTests
{
    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void EmptyCriteria_ReturnsAllConferencesInDocumentOrder(string strategy)
    {
        var result = ByName(strategy).Search(SamplePath, Criteria());

        Assert.Equal(12, result.Count);
        Assert.Equal("Сучасні інформаційні технології", result[0].Title);
        Assert.Equal("Цифрові гуманітарні науки", result[^1].Title);
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void ReadsAttributesDatesAndOptionalDeadline(string strategy)
    {
        var result = ByName(strategy).Search(SamplePath, Criteria());
        var first = result[0];
        var third = result[2];

        Assert.Equal(7, first.Attributes.Count);
        Assert.Equal("800", first.GetAttribute("fee"));
        Assert.Equal(new DateOnly(2026, 3, 12), first.Dates!.Start);
        Assert.Equal(new DateOnly(2026, 3, 14), first.Dates.End);
        Assert.Equal(new DateOnly(2026, 2, 1), first.SubmissionDeadline);

        Assert.Null(third.SubmissionDeadline);
        Assert.Equal(4, result[3].Attributes.Count);
        Assert.Null(result[3].GetAttribute("fee"));
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void FiltersByOneAttribute(string strategy)
    {
        var result = ByName(strategy).Search(SamplePath, Criteria(null, ("city", "Київ")));
        Assert.Equal(5, result.Count);
        Assert.All(result, c => Assert.Equal("Київ", c.GetAttribute("city")));
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void FiltersBySeveralAttributes(string strategy)
    {
        var result = ByName(strategy).Search(SamplePath, Criteria(null, ("city", "Київ"), ("topic", "Математика")));
        Assert.Equal(["Прикладна математика і моделювання", "Теорія ймовірностей і статистика"], result.Select(c => c.Title));
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void FilterOnOptionalAttribute_SkipsNodesWithoutIt(string strategy)
    {
        var result = ByName(strategy).Search(SamplePath, Criteria(null, ("format", "онлайн")));
        Assert.Equal(3, result.Count);
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void Keyword_IsCaseInsensitiveForCyrillic(string strategy)
    {
        var result = ByName(strategy).Search(SamplePath, Criteria("ОБЧИСЛ"));
        Assert.Equal(["Квантові обчислення: теорія і практика", "Обчислювальна лінгвістика", "Розподілені системи та хмарні обчислення"],
                     result.Select(c => c.Title));
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void KeywordAndFilter_AreCombinedWithAnd(string strategy)
    {
        var result = ByName(strategy).Search(SamplePath, Criteria("обчисл", ("city", "Харків")));
        Assert.Equal(2, result.Count);
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void NothingFound_ReturnsEmptyList(string strategy)
    {
        Assert.Empty(ByName(strategy).Search(SamplePath, Criteria(null, ("city", "Париж"))));
        Assert.Empty(ByName(strategy).Search(SamplePath, Criteria("немає такого слова")));
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void ValueWithBothQuoteKinds_IsFoundSafely(string strategy)
    {
        using var file = new TempFile(".xml", """
            <Conferences>
              <Conference title="It's &quot;AI&quot;" city="Київ"><Dates start="2026-01-01" end="2026-01-02"/></Conference>
              <Conference title="Інша" city="Київ"><Dates start="2026-01-01" end="2026-01-02"/></Conference>
            </Conferences>
            """);
        var result = ByName(strategy).Search(file.Path, Criteria(null, ("title", "It's \"AI\"")));
        Assert.Single(result);
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void BrokenXml_ThrowsXmlException(string strategy)
    {
        using var file = new TempFile(".xml", "<Conferences><Conference title='x'></Conferences>");
        Assert.ThrowsAny<XmlException>(() => ByName(strategy).Search(file.Path, Criteria()));
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void WrongRootElement_ThrowsInvalidData(string strategy)
    {
        using var file = new TempFile(".xml", "<Books><Book title='x'/></Books>");
        Assert.Throws<InvalidDataException>(() => ByName(strategy).Search(file.Path, Criteria()));
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void BadDate_ThrowsFormatException(string strategy)
    {
        using var file = new TempFile(".xml", "<Conferences><Conference title='x'><Dates start='12.03.2026' end='2026-03-14'/></Conference></Conferences>");
        Assert.Throws<FormatException>(() => ByName(strategy).Search(file.Path, Criteria()));
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void DtdIsRejected(string strategy)
    {
        using var file = new TempFile(".xml", "<!DOCTYPE Conferences [<!ENTITY x 'y'>]><Conferences/>");
        Assert.ThrowsAny<XmlException>(() => ByName(strategy).Search(file.Path, Criteria()));
    }

    [Fact]
    public void AllStrategies_GiveIdenticalResults_ForEveryFilterAndKeyword()
    {
        var catalog = new Services.AttributeCatalog().Load(SamplePath);
        var cases = new List<Model.SearchCriteria> { Criteria(), Criteria("ко"), Criteria("ФАКУЛЬТЕТ") };
        foreach (var (name, values) in catalog)
            foreach (var v in values)
            {
                cases.Add(Criteria(null, (name, v)));
                cases.Add(Criteria("ін", (name, v)));
            }

        foreach (var c in cases)
        {
            var expected = Signatures(new Search.SaxSearchStrategy().Search(SamplePath, c));
            Assert.Equal(expected, Signatures(new Search.DomSearchStrategy().Search(SamplePath, c)));
            Assert.Equal(expected, Signatures(new Search.LinqSearchStrategy().Search(SamplePath, c)));
        }
    }

    [Fact]
    public void DomXPath_IsGeneratedFromFilters()
    {
        var xpath = Search.DomSearchStrategy.BuildXPath(Criteria("ignored", ("city", "Київ"), ("format", "онлайн")));
        Assert.Equal("/Conferences/Conference[@city='Київ'][@format='онлайн']", xpath);
        Assert.Equal("concat('It', \"'\", 's \"x\"')", Search.DomSearchStrategy.Literal("It's \"x\""));
    }
}
