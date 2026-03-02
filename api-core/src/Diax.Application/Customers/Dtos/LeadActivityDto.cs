namespace Diax.Application.Customers.Dtos;

/// <summary>
/// Representa um evento na timeline de atividades de um lead/cliente.
/// </summary>
public class LeadActivityDto
{
    /// <summary>
    /// Tipo do evento: "created", "contact_registered", "converted",
    /// "email_sent", "email_failed", "email_queued"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Descrição legível do evento.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detalhe adicional (ex: assunto do email, erro, etc.).
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Data e hora do evento.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Indicador visual: "success", "warning", "info", "error"
    /// </summary>
    public string Status { get; set; } = "info";

    /// <summary>
    /// Booleano para rastrear se o e-mail foi lido
    /// </summary>
    public bool WasRead { get; set; }

    /// <summary>
    /// Data em que o e-mail foi lido (se aplicável)
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
