using VetRIS.Domain.Common;
using VetRIS.Domain.Enums;

namespace VetRIS.Domain.Entities.Reporting;

public class ReportTemplate : BaseEntity
{
    public string Name { get; private set; } = default!;
    public Modality? Modality { get; private set; }
    public string? BodyPart { get; private set; }
    public string ContentTemplate { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private ReportTemplate()
    {
    } // EF Core

    public ReportTemplate(string name, string contentTemplate, Modality? modality = null, string? bodyPart = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Template name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(contentTemplate)) throw new ArgumentException("Template content is required.", nameof(contentTemplate));

        Name = name;
        ContentTemplate = contentTemplate;
        Modality = modality;
        BodyPart = bodyPart;
    }

    public void UpdateContent(string contentTemplate)
    {
        if (string.IsNullOrWhiteSpace(contentTemplate)) throw new ArgumentException("Template content is required.", nameof(contentTemplate));
        ContentTemplate = contentTemplate;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
