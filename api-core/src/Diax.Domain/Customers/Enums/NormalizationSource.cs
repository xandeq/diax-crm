namespace Diax.Domain.Customers.Enums;

public enum NormalizationSource
{
    Deterministic = 0,
    EmailPrefix   = 1,
    AiFallback    = 2,
    Manual        = 3,
}
