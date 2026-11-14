using System.Globalization;

namespace XmlAnalyzer.Model;

public sealed record ConferenceDates(DateOnly Start, DateOnly End)
{
    public const string DateFormat = "yyyy-MM-dd";

    public static ConferenceDates Parse(string? start, string? end) =>
        new(ParseDate(start, "start"), ParseDate(end, "end"));

    public static DateOnly ParseDate(string? text, string what)
    {
        if (DateOnly.TryParseExact(text?.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        throw new FormatException($"Некоректна дата у {what}: «{text}». Очікується формат {DateFormat}.");
    }

    public static string Format(DateOnly d) => d.ToString(DateFormat, CultureInfo.InvariantCulture);

    public override string ToString() => $"{Start:dd.MM.yyyy} – {End:dd.MM.yyyy}";
}
