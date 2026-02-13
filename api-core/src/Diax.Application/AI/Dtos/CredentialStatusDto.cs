namespace Diax.Application.AI.Dtos;

/// <summary>
/// DTO com status de credencial de um provider
/// </summary>
public record CredentialStatusDto(
    bool IsConfigured,
    string? LastFourDigits
);
