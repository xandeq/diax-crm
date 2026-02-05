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

public record UpdateUserRequest(
    UserRole Role,
    bool IsActive,
    string? Password = null
);
