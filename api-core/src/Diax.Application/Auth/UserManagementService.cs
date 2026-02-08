using Diax.Application.Auth.Dtos;
using Diax.Domain.Auth;
using Diax.Domain.Common;
using Diax.Domain.UserGroups;
using Diax.Shared.Security;

namespace Diax.Application.Auth;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _repository;
    private readonly IUserGroupRepository _groupRepository;
    private readonly IPermissionService _permissionService;
    private readonly IUnitOfWork _unitOfWork;

    public UserManagementService(
        IUserRepository repository,
        IUserGroupRepository groupRepository,
        IPermissionService permissionService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _groupRepository = groupRepository;
        _permissionService = permissionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.GetAllAsync(cancellationToken);
        var result = new List<UserResponse>();
        foreach (var user in users)
        {
            result.Add(await MapToResponseAsync(user, cancellationToken));
        }
        return result;
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new Exception("Usuário não encontrado.");

        return await MapToResponseAsync(user, cancellationToken);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null) throw new Exception("Este e-mail já está em uso.");

        var passwordHash = PasswordHash.HashPassword(request.Password);
        var user = new User(request.Email, passwordHash);

        await _repository.AddAsync(user, cancellationToken);

        // Atribuir grupos se fornecidos
        if (request.GroupKeys?.Any() == true)
        {
            await AssignGroupsAsync(user.Id, request.GroupKeys, cancellationToken);
        }

        return await MapToResponseAsync(user, cancellationToken);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var user = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new Exception("Usuário não encontrado.");

        // Proteção: não permitir que admin remova a si próprio do grupo system-admin
        if (id == currentUserId && request.GroupKeys != null)
        {
            var currentIsAdmin = await _permissionService.IsAdminAsync(currentUserId, cancellationToken);
            var willRemoveAdmin = !request.GroupKeys.Contains("system-admin");

            if (currentIsAdmin && willRemoveAdmin)
            {
                var receivedKeys = string.Join(", ", request.GroupKeys);
                throw new Exception(
                    $"Você não pode remover sua própria permissão de Administrador. " +
                    $"O grupo 'system-admin' deve estar presente na lista de grupos. " +
                    $"Grupos recebidos: [{receivedKeys}]");
            }
        }

        if (request.IsActive)
        {
            user.Enable();
        }
        else
        {
            // Verificar se é o último admin ativo
            if (await _permissionService.IsAdminAsync(id, cancellationToken))
            {
                var adminCount = await _permissionService.CountUsersInGroupAsync("system-admin", true, cancellationToken);
                if (adminCount <= 1 && user.IsActive)
                    throw new Exception("Não é possível desativar o último Administrador ativo.");
            }
            user.Disable();
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.SetPasswordHash(PasswordHash.HashPassword(request.Password));
        }

        await _repository.UpdateAsync(user, cancellationToken);

        // Atualizar grupos se fornecidos
        if (request.GroupKeys != null)
        {
            await AssignGroupsAsync(user.Id, request.GroupKeys, cancellationToken);
        }

        return await MapToResponseAsync(user, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new Exception("Usuário não encontrado.");

        if (id == currentUserId)
            throw new Exception("Você não pode deletar sua própria conta.");

        if (await _permissionService.IsAdminAsync(id, cancellationToken))
        {
            var adminCount = await _permissionService.CountUsersInGroupAsync("system-admin", false, cancellationToken);
            if (adminCount <= 1)
                throw new Exception("Não é possível deletar o último Administrador do sistema.");
        }

        await _repository.DeleteAsync(user, cancellationToken);
    }

    private async Task AssignGroupsAsync(Guid userId, List<string> groupKeys, CancellationToken ct)
    {
        var allGroups = await _groupRepository.GetAllAsync(ct);
        var currentGroups = await _groupRepository.GetByUserIdAsync(userId, ct);

        var desiredGroupIds = allGroups
            .Where(g => groupKeys.Contains(g.Key))
            .Select(g => g.Id)
            .ToHashSet();

        var currentGroupIds = currentGroups.Select(g => g.Id).ToHashSet();

        // Adicionar novos
        foreach (var groupId in desiredGroupIds.Except(currentGroupIds))
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId, ct);
            group?.AddMember(userId);
            if (group != null) await _groupRepository.UpdateAsync(group, ct);
        }

        // Remover antigos
        foreach (var groupId in currentGroupIds.Except(desiredGroupIds))
        {
            var group = await _groupRepository.GetByIdWithMembersAsync(groupId, ct);
            group?.RemoveMember(userId);
            if (group != null) await _groupRepository.UpdateAsync(group, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<UserResponse> MapToResponseAsync(User user, CancellationToken ct)
    {
        var isAdmin = await _permissionService.IsAdminAsync(user.Id, ct);
        var groups = await _permissionService.GetGroupKeysAsync(user.Id, ct);
        var permissions = await _permissionService.GetPermissionsAsync(user.Id, ct);

        return new UserResponse(
            user.Id,
            user.Email,
            user.IsActive,
            isAdmin,
            groups.ToList(),
            permissions.ToList(),
            user.CreatedAt
        );
    }
}
