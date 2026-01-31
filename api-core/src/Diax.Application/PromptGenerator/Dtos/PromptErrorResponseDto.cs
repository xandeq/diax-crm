namespace Diax.Application.PromptGenerator.Dtos;

public class PromptErrorResponseDto
{
    public bool Success { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? CorrelationId { get; set; }
}
