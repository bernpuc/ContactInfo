namespace ContactInfo.Models;

public class BatchRow
{
    public string  LinkedInUrl { get; set; } = "";
    public string? Name        { get; set; }
    public string? Title       { get; set; }
    public string? Company     { get; set; }

    public Dictionary<string, SourceResult> SourceResults { get; set; } = new();
    public List<RankedContact> RankedEmails { get; set; } = new();
    public List<RankedContact> RankedPhones { get; set; } = new();

    public RowStatus Status { get; set; } = RowStatus.Pending;
    public string?   Error  { get; set; }
}

public enum RowStatus { Pending, Processing, Complete, Failed }
