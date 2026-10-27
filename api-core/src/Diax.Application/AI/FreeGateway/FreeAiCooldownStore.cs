using System.Collections.Concurrent;

namespace Diax.Application.AI.FreeGateway;

public class FreeAiCooldownStore : IFreeAiCooldownStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _cooldownUntil = new(StringComparer.OrdinalIgnoreCase);

    public bool IsInCooldown(string providerKey)
    {
        return _cooldownUntil.TryGetValue(providerKey, out var until) && until > DateTimeOffset.UtcNow;
    }

    public void SetCooldown(string providerKey, TimeSpan duration)
    {
        _cooldownUntil[providerKey] = DateTimeOffset.UtcNow.Add(duration);
    }
}
