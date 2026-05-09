namespace Diax.Domain.EmailMarketing.Enums;

public enum SuppressionReason
{
    HardBounce = 0,
    SpamComplaint = 1,
    ManualOptOut = 2,
    InvalidDomain = 3,
    UserListImport = 4,
}
