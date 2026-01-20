namespace Diax.Application.Snippets.Dtos;

public class CreateSnippetRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
