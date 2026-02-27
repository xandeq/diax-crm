using Diax.Domain.Common;

namespace Diax.Domain.Ads;

/// <summary>
/// Representa a conexão com uma conta de anúncios do Facebook.
/// </summary>
public class FacebookAdAccount : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }

    /// <summary>
    /// ID da conta de anúncios no Facebook (formato: act_XXXXXXXXX)
    /// </summary>
    public string AdAccountId { get; private set; } = string.Empty;

    /// <summary>
    /// Token de acesso da Graph API do Facebook.
    /// </summary>
    public string AccessToken { get; private set; } = string.Empty;

    /// <summary>
    /// Nome da conta de anúncios (obtido da API).
    /// </summary>
    public string AccountName { get; private set; } = string.Empty;

    /// <summary>
    /// Moeda da conta de anúncios.
    /// </summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>
    /// Fuso horário da conta.
    /// </summary>
    public string Timezone { get; private set; } = string.Empty;

    /// <summary>
    /// Status da conta (ACTIVE, DISABLED, etc.)
    /// </summary>
    public string AccountStatus { get; private set; } = string.Empty;

    /// <summary>
    /// Indica se a conexão está ativa/válida.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Data da última sincronização com a API.
    /// </summary>
    public DateTime? LastSyncAt { get; private set; }

    protected FacebookAdAccount() { }

    public static FacebookAdAccount Create(
        Guid userId,
        string adAccountId,
        string accessToken,
        string accountName,
        string currency = "",
        string timezone = "",
        string accountStatus = "ACTIVE")
    {
        return new FacebookAdAccount
        {
            UserId = userId,
            AdAccountId = adAccountId.StartsWith("act_") ? adAccountId : $"act_{adAccountId}",
            AccessToken = accessToken,
            AccountName = accountName,
            Currency = currency,
            Timezone = timezone,
            AccountStatus = accountStatus,
            IsActive = true
        };
    }

    public void UpdateToken(string newAccessToken)
    {
        AccessToken = newAccessToken;
        SetUpdated();
    }

    public void UpdateAccountInfo(string accountName, string currency, string timezone, string accountStatus)
    {
        AccountName = accountName;
        Currency = currency;
        Timezone = timezone;
        AccountStatus = accountStatus;
        LastSyncAt = DateTime.UtcNow;
        SetUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdated();
    }

    public void RecordSync()
    {
        LastSyncAt = DateTime.UtcNow;
        SetUpdated();
    }
}
