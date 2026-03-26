namespace ContactInfo.Models;

public class RankedContact
{
    public string       Value          { get; set; } = "";
    public string?      Label          { get; set; }   // e.g. "work", "personal", "mobile"
    public int          Score          { get; set; }   // number of sources that returned this value
    public List<string> FoundOnSources { get; set; } = new();
    public bool         IsMultiSource  => Score > 1;
}
