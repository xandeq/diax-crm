using Diax.Domain.Common;

namespace Diax.Domain.ImageGeneration;

public class ImageTemplate : AuditableEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }
    public string PromptTemplate { get; private set; }
    public string? DefaultParametersJson { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public bool IsEnabled { get; private set; }

    private ImageTemplate() { } // EF Core

    public ImageTemplate(
        string name,
        string description,
        string category,
        string promptTemplate,
        string? defaultParametersJson = null,
        string? thumbnailUrl = null)
    {
        Name = name;
        Description = description;
        Category = category;
        PromptTemplate = promptTemplate;
        DefaultParametersJson = defaultParametersJson;
        ThumbnailUrl = thumbnailUrl;
        IsEnabled = true;
    }

    public void Update(string name, string description, string category, string promptTemplate,
        string? defaultParametersJson, string? thumbnailUrl)
    {
        Name = name;
        Description = description;
        Category = category;
        PromptTemplate = promptTemplate;
        DefaultParametersJson = defaultParametersJson;
        ThumbnailUrl = thumbnailUrl;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
