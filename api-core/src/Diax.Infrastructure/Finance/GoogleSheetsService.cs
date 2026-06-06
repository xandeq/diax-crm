using System.Globalization;
using System.Text;
using Diax.Application.Finance;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Finance;

public class GoogleSheetsService : IGoogleSheetsService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleSheetsService> _logger;

    public GoogleSheetsService(IConfiguration configuration, ILogger<GoogleSheetsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<GoogleCredential> BuildCredentialAsync(string scope)
    {
        var serviceAccountJson = _configuration["GoogleSheets:ServiceAccountJson"];
        if (!string.IsNullOrWhiteSpace(serviceAccountJson))
            return GoogleCredential.FromJson(serviceAccountJson).CreateScoped(scope);

        var serviceAccountFile = _configuration["GoogleSheets:ServiceAccountFile"];
        if (string.IsNullOrWhiteSpace(serviceAccountFile) || !File.Exists(serviceAccountFile))
        {
            _logger.LogError("Google Sheets service account file not found: {Path}", serviceAccountFile);
            throw new FileNotFoundException($"Google Sheets service account file not found: {serviceAccountFile}");
        }

        await using var stream = new FileStream(serviceAccountFile, FileMode.Open, FileAccess.Read);
        return GoogleCredential.FromStream(stream).CreateScoped(scope);
    }

    public async Task<List<List<string>>> ReadRangeAsync(string spreadsheetId, string range, CancellationToken cancellationToken = default)
    {
        var credential = await BuildCredentialAsync(SheetsService.Scope.SpreadsheetsReadonly);
        var service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "DiaxCRM"
        });

        var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
        var response = await request.ExecuteAsync(cancellationToken);

        var result = new List<List<string>>();
        if (response.Values == null)
            return result;

        foreach (var row in response.Values)
        {
            var rowValues = row.Select(cell => cell?.ToString() ?? string.Empty).ToList();
            result.Add(rowValues);
        }

        return result;
    }

    public async Task UpdatePaymentStatusAsync(string spreadsheetId, string sheetTabName, string itemName, bool isPaid, DateOnly? paymentDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var credential = await BuildCredentialAsync(SheetsService.Scope.Spreadsheets);
            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "DiaxCRM"
            });

            // Read E1:H120 to locate the item (covers DESPESAS + ASSINATURAS sections)
            var range = $"'{sheetTabName}'!E1:H120";
            var readReq = service.Spreadsheets.Values.Get(spreadsheetId, range);
            var readResp = await readReq.ExecuteAsync(cancellationToken);

            if (readResp.Values == null)
            {
                _logger.LogWarning("GoogleSheets UpdatePaymentStatus: tab '{Tab}' returned no data", sheetTabName);
                return;
            }

            var normalizedTarget = Normalize(itemName);
            int? matchRowIndex = null; // 0-based within E1:H120

            for (int i = 0; i < readResp.Values.Count; i++)
            {
                var row = readResp.Values[i];
                if (row.Count == 0) continue;
                var cellE = row[0]?.ToString() ?? string.Empty;
                if (Normalize(cellE) == normalizedTarget)
                {
                    matchRowIndex = i;
                    break;
                }
            }

            if (matchRowIndex == null)
            {
                _logger.LogWarning("GoogleSheets UpdatePaymentStatus: item '{Item}' not found in tab '{Tab}'", itemName, sheetTabName);
                return;
            }

            // Sheet row number = matchRowIndex + 1 (1-based, since range starts at E1)
            var sheetRow = matchRowIndex.Value + 1;
            var pagoCell = $"'{sheetTabName}'!G{sheetRow}";
            var dateCell = $"'{sheetTabName}'!H{sheetRow}";

            var pagoValue = isPaid ? "x" : string.Empty;
            var dateValue = isPaid && paymentDate.HasValue
                ? paymentDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : string.Empty;

            var data = new List<ValueRange>
            {
                new() { Range = pagoCell, Values = [[pagoValue]] },
                new() { Range = dateCell, Values = [[dateValue]] },
            };

            var batchUpdate = new BatchUpdateValuesRequest
            {
                Data = data,
                ValueInputOption = "RAW"
            };

            await service.Spreadsheets.Values.BatchUpdate(batchUpdate, spreadsheetId).ExecuteAsync(cancellationToken);

            _logger.LogInformation("GoogleSheets: updated '{Item}' row {Row} in '{Tab}' — isPaid={IsPaid}", itemName, sheetRow, sheetTabName, isPaid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoogleSheets UpdatePaymentStatus failed for item '{Item}' in tab '{Tab}'", itemName, sheetTabName);
        }
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim().ToUpperInvariant();
        // Remove diacritics for fuzzy-friendly comparison
        var decomposed = normalized.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString();
    }
}
