namespace Diax.Infrastructure.WhatsApp;

/// <summary>
/// Configurações para conexão com a Evolution API (WhatsApp).
/// </summary>
public class EvolutionApiSettings
{
    /// <summary>
    /// URL base da Evolution API.
    /// Ex: https://n8n-evolution-evolution-api.h8tqhp.easypanel.host
    /// </summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>
    /// API Key global da Evolution API.
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Nome da instância do WhatsApp.
    /// Ex: "Alexandre Queiroz Marketing Digital"
    /// </summary>
    public string InstanceName { get; set; } = "";
}
