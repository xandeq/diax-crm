using Diax.Domain.Auth.Enums;

namespace Diax.Application.Auth.Dtos;

public record UserResponse(
    Guid Id,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateUserRequest(
    string Email,
    string Password,
    UserRole Role
);

public class UpdateUserRequest
{
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public string? Password { get; set; }
}
