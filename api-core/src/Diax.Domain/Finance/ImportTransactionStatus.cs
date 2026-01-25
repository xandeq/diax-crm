namespace Diax.Domain.Finance;

public enum ImportTransactionStatus
{
    Pending = 1,
    Matched = 2,
    Created = 3,
    Skipped = 4,
    Error = 5
}
