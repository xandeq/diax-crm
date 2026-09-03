using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Xunit;

namespace Diax.Tests.Domain.Customers;

/// <summary>
/// P1b — progressão automática do funil. Cobre os dois gatilhos de avanço de status:
/// RegisterContact() (envio efetivo de email → Lead vira Contacted) e
/// RegisterEngagement() (clique → Lead/Contacted vira Qualified).
/// </summary>
public class CustomerTests
{
    private static Customer NewCustomerWithStatus(CustomerStatus status)
    {
        var customer = new Customer("Lead Teste", "lead@teste.com");
        if (status != CustomerStatus.Lead)
        {
            customer.UpdateStatus(status);
        }

        return customer;
    }

    // ───── RegisterEngagement (clique → Qualified) ─────

    [Fact]
    public void RegisterEngagement_FromLead_AdvancesToQualified()
    {
        var customer = NewCustomerWithStatus(CustomerStatus.Lead);

        customer.RegisterEngagement();

        Assert.Equal(CustomerStatus.Qualified, customer.Status);
    }

    [Fact]
    public void RegisterEngagement_FromContacted_AdvancesToQualified()
    {
        var customer = NewCustomerWithStatus(CustomerStatus.Contacted);

        customer.RegisterEngagement();

        Assert.Equal(CustomerStatus.Qualified, customer.Status);
    }

    [Fact]
    public void RegisterEngagement_AlreadyQualified_StaysQualified()
    {
        var customer = NewCustomerWithStatus(CustomerStatus.Qualified);

        customer.RegisterEngagement();

        Assert.Equal(CustomerStatus.Qualified, customer.Status);
    }

    [Theory]
    [InlineData(CustomerStatus.Negotiating)]
    [InlineData(CustomerStatus.Customer)]
    [InlineData(CustomerStatus.Inactive)]
    [InlineData(CustomerStatus.Churned)]
    public void RegisterEngagement_BeyondQualified_NeverChangesStatus(CustomerStatus status)
    {
        var customer = NewCustomerWithStatus(status);

        customer.RegisterEngagement();

        Assert.Equal(status, customer.Status);
    }

    [Fact]
    public void RegisterEngagement_DoesNotTouchLastContactAt()
    {
        // LastContactAt é reservado para contato OUTBOUND (RegisterContact) — um clique
        // não é um envio, então não deve mexer nesse campo.
        var customer = NewCustomerWithStatus(CustomerStatus.Lead);
        Assert.Null(customer.LastContactAt);

        customer.RegisterEngagement();

        Assert.Null(customer.LastContactAt);
    }

    // ───── RegisterContact (envio efetivo → Contacted) — cobertura já existente, ─────
    // ───── reforçada aqui para deixar o contrato do P1b explícito.               ─────

    [Fact]
    public void RegisterContact_FromLead_AdvancesToContacted_AndSetsLastContactAt()
    {
        var customer = NewCustomerWithStatus(CustomerStatus.Lead);

        customer.RegisterContact();

        Assert.Equal(CustomerStatus.Contacted, customer.Status);
        Assert.NotNull(customer.LastContactAt);
    }

    [Fact]
    public void RegisterContact_FromQualified_DoesNotChangeStatus_ButUpdatesLastContactAt()
    {
        var customer = NewCustomerWithStatus(CustomerStatus.Qualified);

        customer.RegisterContact();

        Assert.Equal(CustomerStatus.Qualified, customer.Status);
        Assert.NotNull(customer.LastContactAt);
    }
}
