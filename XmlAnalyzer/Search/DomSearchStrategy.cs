using System.Xml;
using XmlAnalyzer.Model;

namespace XmlAnalyzer.Search;

public sealed class DomSearchStrategy : IXmlSearchStrategy
{
    public string Name => "DOM";

    public List<Conference> Search(string path, SearchCriteria criteria)
    {
        var doc = new XmlDocument { XmlResolver = null };
        using (var reader = XmlReader.Create(path, XmlFormat.ReaderSettings))
            doc.Load(reader);

        if (doc.DocumentElement?.LocalName != XmlNames.Root) throw XmlFormat.WrongRoot(doc.DocumentElement?.LocalName);

        var result = new List<Conference>();
        var nodes = doc.DocumentElement.SelectNodes(BuildXPath(criteria));
        if (nodes is null) return result;

        foreach (XmlElement element in nodes)
        {
            var conference = ToConference(element);
            if (criteria.Matches(conference)) result.Add(conference);
        }
        return result;
    }

    public static string BuildXPath(SearchCriteria criteria)
    {
        var xpath = "/" + XmlNames.Root + "/" + XmlNames.Conference;
        foreach (var (name, value) in criteria.Filters)
            xpath += $"[@{XmlConvert.VerifyName(name)}={Literal(value)}]";
        return xpath;
    }

    public static string Literal(string value)
    {
        if (!value.Contains('\'')) return $"'{value}'";
        if (!value.Contains('"')) return $"\"{value}\"";
        return "concat('" + value.Replace("'", "', \"'\", '") + "')";
    }

    private static Conference ToConference(XmlElement element)
    {
        var c = new Conference();
        foreach (XmlAttribute a in element.Attributes)
            c.Attributes[a.Name] = a.Value;

        if (element.SelectSingleNode(XmlNames.Dates) is XmlElement dates)
            c.Dates = ConferenceDates.Parse(dates.GetAttribute(XmlNames.Start), dates.GetAttribute(XmlNames.End));
        if (element.SelectSingleNode(XmlNames.SubmissionDeadline) is XmlElement deadline)
            c.SubmissionDeadline = ConferenceDates.ParseDate(deadline.InnerText, XmlNames.SubmissionDeadline);
        return c;
    }
}
