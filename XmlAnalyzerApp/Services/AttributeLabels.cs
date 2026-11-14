using XmlAnalyzer.Model;

namespace XmlAnalyzerApp.Services;

public static class AttributeLabels
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["title"] = "Назва",
        ["topic"] = "Тематика",
        ["organizer"] = "Організатор",
        ["city"] = "Місто",
        ["format"] = "Формат",
        ["fee"] = "Внесок, грн",
        ["language"] = "Мова",
    };

    public static string For(string attribute) => Labels.GetValueOrDefault(attribute, attribute);
}

public sealed record ResultRow(string Title, string Summary, string Details)
{
    private static readonly HashSet<string> SummaryAttributes = ["title", "topic", "city"];

    public static ResultRow From(Conference c)
    {
        var summary = new List<string>();
        if (c.GetAttribute("topic") is { } topic) summary.Add(topic);
        if (c.GetAttribute("city") is { } city) summary.Add(city);
        if (c.Dates is not null) summary.Add(c.Dates.ToString());

        var details = c.Attributes
            .Where(a => !SummaryAttributes.Contains(a.Key))
            .Select(a => $"{AttributeLabels.For(a.Key)}: {(a.Key == "fee" && a.Value == "0" ? "безкоштовно" : a.Value)}")
            .ToList();
        if (c.SubmissionDeadline is { } d) details.Add($"Подання тез до {d:dd.MM.yyyy}");

        return new ResultRow(c.Title, string.Join("  ·  ", summary), string.Join("   ", details));
    }
}
