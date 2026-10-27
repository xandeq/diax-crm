namespace Diax.Application.AI.FreeGateway;

/// <summary>
/// Estado em memória (singleton, thread-safe) de quais provedores do gateway de IAs gratuitas
/// estão em cooldown após 429/402. Separado de <see cref="FreeAiGatewayService"/> (Scoped) porque
/// precisa sobreviver entre requisições.
/// </summary>
public interface IFreeAiCooldownStore
{
    bool IsInCooldown(string providerKey);
    void SetCooldown(string providerKey, TimeSpan duration);

    /// <summary>Última chave que funcionou para o provedor (null se nenhuma ainda).</summary>
    string? GetWorkingKey(string providerKey);
    void SetWorkingKey(string providerKey, string apiKey);
}
