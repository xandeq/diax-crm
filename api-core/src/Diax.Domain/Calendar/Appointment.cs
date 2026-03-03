using Diax.Domain.Common;

namespace Diax.Domain.Calendar;

public class Appointment : AuditableEntity, IUserOwnedEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required DateTime Date { get; set; }
    public AppointmentType Type { get; set; }

    // Indica se o compromisso já foi notificado no resumo diário
    public bool DailyNotificationSent { get; set; } = false;

    // IUserOwnedEntity
    public Guid UserId { get; set; }
}
