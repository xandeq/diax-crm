namespace Diax.Application.Auth.Dtos;

public record UserResponse(
    Guid Id,
    string Email,
    bool IsActive,
    bool IsAdmin,
    List<string> Groups,
    List<string> Permissions,
    DateTime CreatedAt
);

public record CreateUserRequest(
    string Email,
    string Password,
    List<string>? GroupKeys = null
);

public class UpdateUserRequest
{
    public bool IsActive { get; set; }
    public string? Password { get; set; }
    public List<string>? GroupKeys { get; set; }
}

public record GroupMemberDto(
    Guid UserId,
    string Email,
    bool IsActive,
    DateTime JoinedAt
);

public record UserGroupDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    bool IsSystem,
    int MemberCount,
    DateTime CreatedAt
);
