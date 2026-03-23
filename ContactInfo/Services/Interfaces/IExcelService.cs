using ContactInfo.Models;

namespace ContactInfo.Services.Interfaces;

public interface IExcelService
{
    List<string> ReadLinkedInUrls(Stream stream);
    byte[] ExportBatch(BatchJob job);
    byte[] GenerateSampleImport();
}
