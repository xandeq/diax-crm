namespace Diax.Domain.Finance;

/// <summary>
/// Classificação bancária bruta (raw) da transação, baseada na descrição do extrato.
/// Representa o tipo de operação bancária, sem interpretação financeira.
/// </summary>
public enum RawBankType
{
    /// <summary>
    /// Tipo não identificado
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// PIX recebido
    /// </summary>
    PixReceived = 1,

    /// <summary>
    /// PIX enviado/transferido
    /// </summary>
    PixSent = 2,

    /// <summary>
    /// TED recebida ou enviada
    /// </summary>
    Ted = 3,

    /// <summary>
    /// DOC recebido ou enviado
    /// </summary>
    Doc = 4,

    /// <summary>
    /// Pagamento de boleto
    /// </summary>
    BoletoPayment = 5,

    /// <summary>
    /// Compra no débito
    /// </summary>
    DebitPurchase = 6,

    /// <summary>
    /// Compra no crédito
    /// </summary>
    CreditPurchase = 7,

    /// <summary>
    /// Pagamento de salário / pró-labore
    /// </summary>
    Salary = 8,

    /// <summary>
    /// Transferência bancária genérica
    /// </summary>
    BankTransfer = 9,

    /// <summary>
    /// Saque em caixa eletrônico
    /// </summary>
    AtmWithdrawal = 10,

    /// <summary>
    /// Depósito em conta
    /// </summary>
    Deposit = 11,

    /// <summary>
    /// Tarifa / taxa bancária
    /// </summary>
    BankFee = 12,

    /// <summary>
    /// Rendimento / juros
    /// </summary>
    Interest = 13,

    /// <summary>
    /// Estorno
    /// </summary>
    Refund = 14,

    /// <summary>
    /// Outro tipo de operação bancária
    /// </summary>
    Other = 99
}
