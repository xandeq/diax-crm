namespace Diax.Application.Finance;

public interface IGoogleSheetsService
{
    /// <summary>
    /// Reads rows from a Google Sheets tab.
    /// Returns a list of rows, each row is a list of cell values (as strings).
    /// </summary>
    Task<List<List<string>>> ReadRangeAsync(string spreadsheetId, string range, CancellationToken cancellationToken = default);
}
