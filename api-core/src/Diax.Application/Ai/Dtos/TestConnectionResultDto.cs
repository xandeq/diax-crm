namespace Diax.Application.AI.Dtos;

/// <summary>
/// Resultado do teste de conexão com provider
/// </summary>
public record TestConnectionResultDto(
    bool Success,
    string Message,
    string? ErrorDetails = null
);
