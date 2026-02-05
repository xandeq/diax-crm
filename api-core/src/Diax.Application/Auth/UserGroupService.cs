using Diax.Domain.Common;
using Diax.Domain.UserGroups;

namespace Diax.Application.Auth;

public class UserGroupService : IUserGroupService
{
    private readonly IUserGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserGroupService(IUserGroupRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserGroup>> GetAllAsync()
    {
        var result = await _repository.GetAllAsync();
        // Force evaluation if needed, but List is expected
        return result.ToList();
    }

    public async Task<UserGroup?> GetByIdAsync(Guid id)
    {
        // GetById might not include members.
        // We might need a specific repository method "GetWithMembersAsync".
        // For now, let's look at what we have. API usually needs basic info.
        return await _repository.GetByIdAsync(id);
    }

    public async Task<UserGroup> CreateAsync(string name, string description)
    {
        var key = name.ToLower().Trim().Replace(" ", "-");
        var group = new UserGroup(key, name, false, description);

        await _repository.AddAsync(group);
        await _unitOfWork.SaveChangesAsync(); // ✅ CRÍTICO: Persistir no banco

        return group;
    }

    public async Task UpdateAsync(Guid id, string name, string description)
    {
        var group = await _repository.GetByIdAsync(id);
        if (group == null) throw new KeyNotFoundException("User group not found");

        group.Update(name, description);
        await _repository.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync(); // ✅ CRÍTICO: Persistir alterações
    }

    public async Task DeleteAsync(Guid id)
    {
        var group = await _repository.GetByIdAsync(id);
        if (group == null) throw new KeyNotFoundException("User group not found");

        await _repository.DeleteAsync(group);
        await _unitOfWork.SaveChangesAsync(); // ✅ CRÍTICO: Confirmar deleção
    }

    public async Task AddMemberAsync(Guid groupId, Guid userId)
    {
        var group = await _repository.GetByIdWithMembersAsync(groupId);
        if (group == null) throw new KeyNotFoundException("User group not found");

        group.AddMember(userId);
        await _repository.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync(); // ✅ CRÍTICO: Salvar membro adicionado
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid userId)
    {
        var group = await _repository.GetByIdWithMembersAsync(groupId);
        if (group == null) throw new KeyNotFoundException("User group not found");

        group.RemoveMember(userId);
        await _repository.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync(); // ✅ CRÍTICO: Salvar remoção de membro
    }
}
