namespace Diax.Application.EmailMarketing.Dtos;

/// <summary>
/// Resultado do reset manual (admin) do Circuit Breaker do piloto.
/// </summary>
public class PilotResetResponse
{
    /// <summary>Indica se o circuito estava aberto antes do reset.</summary>
    public bool WasOpen { get; set; }

    /// <summary>Motivo que mantinha o circuito aberto (se havia).</summary>
    public string? PreviousReason { get; set; }

    /// <summary>Estado do circuito após o reset (esperado: false).</summary>
    public bool IsOpenNow { get; set; }
}
