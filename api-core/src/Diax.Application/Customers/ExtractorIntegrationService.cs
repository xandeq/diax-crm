using Diax.Application.Customers.Dtos;
using Diax.Application.Customers.Services;
using Diax.Domain.Customers.Enums;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly ICachedMxCheckService _mxCheck;
    private readonly ILogger<ExtractorIntegrationService> _logger;
    private readonly IReadOnlyList<string> _blockedDomains;
    private readonly IReadOnlyList<string> _blockedTlds;
    private readonly IReadOnlyList<string> _allowedStates;
    private readonly IReadOnlyList<string> _allowedDdds;

    private const int PageSize = 100;

    public ExtractorIntegrationService(
        IExtractorService extractorService,
        CustomerImportService customerImportService,
        IOptions<ExtractorPullOptions> pullOptions,
        ICachedMxCheckService mxCheck,
        ILogger<ExtractorIntegrationService> logger)
    {
        _extractorService = extractorService;
        _customerImportService = customerImportService;
        _mxCheck = mxCheck;
        _logger = logger;

        var options = pullOptions.Value;

        // Normaliza uma vez: domínio sem '@' inicial, TLD sempre com '.' inicial, tudo lowercase.
        _blockedDomains = (options.BlockedDomains ?? [])
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim().TrimStart('@').ToLowerInvariant())
            .ToList();

        _blockedTlds = (options.BlockedTlds ?? [])
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Select(t => t.StartsWith('.') ? t : "." + t)
            .ToList();

        _allowedStates = (options.AllowedStates ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().ToUpperInvariant())
            .ToList();

        _allowedDdds = (options.AllowedDdds ?? [])
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim())
            .ToList();
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
        var candidates = new List<(ImportCustomerRow Row, ExtractorLead Lead)>();
        var skippedContactless = 0;
        var rejectedLowQuality = 0;
        var skippedByGeo = 0;
        var rejectedNoMx = 0;
        var mxUnverified = 0;
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

                if (row == null)
                {
                    skippedContactless++;
                    continue;
                }

                // Filtro de qualidade: e-mail lixo conhecido (%, domínios bloqueados, TLDs estrangeiros)
                // nunca entra no CRM — rejeita ANTES de criar/enriquecer.
                if (IsLowQualityEmail(row.Email))
                {
                    rejectedLowQuality++;
                    continue;
                }

                // Filtro geográfico: negócio é local (Grande Vitória-ES) — o Extrator devolve
                // leads do Brasil inteiro, então descarta lead fora da UF/DDD alvo ANTES do import.
                if (IsOutsideTargetGeo(lead))
                {
                    skippedByGeo++;
                    continue;
                }

                candidates.Add((row, lead));
            }

            _logger.LogInformation("Página {Page}: {Count} leads obtidos (total acumulado: {Total})",
                page, leads.Count, candidates.Count);

            // Se vieram menos registros que o tamanho da página, é a última
            if (leads.Count < PageSize)
                break;

            page++;
        }

        // ── EXTR-01: checagem de MX em lote, DEPOIS da paginação ────────────────
        // Em lote (não por lead) para aproveitar cache + paralelismo. Domínios distintos:
        // ~1000 leads/rodada costumam ter uma fração disso em domínios únicos.
        var domains = candidates
            .Select(c => ExtractDomain(c.Row.Email))
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var mxResults = await _mxCheck.CheckManyAsync(domains, cancellationToken);

        foreach (var candidate in candidates)
        {
            var domain = ExtractDomain(candidate.Row.Email);

            // Lead sem e-mail não é julgado por MX — a validação de contato é a jusante.
            if (!string.IsNullOrEmpty(domain) && mxResults.TryGetValue(domain, out var mx))
            {
                if (mx == MxCheckResult.NoMx)
                {
                    rejectedNoMx++;
                    continue;
                }

                if (mx == MxCheckResult.Unverified)
                    mxUnverified++;   // D-02: PASSA — falha de infra não é prova de lead ruim
            }

            allLeads.Add(candidate.Row);
        }

        if (skippedContactless > 0)
        {
            _logger.LogInformation(
                "Ignorados {Skipped} lead(s) sem nome ou sem contato (e-mail/telefone) — evita duplicatas sem chave de dedup.",
                skippedContactless);
        }

        if (rejectedLowQuality > 0)
        {
            _logger.LogInformation(
                "Filtro de qualidade: {Rejected} lead(s) rejeitado(s) por e-mail lixo ('%', domínio bloqueado ou TLD estrangeiro).",
                rejectedLowQuality);
        }

        if (skippedByGeo > 0)
        {
            _logger.LogInformation(
                "Filtro geográfico: {SkippedByGeo} lead(s) ignorado(s) por UF/DDD fora da região alvo ({AllowedStates} / {AllowedDdds}).",
                skippedByGeo, string.Join(",", _allowedStates), string.Join(",", _allowedDdds));
        }

        if (rejectedNoMx > 0)
        {
            _logger.LogInformation(
                "Filtro de MX: {Rejected} lead(s) rejeitado(s) — domínio sem MX e sem registro A (EXTR-01).",
                rejectedNoMx);
        }

        // Instrumentação (questão em aberto #2 da pesquisa): DNS de saída no SmarterASP é
        // indocumentado. Se UDP/53 estiver bloqueado, TODA checagem cai em Unverified e a
        // feature silenciosamente não filtra nada. Este warning torna isso visível na primeira
        // rodada, sem depender de confirmação do provedor.
        var mxEvaluated = rejectedNoMx + mxUnverified + allLeads.Count;
        if (mxEvaluated > 0 && mxUnverified * 100 / mxEvaluated > 80)
        {
            _logger.LogWarning(
                "ALERTA DE INFRAESTRUTURA: {Unverified}/{Total} ({Pct}%) das checagens de MX voltaram " +
                "'não verificado'. Isso indica DNS de saída bloqueado ou instável no host — o filtro " +
                "de MX está efetivamente desligado nesta rodada (nenhum lead foi perdido, per D-02).",
                mxUnverified, mxEvaluated, mxUnverified * 100 / mxEvaluated);
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

    /// <summary>
    /// Detecta e-mails lixo conhecidos do Extrator: '%' no endereço (encoding quebrado de scraping),
    /// domínios bloqueados (sun.com, blok.ai, etc.) e TLDs estrangeiros óbvios (.es, .ar, ...).
    /// .com e .com.br NUNCA são rejeitados por TLD. Lead sem e-mail (só telefone) passa —
    /// o filtro julga só o e-mail; validação de contato é responsabilidade a jusante.
    /// </summary>
    private bool IsLowQualityEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        if (email.Contains('%'))
            return true;

        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1)
            return false; // malformado sem domínio → deixa a validação de e-mail a jusante rejeitar com erro rastreável

        var domain = email[(at + 1)..].Trim().ToLowerInvariant();

        if (_blockedDomains.Contains(domain))
            return true;

        foreach (var tld in _blockedTlds)
        {
            if (domain.EndsWith(tld, StringComparison.Ordinal))
                return true;
        }

        // Domínio placeholder / infraestrutura de terceiro (instagram.local, wixpress.com,
        // sentry.io...). Porta de is_junk_domain do mx_check.py — conta no bucket "e-mail lixo",
        // não no bucket "sem MX" (preserva os 4 buckets de D-04).
        if (JunkDomainFilter.IsJunk(domain))
            return true;

        return false;
    }

    /// <summary>Domínio normalizado de um e-mail (lowercase, sem '@'). Vazio se não houver.</summary>
    private static string ExtractDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;
        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1) return string.Empty;
        return email[(at + 1)..].Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Detecta lead fora da região alvo (Grande Vitória-ES): rejeita ANTES do import.
    /// Prioriza a UF do lead (campo `state`); quando ausente, infere pelo DDD do
    /// telefone/whatsapp. Lead sem UF e sem telefone é considerado fora da região —
    /// geo não verificável não deve ser aceito silenciosamente.
    /// </summary>
    private bool IsOutsideTargetGeo(ExtractorLead lead)
    {
        if (_allowedStates.Count == 0)
            return false;

        var state = lead.State?.Trim().ToUpperInvariant();

        if (!string.IsNullOrEmpty(state))
            return !_allowedStates.Contains(state);

        var rawPhone = !string.IsNullOrWhiteSpace(lead.WhatsApp) ? lead.WhatsApp : lead.Phone;
        var digits = new string((rawPhone ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length >= 12 && digits.StartsWith("55", StringComparison.Ordinal))
            digits = digits[2..];

        if (digits.Length < 2)
            return true; // sem UF e sem telefone válido → geo não verificável, rejeita

        var ddd = digits[..2];
        return !_allowedDdds.Contains(ddd);
    }

    private static ImportCustomerRow? MapToImportRow(ExtractorLead lead)
    {
        // Nome é obrigatório — prioriza ContactName, fallback para CompanyName
        var name = !string.IsNullOrWhiteSpace(lead.ContactName)
            ? lead.ContactName
            : lead.CompanyName;

        if (string.IsNullOrWhiteSpace(name))
            return null;

        // IDEMPOTÊNCIA: a deduplicação efetiva a jusante (CustomerImportService) é por EMAIL —
        // e-mail válido é obrigatório para qualquer fonte (linha ~344 faz `continue` sem e-mail),
        // então o ramo GetByPhoneAsync é inalcançável neste fluxo (leads só-telefone acabam como
        // falha rastreável, nunca criam). Pulls repetidos do mesmo e-mail casam o registro existente
        // (update, não create) → idempotente. O lead.Id do Extrator só vai para Notes (rastreabilidade),
        // não é chave de dedup. Aqui pulamos apenas os leads SEM e-mail E SEM telefone/whatsapp: sem
        // qualquer contato eles jamais dedupam e sujariam a base — melhor um pré-skip que um "create órfão".
        // Nota: dedup durável por lead.Id (coluna ExternalId) fica como recomendação (migração em prod).
        var usablePhone = !string.IsNullOrWhiteSpace(lead.WhatsApp) ? lead.WhatsApp : lead.Phone;
        var hasEmail = !string.IsNullOrWhiteSpace(lead.Email);
        var hasPhone = !string.IsNullOrWhiteSpace(usablePhone);

        if (!hasEmail && !hasPhone)
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
