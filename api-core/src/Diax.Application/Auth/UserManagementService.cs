using Diax.Application.Auth.Dtos;
using Diax.Domain.Auth;
using Diax.Domain.Auth.Enums;
using Diax.Shared.Security;

namespace Diax.Application.Auth;

public class UserManagementService : IUserManagementService
{
    private readonly IAdminUserRepository _repository;

    public UserManagementService(IAdminUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.GetAllAsync(cancellationToken);
        return users.Select(MapToResponse);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user == null) throw new Exception("Usuário não encontrado.");
        
        return MapToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null) throw new Exception("Este e-mail já está em uso.");

        var passwordHash = PasswordHash.HashPassword(request.Password);
        var user = new AdminUser(request.Email, passwordHash, request.Role);

        await _repository.AddAsync(user, cancellationToken);
        return MapToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user == null) throw new Exception("Usuário não encontrado.");

        if (id == currentUserId && user.Role == UserRole.Admin && request.Role != UserRole.Admin)
        {
            throw new Exception("Você não pode remover sua própria permissão de Administrador.");
        }

        user.SetRole(request.Role);
        
        if (request.IsActive) user.Enable();
        else 
        {
            if (user.Role == UserRole.Admin)
            {
                var adminCount = await _repository.CountByRoleAsync(UserRole.Admin, true, cancellationToken);
                if (adminCount <= 1 && user.IsActive) 
                {
                    throw new Exception("Não é possível desativar o último Administrador ativo.");
                }
            }
            user.Disable();
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.SetPasswordHash(PasswordHash.HashPassword(request.Password));
        }

        await _repository.UpdateAsync(user, cancellationToken);
        return MapToResponse(user);
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user == null) throw new Exception("Usuário não encontrado.");

        if (id == currentUserId)
        {
            throw new Exception("Você não pode deletar sua própria conta.");
        }

        if (user.Role == UserRole.Admin)
        {
            var adminCount = await _repository.CountByRoleAsync(UserRole.Admin, false, cancellationToken);
            if (adminCount <= 1)
            {
                throw new Exception("Não é possível deletar o último Administrador do sistema.");
            }
        }

        await _repository.DeleteAsync(user, cancellationToken);
    }

    private static UserResponse MapToResponse(AdminUser user) => 
        new UserResponse(user.Id, user.Email, user.Role, user.IsActive, user.CreatedAt);
}
