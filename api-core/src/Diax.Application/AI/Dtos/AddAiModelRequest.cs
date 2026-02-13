using System.ComponentModel.DataAnnotations;

namespace Diax.Application.AI.Dtos;

public class AddAiModelRequest
{
    [Required]
    [StringLength(100)]
    public string ProviderKey { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ModelKey { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public bool IsDiscovered { get; set; } = false;

    public decimal? InputCostHint { get; set; }

    public decimal? OutputCostHint { get; set; }

    public int? MaxTokensHint { get; set; }
}
