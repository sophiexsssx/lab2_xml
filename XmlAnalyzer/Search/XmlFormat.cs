using System.Xml;

namespace XmlAnalyzer.Search;

internal static class XmlFormat
{
    public static XmlReaderSettings ReaderSettings => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreWhitespace = true,
        IgnoreComments = true,
    };

    public static InvalidDataException WrongRoot(string? actual) =>
        new($"Кореневий елемент має бути <{Model.XmlNames.Root}>, а знайдено <{actual}>.");
}
