using Diax.Domain.Common;

namespace Diax.Domain.TaxDocuments;

public class TaxDocument : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public int FiscalYear { get; private set; }
    public string InstitutionName { get; private set; } = string.Empty;
    public TaxDocumentType InstitutionType { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StoredFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string? Notes { get; private set; }

    private TaxDocument() { }

    public static TaxDocument Create(
        Guid userId,
        int fiscalYear,
        string institutionName,
        TaxDocumentType institutionType,
        string fileName,
        string storedFileName,
        string contentType,
        long fileSize,
        string? notes)
    {
        return new TaxDocument
        {
            UserId = userId,
            FiscalYear = fiscalYear,
            InstitutionName = institutionName,
            InstitutionType = institutionType,
            FileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSize = fileSize,
            Notes = notes
        };
    }

    public void UpdateMetadata(string institutionName, TaxDocumentType institutionType, int fiscalYear, string? notes)
    {
        InstitutionName = institutionName;
        InstitutionType = institutionType;
        FiscalYear = fiscalYear;
        Notes = notes;
    }
}
