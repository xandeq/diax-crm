using Diax.Domain.Customers.Enums;

namespace Diax.Application.EmailMarketing.Pro;

public record NameNormalizationResult(
    string NormalizedName,
    int Score,
    NormalizationSource Source
);

public interface INameNormalizationService
{
    string NormalizeName(string? raw);
    NameNormalizationResult Normalize(string? raw, string? email = null);
}
