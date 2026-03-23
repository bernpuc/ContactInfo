namespace ContactInfo.Models;

public class BatchJob
{
    public List<BatchRow> Rows        { get; set; } = new();
    public List<string>   SourceNames { get; set; } = new();
    public bool           IsRunning   { get; set; }
    public int ProcessedCount => Rows.Count(r => r.Status is RowStatus.Complete or RowStatus.Failed);
}
