namespace XmlAnalyzer.Model;

public sealed class Conference
{
    public Dictionary<string, string> Attributes { get; } = new(StringComparer.Ordinal);
    public ConferenceDates? Dates { get; set; }
    public DateOnly? SubmissionDeadline { get; set; }

    public string? GetAttribute(string name) => Attributes.TryGetValue(name, out var v) ? v : null;

    public string Title => GetAttribute(XmlNames.Title) ?? "(без назви)";

    public override string ToString() => Title;
}
