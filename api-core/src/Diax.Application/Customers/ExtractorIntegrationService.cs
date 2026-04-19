using Diax.Application.Customers.Dtos;
using Diax.Application.Customers.Services;
using Diax.Domain.Customers.Enums;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Diax.Application.Customers;

public interface IExtractorIntegrationService
{
    Task<Result<BulkImportResponse>> ImportLeadsAsync(
        string? search = null,
        string? status = null,
        string? tag = null,
        string? city = null,
        int maxPages = 10,
        CancellationToken cancellationToken = default);
}

public class ExtractorIntegrationService : IExtractorIntegrationService
{
    private readonly IExtractorService _extractorService;
    private readonly CustomerImportService _customerImportService;
    private readonly ILogger<ExtractorIntegrationService> _logger;

    private const int PageSize = 100;

    public ExtractorIntegrationService(
        IExtractorService extractorService,
        CustomerImportService customerImportService,
        ILogger<ExtractorIntegrationService> logger)
    {
        _extractorService = extractorService;
        _customerImportService = customerImportService;
        _logger = logger;
    }

    /// <summary>
    /// Busca leads do Extrator de Dados e importa para o CRM com deduplicação.
    /// Pagina automaticamente até buscar todos os resultados (respeita maxPages).
    /// </summary>
    public async Task<Result<BulkImportResponse>> ImportLeadsAsync(
        string? search = null,
        string? status = null,
        string? tag = null,
        string? city = null,
        int maxPages = 10,
        CancellationToken cancellationToken = default)
    {
        var allLeads = new List<ImportCustomerRow>();
        var page = 1;

        _logger.LogInformation(
            "Iniciando importação do Extrator (search={Search}, status={Status}, tag={Tag}, city={City}, maxPages={MaxPages})",
            search, status, tag, city, maxPages);

        while (page <= maxPages)
        {
            var result = await _extractorService.FetchLeadsAsync(search, status, tag, city, page, PageSize);

            if (result.IsFailure)
            {
                _logger.LogError("Falha ao buscar página {Page} do Extrator: {Error}", page, result.Error.Message);
                return Result.Failure<BulkImportResponse>(result.Error);
            }

            var response = result.Value;
            var leads = response.Leads ?? [];

            if (leads.Count == 0)
                break;

            foreach (var lead in leads)
            {
                var row = MapToImportRow(lead);
                if (row != null)
                    allLeads.Add(row);
            }

            _logger.LogInformation("Página {Page}: {Count} leads obtidos (total acumulado: {Total})",
                page, leads.Count, allLeads.Count);

            // Se vieram menos registros que o tamanho da página, é a última
            if (leads.Count < PageSize)
                break;

            page++;
        }

        if (allLeads.Count == 0)
        {
            return Result.Failure<BulkImportResponse>(new Error(
                "ExtractorImport.NoLeads",
                "Nenhum lead válido encontrado no Extrator com os filtros informados."));
        }

        _logger.LogInformation("Importando {Count} leads do Extrator para o CRM...", allLeads.Count);

        var importRequest = new BulkImportRequest(
            Customers: allLeads,
            Source: LeadSource.Scraping
        );

        var fileName = $"Extrator de Dados - {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        var importResult = await _customerImportService.ImportAsync(importRequest, fileName, cancellationToken);

        _logger.LogInformation(
            "Importação concluída: {Success} sucesso, {Skipped} ignorados, {Failed} falhas",
            importResult.SuccessCount, importResult.SkippedCount, importResult.FailedCount);

        return Result.Success(importResult);
    }

    private static ImportCustomerRow? MapToImportRow(ExtractorLead lead)
    {
        // Nome é obrigatório — prioriza ContactName, fallback para CompanyName
        var name = !string.IsNullOrWhiteSpace(lead.ContactName)
            ? lead.ContactName
            : lead.CompanyName;

        if (string.IsNullOrWhiteSpace(name))
            return null;

        var noteParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(lead.Website))
            noteParts.Add($"Website: {lead.Website}");

        if (!string.IsNullOrWhiteSpace(lead.CrmStatus))
            noteParts.Add($"Status no Extrator: {lead.CrmStatus}");

        if (lead.Id > 0)
            noteParts.Add($"ID Extrator: {lead.Id}");

        var tags = new List<string> { "extrator-import" };

        if (!string.IsNullOrWhiteSpace(lead.Tags))
            tags.Add(lead.Tags.Trim());

        return new ImportCustomerRow(
            Name: name,
            Email: lead.Email ?? string.Empty,
            Phone: lead.Phone,
            WhatsApp: lead.WhatsApp ?? lead.Phone,
            CompanyName: lead.CompanyName,
            Notes: noteParts.Count > 0 ? string.Join("\n", noteParts) : null,
            Tags: string.Join(",", tags)
        );
    }
}
