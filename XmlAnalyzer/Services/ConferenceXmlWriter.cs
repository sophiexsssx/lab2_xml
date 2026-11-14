using System.Text;
using System.Xml;
using System.Xml.Linq;
using XmlAnalyzer.Model;

namespace XmlAnalyzer.Services;

public sealed class ConferenceXmlWriter
{
    public void Write(List<Conference> items, string path)
    {
        var root = new XElement(XmlNames.Root,
            items.Select(c => new XElement(XmlNames.Conference,
                c.Attributes.Select(a => new XAttribute(a.Key, a.Value)),
                c.Dates is null ? null : new XElement(XmlNames.Dates,
                    new XAttribute(XmlNames.Start, ConferenceDates.Format(c.Dates.Start)),
                    new XAttribute(XmlNames.End, ConferenceDates.Format(c.Dates.End))),
                c.SubmissionDeadline is { } d ? new XElement(XmlNames.SubmissionDeadline, ConferenceDates.Format(d)) : null)));

        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using var writer = XmlWriter.Create(path, settings);
        new XDocument(new XDeclaration("1.0", "utf-8", null), root).Save(writer);
    }
}
