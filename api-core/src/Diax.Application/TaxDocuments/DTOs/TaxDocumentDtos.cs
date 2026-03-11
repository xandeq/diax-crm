using Diax.Domain.TaxDocuments;

namespace Diax.Application.TaxDocuments.DTOs;

public record TaxDocumentDto(
    Guid Id,
    int FiscalYear,
    string InstitutionName,
    TaxDocumentType InstitutionType,
    string InstitutionTypeName,
    string FileName,
    long FileSize,
    string ContentType,
    string? Notes,
    DateTime UploadedAt);

public record UploadTaxDocumentRequest(
    int FiscalYear,
    string InstitutionName,
    TaxDocumentType InstitutionType,
    string? Notes);

public record UpdateTaxDocumentRequest(
    string InstitutionName,
    TaxDocumentType InstitutionType,
    int FiscalYear,
    string? Notes);

public record TaxDocumentListRequest(
    int? FiscalYear,
    TaxDocumentType? InstitutionType,
    string? Search);
