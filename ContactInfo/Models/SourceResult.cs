namespace ContactInfo.Models;

/// <summary>A contact value (email or phone) with an optional provider label such as "work" or "personal".</summary>
public record LabeledValue(string Value, string? Label = null);

public class SourceResult
{
    public string SourceName { get; set; } = "";
    public string? Name     { get; set; }
    public string? Title    { get; set; }
    public string? Company  { get; set; }
    public List<LabeledValue> Emails { get; set; } = new();
    public List<LabeledValue> Phones { get; set; } = new();
    public bool    Success { get; set; }
    public string? Error   { get; set; }
}
