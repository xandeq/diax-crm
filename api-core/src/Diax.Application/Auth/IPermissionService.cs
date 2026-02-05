namespace Diax.Application.Auth;

/// <summary>
/// Serviço para verificar permissões de usuários via grupos (RBAC).
/// Interface na camada Application, implementação na Infrastructure.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Verifica se o usuário pertence ao grupo system-admin.
    /// </summary>
    Task<bool> IsAdminAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Verifica se o usuário possui uma permissão específica.
    /// Admin tem acesso total automaticamente.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string permissionKey, CancellationToken ct = default);

    /// <summary>
    /// Retorna todas as permissões do usuário (via grupos).
    /// </summary>
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Retorna as keys dos grupos do usuário.
    /// </summary>
    Task<IReadOnlyList<string>> GetGroupKeysAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Conta quantos usuários ativos existem em um grupo específico.
    /// </summary>
    Task<int> CountUsersInGroupAsync(string groupKey, bool onlyActive = true, CancellationToken ct = default);
}
