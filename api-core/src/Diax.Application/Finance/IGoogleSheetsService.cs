namespace Diax.Application.Finance;

public interface IGoogleSheetsService
{
    /// <summary>
    /// Reads rows from a Google Sheets tab.
    /// Returns a list of rows, each row is a list of cell values (as strings).
    /// </summary>
    Task<List<List<string>>> ReadRangeAsync(string spreadsheetId, string range, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a row in the DESPESAS/ASSINATURAS section of a monthly sheet tab
    /// and updates columns G (PAGO?) and H (DATA PAGAMENTO).
    /// Best-effort: never throws, logs errors internally.
    /// </summary>
    Task UpdatePaymentStatusAsync(string spreadsheetId, string sheetTabName, string itemName, bool isPaid, DateOnly? paymentDate, CancellationToken cancellationToken = default);
}
