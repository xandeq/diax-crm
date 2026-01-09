namespace Diax.Domain.Finance;

/// <summary>
/// Tipos de contas financeiras disponíveis no sistema
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Conta corrente pessoal
    /// </summary>
    Checking = 0,

    /// <summary>
    /// Conta PJ (pessoa jurídica)
    /// </summary>
    Business = 1,

    /// <summary>
    /// Conta poupança
    /// </summary>
    Savings = 2,

    /// <summary>
    /// Caixa (dinheiro físico)
    /// </summary>
    Cash = 3,

    /// <summary>
    /// Investimentos
    /// </summary>
    Investment = 4,

    /// <summary>
    /// Carteira digital (PicPay, Mercado Pago, etc)
    /// </summary>
    DigitalWallet = 5
}
