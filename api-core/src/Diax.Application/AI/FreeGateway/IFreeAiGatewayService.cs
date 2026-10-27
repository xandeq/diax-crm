namespace Diax.Application.AI.FreeGateway;

/// <summary>
/// Gateway de IAs GRATUITAS para consumo externo (ex.: gerador de prompts via API), com
/// fallback automático entre provedores quando um falha ou está sem crédito.
/// </summary>
public interface IFreeAiGatewayService
{
    /// <summary>
    /// Executa um chat completion percorrendo a cadeia de provedores até obter sucesso.
    /// Lança <see cref="ArgumentException"/> quando a requisição é inválida.
    /// </summary>
    Task<FreeAiChatResult> CompleteAsync(FreeAiChatRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Testa cada item da cadeia com um prompt mínimo, sem afetar cooldown. Resultado fica em
    /// cache por 60s (compartilhado entre requisições) para não martelar os provedores a cada hit
    /// de /health. <paramref name="forceRefresh"/> só tem efeito se o último check tiver mais de
    /// 15s — caso contrário serve o cache mesmo assim.
    /// </summary>
    Task<FreeAiHealthResult> CheckHealthAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>Cadeia efetiva (configurada ou a padrão), já filtrada para provedores conhecidos.</summary>
    IReadOnlyList<FreeAiChainEntry> GetChain();
}
