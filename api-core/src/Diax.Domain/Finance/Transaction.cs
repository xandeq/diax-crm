using Diax.Domain.Common;

namespace Diax.Domain.Finance;

/// <summary>
/// Entidade unificada que representa qualquer movimentação financeira.
/// Substitui Income + Expense com classificação via TransactionType.
///
/// Dois eixos de classificação:
/// - RawBankType: tipo bancário bruto (vem do extrato) — verdade do banco
/// - Type: tipo financeiro interpretado (Income/Expense/Transfer/Ignored) — interpretação do usuário
///
/// Amount é SEMPRE positivo. O sinal/direção é determinado pelo Type:
/// - Income = crédito na conta
/// - Expense = débito na conta
/// - Transfer = neutro (gerido via AccountTransfer)
/// - Ignored = neutro
/// </summary>
public class Transaction : AuditableEntity, IUserOwnedEntity
{
    public Guid UserId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }

    /// <summary>
    /// Tipo financeiro interpretado (Income/Expense/Transfer/Ignored)
    /// </summary>
    public TransactionType Type { get; private set; }

    /// <summary>
    /// Tipo bancário bruto, detectado automaticamente na importação.
    /// Null para transações criadas manualmente.
    /// </summary>
    public RawBankType? RawBankType { get; private set; }

    /// <summary>
    /// Descrição original do extrato bancário (preservada sem alteração).
    /// Null para transações criadas manualmente.
    /// </summary>
    public string? RawDescription { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }
    public bool IsRecurring { get; private set; }

    // Categoria unificada
    public Guid? CategoryId { get; private set; }
    public virtual TransactionCategory? Category { get; private set; }

    // Conta financeira (obrigatória para Income/Transfer, condicional para Expense)
    public Guid? FinancialAccountId { get; private set; }
    public virtual FinancialAccount? FinancialAccount { get; private set; }

    // Cartão de crédito (apenas para Expense com PaymentMethod.CreditCard)
    public Guid? CreditCardId { get; private set; }
    public virtual CreditCard? CreditCard { get; private set; }

    public Guid? CreditCardInvoiceId { get; private set; }
    public virtual CreditCardInvoice? CreditCardInvoice { get; private set; }

    // Status e pagamento (relevante para Expense)
    public TransactionStatus Status { get; private set; }
    public DateTime? PaidDate { get; private set; }

    /// <summary>
    /// Agrupa um par de transações de transferência (débito + crédito).
    /// Duas Transactions com o mesmo TransferGroupId são lados opostos da mesma transferência.
    /// </summary>
    public Guid? TransferGroupId { get; private set; }

    /// <summary>
    /// FK para AccountTransfer quando esta transação faz parte de uma transferência formal.
    /// </summary>
    public Guid? AccountTransferId { get; private set; }
    public virtual AccountTransfer? AccountTransfer { get; private set; }

    // EF Core constructor
    protected Transaction() { }

    /// <summary>
    /// Cria uma transação do tipo Income (receita)
    /// </summary>
    public static Transaction CreateIncome(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        Guid? categoryId,
        bool isRecurring,
        Guid financialAccountId,
        Guid userId)
    {
        ValidateCommonFields(description, amount, userId);

        if (financialAccountId == Guid.Empty)
            throw new ArgumentException("Income must be linked to a financial account", nameof(financialAccountId));

        return new Transaction
        {
            Description = description,
            Amount = amount,
            Date = date,
            Type = TransactionType.Income,
            PaymentMethod = paymentMethod,
            CategoryId = categoryId,
            IsRecurring = isRecurring,
            FinancialAccountId = financialAccountId,
            UserId = userId,
            Status = TransactionStatus.Paid // Incomes são sempre "pagos"
        };
    }

    /// <summary>
    /// Cria uma transação do tipo Expense (despesa)
    /// </summary>
    public static Transaction CreateExpense(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        Guid? categoryId,
        bool isRecurring,
        Guid userId,
        Guid? creditCardId = null,
        Guid? creditCardInvoiceId = null,
        Guid? financialAccountId = null,
        TransactionStatus status = TransactionStatus.Pending,
        DateTime? paidDate = null)
    {
        ValidateCommonFields(description, amount, userId);

        var transaction = new Transaction
        {
            Description = description,
            Amount = amount,
            Date = date,
            Type = TransactionType.Expense,
            PaymentMethod = paymentMethod,
            CategoryId = categoryId,
            IsRecurring = isRecurring,
            UserId = userId,
            CreditCardId = creditCardId,
            CreditCardInvoiceId = creditCardInvoiceId,
            FinancialAccountId = financialAccountId,
            Status = status,
            PaidDate = paidDate
        };

        transaction.ValidateExpenseConstraints();
        return transaction;
    }

    /// <summary>
    /// Cria uma transação do tipo Transfer.
    /// Normalmente criada em par (uma para débito, uma para crédito) via AccountTransfer.
    /// </summary>
    public static Transaction CreateTransfer(
        string description,
        decimal amount,
        DateTime date,
        Guid financialAccountId,
        Guid userId,
        Guid transferGroupId,
        Guid? accountTransferId = null)
    {
        ValidateCommonFields(description, amount, userId);

        if (financialAccountId == Guid.Empty)
            throw new ArgumentException("Transfer must be linked to a financial account", nameof(financialAccountId));

        if (transferGroupId == Guid.Empty)
            throw new ArgumentException("Transfer must have a transfer group ID", nameof(transferGroupId));

        return new Transaction
        {
            Description = description,
            Amount = amount,
            Date = date,
            Type = TransactionType.Transfer,
            PaymentMethod = PaymentMethod.BankTransfer,
            FinancialAccountId = financialAccountId,
            UserId = userId,
            TransferGroupId = transferGroupId,
            AccountTransferId = accountTransferId,
            Status = TransactionStatus.Paid
        };
    }

    /// <summary>
    /// Cria uma transação importada de extrato bancário
    /// </summary>
    public static Transaction CreateFromImport(
        string description,
        decimal amount,
        DateTime date,
        TransactionType type,
        PaymentMethod paymentMethod,
        Guid? categoryId,
        Guid? financialAccountId,
        Guid? creditCardId,
        Guid? creditCardInvoiceId,
        Guid userId,
        string rawDescription,
        RawBankType rawBankType,
        TransactionStatus status = TransactionStatus.Pending)
    {
        ValidateCommonFields(description, amount, userId);

        return new Transaction
        {
            Description = description,
            Amount = amount,
            Date = date,
            Type = type,
            RawBankType = rawBankType,
            RawDescription = rawDescription,
            PaymentMethod = paymentMethod,
            CategoryId = categoryId,
            FinancialAccountId = financialAccountId,
            CreditCardId = creditCardId,
            CreditCardInvoiceId = creditCardInvoiceId,
            UserId = userId,
            Status = status,
            IsRecurring = false
        };
    }

    /// <summary>
    /// Atualiza os campos da transação
    /// </summary>
    public void Update(
        string description,
        decimal amount,
        DateTime date,
        PaymentMethod paymentMethod,
        Guid? categoryId,
        bool isRecurring,
        Guid? financialAccountId = null,
        Guid? creditCardId = null,
        Guid? creditCardInvoiceId = null,
        TransactionStatus? status = null,
        DateTime? paidDate = null)
    {
        ValidateCommonFields(description, amount, UserId);

        Description = description;
        Amount = amount;
        Date = date;
        PaymentMethod = paymentMethod;
        CategoryId = categoryId;
        IsRecurring = isRecurring;
        FinancialAccountId = financialAccountId;
        CreditCardId = creditCardId;
        CreditCardInvoiceId = creditCardInvoiceId;

        if (status.HasValue)
            Status = status.Value;

        if (paidDate.HasValue)
            PaidDate = paidDate.Value;

        if (Type == TransactionType.Expense)
            ValidateExpenseConstraints();
    }

    /// <summary>
    /// Reclassifica o tipo financeiro da transação.
    /// Importante: o chamador deve reverter/reaplica saldos conforme necessário ANTES de chamar este método.
    /// </summary>
    public void Reclassify(TransactionType newType, Guid? transferGroupId = null)
    {
        if (newType == TransactionType.Transfer && (transferGroupId == null || transferGroupId == Guid.Empty))
            throw new ArgumentException("Transfer type requires a TransferGroupId", nameof(transferGroupId));

        Type = newType;
        TransferGroupId = transferGroupId;

        // Se reclassificado como Transfer ou Ignored, limpar campos de expense
        if (newType == TransactionType.Transfer || newType == TransactionType.Ignored)
        {
            CreditCardId = null;
            CreditCardInvoiceId = null;
        }
    }

    public void MarkAsPaid(DateTime? paidDate = null)
    {
        Status = TransactionStatus.Paid;
        PaidDate = paidDate ?? DateTime.UtcNow;
    }

    public void MarkAsPending()
    {
        Status = TransactionStatus.Pending;
        PaidDate = null;
    }

    /// <summary>
    /// Define a informação bancária bruta (raw) — normalmente chamado na importação
    /// </summary>
    public void SetRawBankInfo(string rawDescription, RawBankType rawBankType)
    {
        RawDescription = rawDescription;
        RawBankType = rawBankType;
    }

    /// <summary>
    /// Vincula esta transação a um AccountTransfer e TransferGroup
    /// </summary>
    public void LinkToTransfer(Guid accountTransferId, Guid transferGroupId)
    {
        AccountTransferId = accountTransferId;
        TransferGroupId = transferGroupId;
        Type = TransactionType.Transfer;
    }

    private static void ValidateCommonFields(string description, decimal amount, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));
    }

    private void ValidateExpenseConstraints()
    {
        if (PaymentMethod == PaymentMethod.CreditCard)
        {
            if (CreditCardId == null || CreditCardId == Guid.Empty)
                throw new ArgumentException("Credit card expenses must have a valid CreditCardId");

            if (FinancialAccountId != null)
                throw new ArgumentException("Credit card expenses cannot be linked to a FinancialAccount");
        }
        else
        {
            if (FinancialAccountId == null || FinancialAccountId == Guid.Empty)
                throw new ArgumentException($"Cash expenses ({PaymentMethod}) must have a valid FinancialAccountId");

            if (CreditCardId != null)
                throw new ArgumentException($"Cash expenses ({PaymentMethod}) cannot be linked to a credit card");
        }
    }
}
