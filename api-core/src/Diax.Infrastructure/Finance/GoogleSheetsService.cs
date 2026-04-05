using Diax.Application.Finance;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
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

    public async Task<List<List<string>>> ReadRangeAsync(string spreadsheetId, string range, CancellationToken cancellationToken = default)
    {
        GoogleCredential credential;

        var serviceAccountJson = _configuration["GoogleSheets:ServiceAccountJson"];
        if (!string.IsNullOrWhiteSpace(serviceAccountJson))
        {
            credential = GoogleCredential
                .FromJson(serviceAccountJson)
                .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
        }
        else
        {
            var serviceAccountFile = _configuration["GoogleSheets:ServiceAccountFile"];
            if (string.IsNullOrWhiteSpace(serviceAccountFile) || !File.Exists(serviceAccountFile))
            {
                _logger.LogError("Google Sheets service account file not found: {Path}", serviceAccountFile);
                throw new FileNotFoundException($"Google Sheets service account file not found: {serviceAccountFile}");
            }

            await using var stream = new FileStream(serviceAccountFile, FileMode.Open, FileAccess.Read);
            credential = GoogleCredential
                .FromStream(stream)
                .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
        }

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
}
