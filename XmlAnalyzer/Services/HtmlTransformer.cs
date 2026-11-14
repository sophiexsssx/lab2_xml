using System.Xml;
using System.Xml.Xsl;

namespace XmlAnalyzer.Services;

public sealed class HtmlTransformer
{
    public void Transform(string xmlPath, string xslPath, string htmlPath)
    {
        var xslt = new XslCompiledTransform();
        using (var xslReader = XmlReader.Create(xslPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            xslt.Load(xslReader, XsltSettings.Default, null);

        using var xmlReader = XmlReader.Create(xmlPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        using var output = File.Create(htmlPath);
        xslt.Transform(xmlReader, null, output);
    }
}
