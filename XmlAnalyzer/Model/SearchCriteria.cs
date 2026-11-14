namespace XmlAnalyzer.Model;

public sealed class SearchCriteria
{
    public SearchCriteria(string? keyword, IReadOnlyDictionary<string, string>? filters)
    {
        Keyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        Filters = filters is null
            ? new Dictionary<string, string>()
            : filters.Where(f => !string.IsNullOrEmpty(f.Value)).ToDictionary(f => f.Key, f => f.Value);
    }

    public string? Keyword { get; }
    public IReadOnlyDictionary<string, string> Filters { get; }

    public bool Matches(Conference c)
    {
        foreach (var (name, value) in Filters)
            if (!string.Equals(c.GetAttribute(name), value, StringComparison.Ordinal))
                return false;

        return Keyword is null
               || c.Attributes.Values.Any(v => v.Contains(Keyword, StringComparison.OrdinalIgnoreCase));
    }

    public override string ToString()
    {
        var parts = Filters.Select(f => $"{f.Key} = «{f.Value}»").ToList();
        if (Keyword is not null) parts.Insert(0, $"ключове слово «{Keyword}»");
        return parts.Count == 0 ? "без критеріїв" : string.Join(", ", parts);
    }
}
