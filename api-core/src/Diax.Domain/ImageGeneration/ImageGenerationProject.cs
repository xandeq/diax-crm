using Diax.Domain.Common;

namespace Diax.Domain.ImageGeneration;

public enum ProjectStatus
{
    Draft = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public class ImageGenerationProject : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public Guid? TemplateId { get; private set; }
    public ProjectStatus Status { get; private set; }
    public string? ParametersJson { get; private set; }
    public string? ReferenceImageUrl { get; private set; }

    // Navigation properties
    public ImageTemplate? Template { get; private set; }
    public ICollection<GeneratedImage> GeneratedImages { get; private set; } = new List<GeneratedImage>();

    private ImageGenerationProject() { } // EF Core

    public ImageGenerationProject(
        Guid userId,
        string name,
        Guid? templateId = null,
        string? parametersJson = null,
        string? referenceImageUrl = null)
    {
        UserId = userId;
        Name = name;
        TemplateId = templateId;
        Status = ProjectStatus.Draft;
        ParametersJson = parametersJson;
        ReferenceImageUrl = referenceImageUrl;
    }

    public void SetProcessing() => Status = ProjectStatus.Processing;
    public void SetCompleted() => Status = ProjectStatus.Completed;
    public void SetFailed() => Status = ProjectStatus.Failed;

    public void UpdateParameters(string? parametersJson)
    {
        ParametersJson = parametersJson;
    }

    public void SetReferenceImage(string? referenceImageUrl)
    {
        ReferenceImageUrl = referenceImageUrl;
    }
}
