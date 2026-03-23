using ClosedXML.Excel;
using ContactInfo.Models;
using ContactInfo.Services.Interfaces;

namespace ContactInfo.Services;

public class ExcelService : IExcelService
{
    /// <summary>
    /// Reads LinkedIn URLs from column A of the first worksheet, skipping the header row.
    /// </summary>
    public List<string> ReadLinkedInUrls(Stream stream)
    {
        using var wb  = new XLWorkbook(stream);
        var ws        = wb.Worksheets.First();
        var lastRow   = ws.LastRowUsed()?.RowNumber() ?? 1;
        var urls      = new List<string>();

        for (int r = 2; r <= lastRow; r++)
        {
            var val = ws.Cell(r, 1).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(val))
                urls.Add(val);
        }

        return urls;
    }

    /// <summary>
    /// Exports a completed BatchJob to an Excel workbook.
    /// Columns: LinkedIn URL | Name | Title | Company | [Source] Emails | [Source] Phones (per source) | Top Emails | Top Phones
    /// Top Emails/Phones cells are green-highlighted when any value was found on multiple sources.
    /// </summary>
    public byte[] ExportBatch(BatchJob job)
    {
        using var wb = new XLWorkbook();
        var ws       = wb.AddWorksheet("Results");

        // ── Build header row ────────────────────────────────────────────────────
        int col = 1;
        ws.Cell(1, col++).Value = "LinkedIn URL";
        ws.Cell(1, col++).Value = "Name";
        ws.Cell(1, col++).Value = "Title";
        ws.Cell(1, col++).Value = "Company";

        var srcCols = new Dictionary<string, (int emailCol, int phoneCol)>();
        foreach (var src in job.SourceNames)
        {
            srcCols[src] = (col, col + 1);
            ws.Cell(1, col++).Value = $"{src} Emails";
            ws.Cell(1, col++).Value = $"{src} Phones";
        }

        int topEmailCol = col++;
        int topPhoneCol = col;
        ws.Cell(1, topEmailCol).Value = "Top Emails";
        ws.Cell(1, topPhoneCol).Value = "Top Phones";

        // ── Style header row ────────────────────────────────────────────────────
        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold            = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        headerRow.Style.Font.FontColor       = XLColor.White;

        // ── Data rows ───────────────────────────────────────────────────────────
        for (int i = 0; i < job.Rows.Count; i++)
        {
            var row    = job.Rows[i];
            int exRow  = i + 2;
            int c      = 1;

            ws.Cell(exRow, c++).Value = row.LinkedInUrl;
            ws.Cell(exRow, c++).Value = row.Name    ?? "";
            ws.Cell(exRow, c++).Value = row.Title   ?? "";
            ws.Cell(exRow, c++).Value = row.Company ?? "";

            foreach (var src in job.SourceNames)
            {
                var (ec, pc) = srcCols[src];
                var result   = row.SourceResults.GetValueOrDefault(src);
                ws.Cell(exRow, ec).Value = string.Join(", ", result?.Emails ?? []);
                ws.Cell(exRow, pc).Value = string.Join(", ", result?.Phones ?? []);
            }

            // Top emails — green if any value came from multiple sources
            var topEmailCell = ws.Cell(exRow, topEmailCol);
            topEmailCell.Value = string.Join(", ", row.RankedEmails.Select(e => e.Value));
            if (row.RankedEmails.Any(e => e.IsMultiSource))
                topEmailCell.Style.Fill.BackgroundColor = XLColor.LightGreen;

            var topPhoneCell = ws.Cell(exRow, topPhoneCol);
            topPhoneCell.Value = string.Join(", ", row.RankedPhones.Select(p => p.Value));
            if (row.RankedPhones.Any(p => p.IsMultiSource))
                topPhoneCell.Style.Fill.BackgroundColor = XLColor.LightGreen;

            // Alternate row shading
            if (i % 2 == 1)
            {
                ws.Row(exRow).Cells(1, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            }
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Generates a sample import file with demo LinkedIn URLs for testing.
    /// </summary>
    public byte[] GenerateSampleImport()
    {
        var sampleUrls = new[]
        {
            "https://www.linkedin.com/in/sarah-johnson-4a2b",
            "https://www.linkedin.com/in/michael-chen-8f3c",
            "https://www.linkedin.com/in/emily-rodriguez-2d1e",
            "https://www.linkedin.com/in/james-williams-9b4a",
            "https://www.linkedin.com/in/olivia-martinez-7c5f",
            "https://www.linkedin.com/in/daniel-thompson-3e2d",
            "https://www.linkedin.com/in/ashley-brown-6a1b",
            "https://www.linkedin.com/in/christopher-davis-5f8c",
        };

        using var wb = new XLWorkbook();
        var ws       = wb.AddWorksheet("LinkedIn URLs");

        ws.Cell(1, 1).Value                      = "LinkedIn URL";
        ws.Row(1).Style.Font.Bold                = true;
        ws.Row(1).Style.Fill.BackgroundColor     = XLColor.FromHtml("#4472C4");
        ws.Row(1).Style.Font.FontColor           = XLColor.White;

        for (int i = 0; i < sampleUrls.Length; i++)
            ws.Cell(i + 2, 1).Value = sampleUrls[i];

        ws.Column(1).AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
