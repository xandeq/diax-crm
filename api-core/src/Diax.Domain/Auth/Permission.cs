namespace Diax.Domain.Auth;

public class Permission
{
    public Guid Id { get; private set; }
    public string Key { get; private set; }
    public string Description { get; private set; }

    public Permission(string key, string description)
    {
        Id = Guid.NewGuid();
        Key = key;
        Description = description;
    }
}
