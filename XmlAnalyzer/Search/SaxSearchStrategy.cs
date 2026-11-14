using System.Xml;
using XmlAnalyzer.Model;

namespace XmlAnalyzer.Search;

public sealed class SaxSearchStrategy : IXmlSearchStrategy
{
    public string Name => "SAX";

    public List<Conference> Search(string path, SearchCriteria criteria)
    {
        var result = new List<Conference>();
        using var reader = XmlReader.Create(path, XmlFormat.ReaderSettings);

        reader.MoveToContent();
        if (reader.LocalName != XmlNames.Root) throw XmlFormat.WrongRoot(reader.LocalName);

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != XmlNames.Conference) continue;
            var conference = ReadConference(reader);
            if (criteria.Matches(conference)) result.Add(conference);
        }
        return result;
    }

    private static Conference ReadConference(XmlReader reader)
    {
        var c = new Conference();
        bool isEmpty = reader.IsEmptyElement;

        while (reader.MoveToNextAttribute())
            c.Attributes[reader.Name] = reader.Value;
        reader.MoveToElement();
        if (isEmpty) return c;

        int depth = reader.Depth;
        string? deadlineText = null;

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.EndElement when reader.Depth == depth:
                    return c;

                case XmlNodeType.Element when reader.Depth == depth + 1 && reader.LocalName == XmlNames.Dates:
                    c.Dates = ConferenceDates.Parse(reader.GetAttribute(XmlNames.Start), reader.GetAttribute(XmlNames.End));
                    break;

                case XmlNodeType.Element when reader.Depth == depth + 1 && reader.LocalName == XmlNames.SubmissionDeadline:
                    if (reader.IsEmptyElement)
                        c.SubmissionDeadline = ConferenceDates.ParseDate("", XmlNames.SubmissionDeadline);
                    else
                        deadlineText = "";
                    break;

                case XmlNodeType.Text or XmlNodeType.CDATA when deadlineText is not null:
                    deadlineText += reader.Value;
                    break;

                case XmlNodeType.EndElement when deadlineText is not null && reader.Depth == depth + 1:
                    c.SubmissionDeadline = ConferenceDates.ParseDate(deadlineText, XmlNames.SubmissionDeadline);
                    deadlineText = null;
                    break;
            }
        }
        throw new XmlException("Неочікуваний кінець файлу всередині <Conference>.");
    }
}
