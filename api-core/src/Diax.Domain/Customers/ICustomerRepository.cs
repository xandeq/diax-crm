using Diax.Domain.Common;
using Diax.Domain.Customers.Enums;

namespace Diax.Domain.Customers;

/// <summary>
/// Repositório específico para Customer com operações adicionais.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>
    /// Busca cliente por e-mail.
    /// </summary>
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Contagens da normalização de nomes calculadas no SQL (sem carregar a base).</summary>
    Task<NormalizationCounts> GetNormalizationCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Página da prévia de normalização: pendentes (sem NormalizedName) primeiro, depois CreatedAt asc
    /// (Id desempata para a paginação ser estável). Skip/Take no SQL.
    /// </summary>
    Task<List<Customer>> GetNormalizationPreviewPageAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Todos os cadastros com o e-mail (trim + case-insensitive). Usado no descadastro:
    /// duplicados não podem continuar recebendo depois do opt-out.
    /// </summary>
    Task<List<Customer>> GetAllByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leads prontos para outreach por email (Status Lead, segmentado, email válido, sem opt-out,
    /// fora do cooldown), ordenados por segmento desc e criação asc. Filtro, ordem e Take no SQL.
    /// </summary>
    Task<List<Customer>> GetEmailOutreachReadyAsync(DateTime cooldownDate, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leads prontos para outreach por WhatsApp (status antes de Customer, segmentado, com WhatsApp ou
    /// telefone, sem opt-out, fora do cooldown), mesma ordenação. Filtro, ordem e Take no SQL.
    /// </summary>
    Task<List<Customer>> GetWhatsAppOutreachReadyAsync(DateTime cooldownDate, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca cliente por documento (CPF/CNPJ).
    /// </summary>
    Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca cliente por telefone (buscando em Phone ou WhatsApp).
    /// </summary>
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca cliente pelo ID durável da fonte externa (hoje: `lead.Id` do Extrator de Dados).
    /// Chave de dedup cross-pull para leads que trocam de e-mail entre passadas do scraper (IMPT-01).
    /// </summary>
    Task<Customer?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista clientes por status.
    /// </summary>
    Task<IEnumerable<Customer>> GetByStatusAsync(CustomerStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clientes do pipeline de vendas: funil aberto (Lead..Negotiating) +
    /// convertidos (Customer) a partir de <paramref name="wonSince"/>.
    /// </summary>
    Task<IEnumerable<Customer>> GetPipelineAsync(DateTime wonSince, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista apenas leads (status < Customer).
    /// </summary>
    Task<IEnumerable<Customer>> GetLeadsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera todos os leads da base (Status < Customer). Usado em operações em lote pesado.
    /// </summary>
    Task<IEnumerable<Customer>> GetAllLeadsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista clientes por IDs informados.
    /// </summary>
    Task<IEnumerable<Customer>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista apenas clientes ativos.
    /// </summary>
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca clientes por origem.
    /// </summary>
    Task<IEnumerable<Customer>> GetBySourceAsync(LeadSource source, CancellationToken cancellationToken = default);

    /// <summary>Contagem por status (GroupBy no servidor) — funil do dashboard comercial.</summary>
    Task<Dictionary<CustomerStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Contagem por segmento (Hot/Warm/Cold) entre leads abertos.</summary>
    Task<Dictionary<LeadSegment, int>> GetSegmentCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe um cliente com o e-mail informado.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe um cliente com o telefone ou WhatsApp informado.
    /// </summary>
    Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exclui múltiplos customers/leads em uma única operação.
    /// </summary>
    Task<int> BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca paginada com filtros e sorting.
    /// </summary>
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        CustomerStatus? status = null,
        LeadSource? source = null,
        string? sortBy = null,
        bool sortDescending = false,
        bool? hasEmail = null,
        bool? hasWhatsApp = null,
        PersonType? personType = null,
        LeadSegment? segment = null,
        bool? onlyLeads = null,
        bool? onlyCustomers = null,
        bool? neverEmailed = null,
        DateTime? createdAfter = null,
        CancellationToken cancellationToken = default);
}

/// <param name="Total">Todos os cadastros.</param>
/// <param name="Normalized">Com NormalizedName.</param>
/// <param name="High">NormalizationScore >= 80.</param>
/// <param name="Low">NormalizationScore entre 1 e 79.</param>
public record NormalizationCounts(int Total, int Normalized, int High, int Low);
