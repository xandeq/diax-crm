namespace Diax.Application.SurfAlerts.Dtos;

public class IngestSurfAlertResponse
{
    public Guid Id { get; set; }

    /// <summary>"created" ou "duplicate" (mesma Idempotency-Key já recebida antes).</summary>
    public string Status { get; set; } = "created";
}
