using Diax.Domain.Common;

namespace Diax.Domain.Customers;

public enum MeetingStatus
{
    Confirmed = 0,
    Cancelled = 1,
    Completed = 2,
}

/// <summary>
/// Reunião agendada pelo link público (/agendar). Slots de 30 minutos em
/// horário comercial; o índice único filtrado (user, scheduled_at, status=Confirmed)
/// impede double-booking mesmo em corrida.
/// </summary>
public class Meeting : AuditableEntity, IUserOwnedEntity
{
    public const int DefaultDurationMinutes = 30;

    public Guid UserId { get; set; }

    /// <summary>Lead/cliente correspondente (match por email na reserva), se houver.</summary>
    public Guid? CustomerId { get; private set; }

    public string ContactName { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string? ContactPhone { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Início do slot em UTC.</summary>
    public DateTime ScheduledAt { get; private set; }
    public int DurationMinutes { get; private set; } = DefaultDurationMinutes;
    public MeetingStatus Status { get; private set; } = MeetingStatus.Confirmed;
    public DateTime? CancelledAt { get; private set; }

    protected Meeting() { } // EF Core

    public Meeting(
        Guid userId,
        DateTime scheduledAtUtc,
        string contactName,
        string contactEmail,
        string? contactPhone,
        string? notes,
        Guid? customerId = null)
    {
        if (string.IsNullOrWhiteSpace(contactName))
            throw new ArgumentException("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(contactEmail) || !contactEmail.Contains('@'))
            throw new ArgumentException("Email válido é obrigatório.");
        if (scheduledAtUtc.Kind != DateTimeKind.Utc)
            scheduledAtUtc = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);

        UserId = userId;
        ScheduledAt = scheduledAtUtc;
        ContactName = contactName.Trim();
        ContactEmail = contactEmail.Trim().ToLowerInvariant();
        ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CustomerId = customerId;
    }

    public void LinkCustomer(Guid customerId) => CustomerId = customerId;

    public void Cancel()
    {
        if (Status == MeetingStatus.Completed)
            throw new InvalidOperationException("Reunião concluída não pode ser cancelada.");
        Status = MeetingStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    public void Complete() => Status = MeetingStatus.Completed;
}
