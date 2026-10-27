namespace Diax.Application.AI.FreeGateway;

/// <summary>
/// Configuração do gateway de IAs GRATUITAS (seção "FreeAi" em appsettings/env/AWS SM).
/// Se <see cref="Chain"/> vier vazio, o serviço usa <see cref="FreeAiGatewayService.DefaultChain"/>.
/// </summary>
public class FreeAiOptions
{
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Tempo (segundos) que um provedor fica fora da rotação após 429/402.</summary>
    public int CooldownSeconds { get; set; } = 60;

    /// <summary>
    /// Ordem de tentativa. Deixe vazio para usar a cadeia padrão embutida no código
    /// (ver <see cref="FreeAiGatewayService.DefaultChain"/>) — provisória, a ser substituída
    /// pelos modelos validados ao vivo.
    /// </summary>
    public List<FreeAiChainEntry> Chain { get; set; } = new();
}

/// <summary>Um item da cadeia de fallback: provedor + modelo.</summary>
public class FreeAiChainEntry
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    public FreeAiChainEntry() { }

    public FreeAiChainEntry(string provider, string model)
    {
        Provider = provider;
        Model = model;
    }
}
