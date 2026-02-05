using Diax.Application.Auth.Dtos;

namespace Diax.Application.Auth;

public interface IUserManagementService
{
    Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
}
