namespace Diax.Infrastructure.Email;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "DIAX CRM";
    public int DispatchIntervalMinutes { get; set; } = 5;
    public int DailyLimit { get; set; } = 250;
    public int HourlyLimit { get; set; } = 50;
    public int BatchSize { get; set; } = 50;
    public int PerProviderBatchSize { get; set; } = 20;

    /// <summary>
    /// Sandbox: quando preenchido, TODO email da fila é redirecionado para este endereço
    /// (assunto ganha prefixo [SANDBOX]). Usar em ambientes não-produtivos para impedir
    /// envio real a leads durante desenvolvimento/teste.
    /// </summary>
    public string SandboxRedirectTo { get; set; } = string.Empty;

    /// <summary>
    /// Providers desabilitados (sem credencial válida) — saem da rotação de enfileiramento,
    /// do worker e recebem reassinalação automática dos itens pendentes.
    /// </summary>
    public List<string> DisabledProviders { get; set; } = [];
}
