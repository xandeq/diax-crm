namespace Diax.Domain.Common;

public interface IUserOwnedEntity
{
    Guid UserId { get; }
}
