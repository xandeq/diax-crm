namespace Diax.Domain.Finance;

public record ParsedTransaction(
    string RawDescription,
    decimal Amount,
    DateTime TransactionDate,
    string? ExternalId = null
);

public interface IFileParser
{
    string FileType { get; }
    IAsyncEnumerable<ParsedTransaction> ParseAsync(Stream fileStream, CancellationToken ct = default);
}
