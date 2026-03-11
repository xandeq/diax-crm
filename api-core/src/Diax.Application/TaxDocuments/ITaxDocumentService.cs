using Diax.Application.TaxDocuments.DTOs;
using Diax.Shared.Results;

namespace Diax.Application.TaxDocuments;

public interface ITaxDocumentService
{
    Task<Result<List<TaxDocumentDto>>> GetAllAsync(Guid userId, TaxDocumentListRequest request, CancellationToken ct = default);
    Task<Result<List<int>>> GetFiscalYearsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<TaxDocumentDto>> CreateAsync(Guid userId, UploadTaxDocumentRequest request, string fileName, string storedFileName, string contentType, long fileSize, CancellationToken ct = default);
    Task<Result<TaxDocumentDto>> UpdateAsync(Guid userId, Guid id, UpdateTaxDocumentRequest request, CancellationToken ct = default);
    Task<Result<(string StoredFileName, string FileName, string ContentType)>> GetDownloadInfoAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<Result<string>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default);
}
