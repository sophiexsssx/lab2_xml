using System.Xml;
using System.Xml.Linq;
using XmlAnalyzer.Model;
using XmlAnalyzer.Search;

namespace XmlAnalyzer.Services;

public sealed class AttributeCatalog
{
    public Dictionary<string, List<string>> Load(string path)
    {
        XDocument doc;
        using (var reader = XmlReader.Create(path, XmlFormat.ReaderSettings))
            doc = XDocument.Load(reader);

        if (doc.Root?.Name.LocalName != XmlNames.Root) throw XmlFormat.WrongRoot(doc.Root?.Name.LocalName);

        var culture = System.Globalization.CultureInfo.GetCultureInfo("uk-UA");
        var comparer = StringComparer.Create(culture, ignoreCase: true);

        var result = new Dictionary<string, List<string>>();
        foreach (var a in doc.Root.Elements(XmlNames.Conference).Attributes())
        {
            if (!result.TryGetValue(a.Name.LocalName, out var values))
                result[a.Name.LocalName] = values = [];
            if (!values.Contains(a.Value)) values.Add(a.Value);
        }
        foreach (var values in result.Values) values.Sort(comparer);
        return result;
    }
}
