namespace ContactInfo.Models;

public class SourceResult
{
    public string SourceName { get; set; } = "";
    public string? Name     { get; set; }
    public string? Title    { get; set; }
    public string? Company  { get; set; }
    public List<string> Emails { get; set; } = new();
    public List<string> Phones { get; set; } = new();
    public bool    Success { get; set; }
    public string? Error   { get; set; }
}
