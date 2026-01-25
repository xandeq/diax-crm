using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
using Diax.Domain.Finance;

namespace Diax.Infrastructure.Finance.Parsers;

public class CsvFileParser : IFileParser
{
    public string FileType => "CSV";

    public async IAsyncEnumerable<ParsedTransaction> ParseAsync(Stream fileStream, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ",", // Should be configurable
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        // Basic auto-detection of columns
        var headers = csv.HeaderRecord;
        var dateIndex = Array.FindIndex(headers!, h => h.Contains("Data", StringComparison.OrdinalIgnoreCase) || h.Contains("Date", StringComparison.OrdinalIgnoreCase));
        var descIndex = Array.FindIndex(headers!, h => h.Contains("Descrição", StringComparison.OrdinalIgnoreCase) || h.Contains("Description", StringComparison.OrdinalIgnoreCase) || h.Contains("Histórico", StringComparison.OrdinalIgnoreCase));
        var amountIndex = Array.FindIndex(headers!, h => h.Contains("Valor", StringComparison.OrdinalIgnoreCase) || h.Contains("Amount", StringComparison.OrdinalIgnoreCase) || h.Contains("Valor (R$)", StringComparison.OrdinalIgnoreCase));

        if (dateIndex == -1 || descIndex == -1 || amountIndex == -1)
        {
            throw new Exception("Não foi possível identificar as colunas obrigatórias (Data, Descrição, Valor) no arquivo CSV.");
        }

        while (await csv.ReadAsync())
        {
            var dateStr = csv.GetField(dateIndex);
            var desc = csv.GetField(descIndex);
            var amountStr = csv.GetField(amountIndex);

            if (DateTime.TryParse(dateStr, out var date) && decimal.TryParse(amountStr, out var amount))
            {
                yield return new ParsedTransaction(desc ?? string.Empty, amount, date);
            }
        }
    }
}
