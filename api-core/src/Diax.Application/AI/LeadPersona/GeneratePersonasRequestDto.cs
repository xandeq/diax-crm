namespace Diax.Application.AI.LeadPersona;

public class GeneratePersonasRequestDto
{
    public string Provider { get; set; } = null!;
    public string? Model { get; set; }
    public int? Count { get; set; } = 5; // Number of personas to generate (3-5)
    public string? FocusSegment { get; set; } // Optional: "Hot", "Warm", "Cold" or specific industry
    public bool? IncludeOutreachTips { get; set; } = true;
    public float? Temperature { get; set; } = 0.7f;
    public int? MaxTokens { get; set; } = 2000;
}
