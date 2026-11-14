using System.Xml;
using System.Xml.Linq;
using XmlAnalyzer.Model;

namespace XmlAnalyzer.Search;

public sealed class LinqSearchStrategy : IXmlSearchStrategy
{
    public string Name => "LINQ to XML";

    public List<Conference> Search(string path, SearchCriteria criteria)
    {
        XDocument doc;
        using (var reader = XmlReader.Create(path, XmlFormat.ReaderSettings))
            doc = XDocument.Load(reader);

        if (doc.Root?.Name.LocalName != XmlNames.Root) throw XmlFormat.WrongRoot(doc.Root?.Name.LocalName);

        IEnumerable<XElement> query = doc.Root.Elements(XmlNames.Conference);
        foreach (var (name, value) in criteria.Filters)
        {
            string attr = name, expected = value;
            query = query.Where(e => (string?)e.Attribute(attr) == expected);
        }

        return query
            .Select(ToConference)
            .Where(criteria.Matches)
            .ToList();
    }

    private static Conference ToConference(XElement e)
    {
        var c = new Conference();
        foreach (var a in e.Attributes())
            c.Attributes[a.Name.LocalName] = a.Value;

        if (e.Element(XmlNames.Dates) is { } dates)
            c.Dates = ConferenceDates.Parse((string?)dates.Attribute(XmlNames.Start), (string?)dates.Attribute(XmlNames.End));
        if (e.Element(XmlNames.SubmissionDeadline) is { } deadline)
            c.SubmissionDeadline = ConferenceDates.ParseDate(deadline.Value, XmlNames.SubmissionDeadline);
        return c;
    }
}
