using ContactInfo.Models;

namespace ContactInfo.Services.Interfaces;

public interface IContactSource
{
    string Name { get; }
    Task<SourceResult> LookupAsync(string linkedInUrl);
}
