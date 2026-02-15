using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;

namespace Diax.Application.Customers.Dtos;

/// <summary>
/// Representa uma linha de dados para importação de customer/lead.
/// </summary>
public record ImportCustomerRow(
    string Name,
    string Email,
    string? Phone,
    string? WhatsApp,
    string? CompanyName,
    string? Notes,
    string? Tags);

/// <summary>
/// Request para importação em lote de customers/leads.
/// </summary>
public record BulkImportRequest(
    List<ImportCustomerRow> Customers,
    LeadSource Source = LeadSource.Import);

/// <summary>
/// Response da importação em lote.
/// </summary>
public record BulkImportResponse(
    bool Success,
    int TotalRecords,
    int SuccessCount,
    int FailedCount,
    List<ImportError> Errors);

/// <summary>
/// Detalhes de um erro ocorrido durante a importação.
/// </summary>
public record ImportError(
    int RowNumber,
    string Email,
    string ErrorMessage);

/// <summary>
/// Response com histórico de uma importação.
/// </summary>
public record ImportHistoryResponse(
    Guid Id,
    string FileName,
    string Type,
    string Status,
    int TotalRecords,
    int SuccessCount,
    int FailedCount,
    string? ErrorDetails,
    DateTime CreatedAt)
{
    /// <summary>
    /// Converte a entidade CustomerImport para DTO de resposta.
    /// </summary>
    public static ImportHistoryResponse FromEntity(CustomerImport import)
    {
        return new ImportHistoryResponse(
            import.Id,
            import.FileName,
            import.Type.ToString(),
            import.Status.ToString(),
            import.TotalRecords,
            import.SuccessCount,
            import.FailedCount,
            import.ErrorDetails,
            import.CreatedAt);
    }
}
