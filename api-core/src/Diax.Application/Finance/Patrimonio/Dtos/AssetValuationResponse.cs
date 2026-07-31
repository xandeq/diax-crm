using Diax.Domain.Finance.Assets;

namespace Diax.Application.Finance.Patrimonio.Dtos;

public record AssetValuationResponse(
    Guid Id,
    Guid AssetId,
    decimal Value,
    string Currency,
    DateTime AsOf,
    AssetValuationSource Source,
    DateTime CreatedAt
);
