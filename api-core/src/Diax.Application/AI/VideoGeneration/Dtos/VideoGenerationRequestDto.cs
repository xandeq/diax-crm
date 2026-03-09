using System.ComponentModel.DataAnnotations;

namespace Diax.Application.AI.VideoGeneration.Dtos;

public record VideoGenerationRequestDto(
    [Required] string Provider,
    [Required] string Model,
    string? Prompt = null,
    string? NegativePrompt = null,
    int? DurationSeconds = 5,
    int Width = 1280,
    int Height = 720,
    string? AspectRatio = null,
    string? Seed = null,
    string? ReferenceImageBase64 = null
);
