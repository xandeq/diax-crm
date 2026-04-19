using Diax.Domain.Common;

namespace Diax.Domain.Calendar;

public class Appointment : AuditableEntity, IUserOwnedEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTime Date { get; set; }
    public AppointmentType Type { get; set; }
    public int DurationMinutes { get; set; } = 60;

    // Indica se o compromisso já foi notificado no resumo diário
    public bool DailyNotificationSent { get; set; } = false;

    // Label dinâmico (opcional)
    public Guid? LabelId { get; set; }
    public AppointmentLabel? Label { get; set; }

    // Recorrência — todos os compromissos da série compartilham o mesmo GroupId
    public Guid? RecurrenceGroupId { get; set; }

    // Soft-cancel para excluir apenas uma ocorrência da série
    public bool IsCancelled { get; set; } = false;

    // IUserOwnedEntity
    public Guid UserId { get; set; }
}
