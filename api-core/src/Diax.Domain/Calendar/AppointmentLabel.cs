using Diax.Domain.Common;

namespace Diax.Domain.Calendar;

public class AppointmentLabel : AuditableEntity, IUserOwnedEntity
{
    public required string Name { get; set; }
    public required string Color { get; set; }  // hex: "#3B82F6"
    public int Order { get; set; }
    public Guid UserId { get; set; }
}
