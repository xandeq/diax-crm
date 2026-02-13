using System.ComponentModel.DataAnnotations;

namespace Diax.Application.AI.Dtos;

public class AddAiProviderRequest
{
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? BaseUrl { get; set; }

    public bool SupportsListModels { get; set; } = false;

    public bool IsEnabled { get; set; } = true;
}
