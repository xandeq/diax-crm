namespace Diax.Domain.Customers.Enums;

/// <summary>
/// Status do cliente no pipeline de vendas.
/// Representa a evolução de Lead → Cliente.
/// </summary>
public enum CustomerStatus
{
    /// <summary>
    /// Lead recém-capturado, ainda não contactado.
    /// </summary>
    Lead = 0,

    /// <summary>
    /// Lead que já foi contactado pelo menos uma vez.
    /// </summary>
    Contacted = 1,

    /// <summary>
    /// Lead qualificado com potencial real de conversão.
    /// </summary>
    Qualified = 2,

    /// <summary>
    /// Em negociação ativa (proposta enviada, reuniões).
    /// </summary>
    Negotiating = 3,

    /// <summary>
    /// Cliente convertido - fechou negócio.
    /// </summary>
    Customer = 4,

    /// <summary>
    /// Cliente/Lead inativo (desistiu, não responde).
    /// </summary>
    Inactive = 5,

    /// <summary>
    /// Cliente que cancelou/encerrou contrato (churn).
    /// </summary>
    Churned = 6
}
