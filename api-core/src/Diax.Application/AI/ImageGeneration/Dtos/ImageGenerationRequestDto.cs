using System.ComponentModel.DataAnnotations;

namespace Diax.Application.AI.ImageGeneration.Dtos;

public record ImageGenerationRequestDto(
    [Required] string Provider,
    [Required] string Model,
    [Required][StringLength(4000, MinimumLength = 3)] string Prompt,
    string? NegativePrompt = null,
    int Width = 1024,
    int Height = 1024,
    int NumberOfImages = 1,
    string? Style = null,
    string? Quality = null,
    string? Seed = null,
    Guid? ProjectId = null,
    string? ReferenceImageBase64 = null
);
