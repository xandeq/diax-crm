using Diax.Domain.Common;
using Diax.Domain.Customers.Enums;

namespace Diax.Domain.Customers;

/// <summary>
/// Entidade principal que representa tanto Leads quanto Clientes.
/// A evolução de Lead → Cliente ocorre através do Status.
/// </summary>
public class Customer : AuditableEntity
{
    // ===== IDENTIDADE =====

    /// <summary>
    /// Nome completo da pessoa ou nome fantasia da empresa.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Razão social ou nome da empresa (opcional para PF).
    /// </summary>
    public string? CompanyName { get; private set; }

    /// <summary>
    /// Tipo de pessoa: Física (Individual) ou Jurídica (Company).
    /// </summary>
    public PersonType PersonType { get; private set; }

    /// <summary>
    /// CPF ou CNPJ (sem formatação, apenas números).
    /// Opcional no início do cadastro.
    /// </summary>
    public string? Document { get; private set; }

    // ===== CONTATO =====

    /// <summary>
    /// E-mail principal de contato (opcional para leads sem email).
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// E-mail secundário (opcional).
    /// </summary>
    public string? SecondaryEmail { get; private set; }

    /// <summary>
    /// Telefone principal (com DDD).
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// WhatsApp (pode ser diferente do telefone).
    /// </summary>
    public string? WhatsApp { get; private set; }

    /// <summary>
    /// Website da empresa/pessoa.
    /// </summary>
    public string? Website { get; private set; }

    // ===== ORIGEM E CONTEXTO =====

    /// <summary>
    /// De onde veio esse lead/cliente.
    /// </summary>
    public LeadSource Source { get; private set; }

    /// <summary>
    /// Detalhes adicionais sobre a origem (ex: nome do evento, campanha específica).
    /// </summary>
    public string? SourceDetails { get; private set; }

    /// <summary>
    /// Observações livres sobre o cliente/lead.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Tags separadas por vírgula para categorização.
    /// Ex: "premium,tech,startup"
    /// </summary>
    public string? Tags { get; private set; }

    // ===== STATUS E FLAGS =====

    /// <summary>
    /// Status atual no pipeline de vendas.
    /// </summary>
    public CustomerStatus Status { get; private set; }

    /// <summary>
    /// Indica se é um lead (ainda não converteu).
    /// Calculado automaticamente baseado no Status.
    /// </summary>
    public bool IsLead => Status < CustomerStatus.Customer;

    /// <summary>
    /// Indica se é um cliente ativo (já converteu e não deu churn).
    /// </summary>
    public bool IsActiveCustomer => Status == CustomerStatus.Customer;

    /// <summary>
    /// Data em que o lead foi convertido para cliente.
    /// </summary>
    public DateTime? ConvertedAt { get; private set; }

    /// <summary>
    /// Data do último contato/interação.
    /// </summary>
    public DateTime? LastContactAt { get; private set; }

    // ===== SEGMENTAÇÃO (Outreach) =====

    /// <summary>
    /// Pontuação calculada para priorização de outreach.
    /// </summary>
    public int? LeadScore { get; private set; }

    /// <summary>
    /// Segmento atribuído: Hot, Warm ou Cold.
    /// </summary>
    public LeadSegment? Segment { get; private set; }

    /// <summary>
    /// Indica se o lead optou por não receber emails.
    /// </summary>
    public bool EmailOptOut { get; private set; }

    /// <summary>
    /// Data do último email enviado para este contato.
    /// </summary>
    public DateTime? LastEmailSentAt { get; private set; }

    /// <summary>
    /// Total de emails enviados para este contato.
    /// </summary>
    public int EmailSentCount { get; private set; }

    // ===== CONFIABILIDADE & SANITIZAÇÃO =====

    /// <summary>
    /// Avaliação da completude e qualidade do lead.
    /// </summary>
    public LeadQuality? Quality { get; private set; }

    /// <summary>
    /// Classificação do formato/tipo de email.
    /// </summary>
    public EmailType? EmailType { get; private set; }

    /// <summary>
    /// Indica se o domínio do e-mail ou dados relacionados foram considerados suspeitos/falsos.
    /// </summary>
    public bool HasSuspiciousDomain { get; private set; }

    /// <summary>
    /// Informa se o lead está elegível para campanhas de marketing em massa (baseado na qualidade e domínios).
    /// </summary>
    public bool IsEligibleForCampaigns { get; private set; }

    // ===== WHATSAPP (Outreach) =====

    /// <summary>
    /// Indica se o lead optou por não receber mensagens WhatsApp.
    /// </summary>
    public bool WhatsAppOptOut { get; private set; }

    /// <summary>
    /// Data da última mensagem WhatsApp enviada para este contato.
    /// </summary>
    public DateTime? LastWhatsAppSentAt { get; private set; }

    /// <summary>
    /// Total de mensagens WhatsApp enviadas para este contato.
    /// </summary>
    public int WhatsAppSentCount { get; private set; }

    // ===== CONSTRUTORES =====

    /// <summary>
    /// Construtor para EF Core.
    /// </summary>
    protected Customer() { }

    /// <summary>
    /// Cria um novo Lead/Cliente.
    /// </summary>
    public Customer(
        string name,
        string? email = null,
        PersonType personType = PersonType.Individual,
        LeadSource source = LeadSource.Manual)
    {
        Name = name;
        Email = email;
        PersonType = personType;
        Source = source;
        Status = CustomerStatus.Lead; // Sempre começa como Lead
    }

    // ===== MÉTODOS DE DOMÍNIO =====

    /// <summary>
    /// Atualiza as flags de classificação e sanitização do Lead.
    /// </summary>
    public void UpdateClassification(
        LeadQuality? quality,
        EmailType? emailType,
        bool hasSuspiciousDomain,
        bool isEligibleForCampaigns)
    {
        Quality = quality;
        EmailType = emailType;
        HasSuspiciousDomain = hasSuspiciousDomain;
        IsEligibleForCampaigns = isEligibleForCampaigns;
    }

    /// <summary>
    /// Atualiza as informações básicas do cliente.
    /// </summary>
    public void UpdateBasicInfo(
        string name,
        string? email,
        PersonType personType,
        string? companyName = null,
        string? document = null)
    {
        Name = name;
        Email = email;
        PersonType = personType;
        CompanyName = companyName;
        Document = document;
    }

    /// <summary>
    /// Atualiza as informações de contato.
    /// </summary>
    public void UpdateContactInfo(
        string? phone = null,
        string? whatsApp = null,
        string? secondaryEmail = null,
        string? website = null)
    {
        Phone = phone;
        WhatsApp = whatsApp;
        SecondaryEmail = secondaryEmail;
        Website = website;
    }

    /// <summary>
    /// Atualiza o status no pipeline.
    /// </summary>
    public void UpdateStatus(CustomerStatus newStatus)
    {
        var previousStatus = Status;
        Status = newStatus;

        // Se converteu para cliente, registra a data
        if (previousStatus < CustomerStatus.Customer && newStatus == CustomerStatus.Customer)
        {
            ConvertedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Registra um contato/interação.
    /// </summary>
    public void RegisterContact()
    {
        LastContactAt = DateTime.UtcNow;

        // Se ainda é Lead, avança para Contacted
        if (Status == CustomerStatus.Lead)
        {
            Status = CustomerStatus.Contacted;
        }
    }

    /// <summary>
    /// Atualiza as observações.
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }

    /// <summary>
    /// Atualiza as tags.
    /// </summary>
    public void UpdateTags(string? tags)
    {
        Tags = tags;
    }

    /// <summary>
    /// Atualiza informações de origem.
    /// </summary>
    public void UpdateSource(LeadSource source, string? sourceDetails = null)
    {
        Source = source;
        SourceDetails = sourceDetails;
    }

    /// <summary>
    /// Converte o lead para cliente.
    /// </summary>
    public void ConvertToCustomer()
    {
        if (Status == CustomerStatus.Customer)
            return;

        Status = CustomerStatus.Customer;
        ConvertedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca como inativo.
    /// </summary>
    public void Deactivate()
    {
        Status = CustomerStatus.Inactive;
    }

    /// <summary>
    /// Marca como churn (cancelou).
    /// </summary>
    public void MarkAsChurned()
    {
        Status = CustomerStatus.Churned;
    }

    // ===== MÉTODOS DE SEGMENTAÇÃO =====

    /// <summary>
    /// Atualiza score e segmento do lead.
    /// </summary>
    public void UpdateSegmentation(int score, LeadSegment segment)
    {
        LeadScore = score;
        Segment = segment;
    }

    /// <summary>
    /// Registra que um email foi enviado para este contato.
    /// </summary>
    public void RegisterEmailSent()
    {
        LastEmailSentAt = DateTime.UtcNow;
        EmailSentCount++;
    }

    /// <summary>
    /// Marca o lead como opt-out de emails.
    /// </summary>
    public void OptOutEmail()
    {
        EmailOptOut = true;
    }

    /// <summary>
    /// Reativa o recebimento de emails.
    /// </summary>
    public void OptInEmail()
    {
        EmailOptOut = false;
    }

    // ===== MÉTODOS DE WHATSAPP =====

    /// <summary>
    /// Registra que uma mensagem WhatsApp foi enviada para este contato.
    /// </summary>
    public void RegisterWhatsAppSent()
    {
        LastWhatsAppSentAt = DateTime.UtcNow;
        WhatsAppSentCount++;
    }

    /// <summary>
    /// Marca o lead como opt-out de WhatsApp.
    /// </summary>
    public void OptOutWhatsApp()
    {
        WhatsAppOptOut = true;
    }

    /// <summary>
    /// Reativa o recebimento de mensagens WhatsApp.
    /// </summary>
    public void OptInWhatsApp()
    {
        WhatsAppOptOut = false;
    }
}
