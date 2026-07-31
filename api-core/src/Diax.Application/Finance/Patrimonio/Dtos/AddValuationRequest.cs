using Diax.Domain.Finance.Assets;

namespace Diax.Application.Finance.Patrimonio.Dtos;

public record AddValuationRequest(
    decimal Value,
    string? Currency = null,
    DateTime? AsOf = null,
    AssetValuationSource? Source = null
);
