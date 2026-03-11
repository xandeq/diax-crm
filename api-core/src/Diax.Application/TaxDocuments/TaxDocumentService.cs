using Diax.Application.TaxDocuments.DTOs;
using Diax.Domain.Common;
using Diax.Domain.TaxDocuments;
using Diax.Shared.Results;

namespace Diax.Application.TaxDocuments;

public class TaxDocumentService : ITaxDocumentService
{
    private readonly ITaxDocumentRepository _repo;
    private readonly IUnitOfWork _uow;

    public TaxDocumentService(ITaxDocumentRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Result<List<TaxDocumentDto>>> GetAllAsync(Guid userId, TaxDocumentListRequest request, CancellationToken ct = default)
    {
        var docs = await _repo.GetByUserAsync(userId, request.FiscalYear, request.InstitutionType, request.Search, ct);
        return Result.Success(docs.Select(ToDto).ToList());
    }

    public async Task<Result<List<int>>> GetFiscalYearsAsync(Guid userId, CancellationToken ct = default)
    {
        var years = await _repo.GetFiscalYearsByUserAsync(userId, ct);
        return Result.Success(years);
    }

    public async Task<Result<TaxDocumentDto>> CreateAsync(
        Guid userId,
        UploadTaxDocumentRequest request,
        string fileName,
        string storedFileName,
        string contentType,
        long fileSize,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.InstitutionName))
            return Result.Failure<TaxDocumentDto>(Error.Validation("RequiredField", "O nome da instituição é obrigatório."));

        if (request.FiscalYear < 2000 || request.FiscalYear > DateTime.UtcNow.Year + 1)
            return Result.Failure<TaxDocumentDto>(Error.Validation("InvalidFiscalYear", "Ano fiscal inválido."));

        var doc = TaxDocument.Create(
            userId,
            request.FiscalYear,
            request.InstitutionName.Trim(),
            request.InstitutionType,
            fileName,
            storedFileName,
            contentType,
            fileSize,
            request.Notes?.Trim());

        await _repo.AddAsync(doc, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(ToDto(doc));
    }

    public async Task<Result<TaxDocumentDto>> UpdateAsync(Guid userId, Guid id, UpdateTaxDocumentRequest request, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAndUserAsync(id, userId, ct);
        if (doc is null)
            return Result.Failure<TaxDocumentDto>(Error.NotFound(nameof(TaxDocument), id));

        if (string.IsNullOrWhiteSpace(request.InstitutionName))
            return Result.Failure<TaxDocumentDto>(Error.Validation("RequiredField", "O nome da instituição é obrigatório."));

        doc.UpdateMetadata(request.InstitutionName.Trim(), request.InstitutionType, request.FiscalYear, request.Notes?.Trim());
        await _uow.SaveChangesAsync(ct);

        return Result.Success(ToDto(doc));
    }

    public async Task<Result<(string StoredFileName, string FileName, string ContentType)>> GetDownloadInfoAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAndUserAsync(id, userId, ct);
        if (doc is null)
            return Result.Failure<(string, string, string)>(Error.NotFound(nameof(TaxDocument), id));

        return Result.Success<(string, string, string)>((doc.StoredFileName, doc.FileName, doc.ContentType));
    }

    public async Task<Result<string>> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAndUserAsync(id, userId, ct);
        if (doc is null)
            return Result.Failure<string>(Error.NotFound(nameof(TaxDocument), id));

        var storedFileName = doc.StoredFileName;
        await _repo.DeleteAsync(doc, ct);
        await _uow.SaveChangesAsync(ct);

        return Result.Success(storedFileName);
    }

    private static TaxDocumentDto ToDto(TaxDocument d) => new(
        d.Id,
        d.FiscalYear,
        d.InstitutionName,
        d.InstitutionType,
        GetInstitutionTypeName(d.InstitutionType),
        d.FileName,
        d.FileSize,
        d.ContentType,
        d.Notes,
        d.CreatedAt);

    private static string GetInstitutionTypeName(TaxDocumentType type) => type switch
    {
        TaxDocumentType.Bank => "Banco",
        TaxDocumentType.Brokerage => "Corretora",
        TaxDocumentType.Company => "Empresa",
        TaxDocumentType.Platform => "Plataforma",
        TaxDocumentType.Investment => "Investimento",
        TaxDocumentType.Pension => "Previdência",
        TaxDocumentType.Crypto => "Criptoativos",
        TaxDocumentType.PaymentPlatform => "Pagamentos",
        TaxDocumentType.Other => "Outros",
        _ => "Outros"
    };
}
