using Diax.Domain.Customers;

namespace Diax.Tests.Domain;

public class ServicePackageTests
{
    [Fact]
    public void Create_TrimsAndKeepsFields()
    {
        var p = new ServicePackage(Guid.NewGuid(), "  Site Essencial ", "Site de 1 página", "Página única\nWhatsApp", 1800m, 15, 1);
        Assert.Equal("Site Essencial", p.Name);
        Assert.Equal(1800m, p.Price);
        Assert.Equal(15, p.DeliveryDays);
        Assert.True(p.IsActive);
    }

    [Theory]
    [InlineData("", 100)]
    [InlineData("Nome", 0)]
    [InlineData("Nome", -1)]
    public void Create_RejectsInvalid(string name, decimal price)
    {
        Assert.Throws<ArgumentException>(() => new ServicePackage(Guid.NewGuid(), name, "", "", price, 10, 0));
    }

    [Fact]
    public void ToProposalDescription_ListsScopeAndDeadline()
    {
        var p = new ServicePackage(Guid.NewGuid(), "Site", "Site institucional", "Até 5 páginas\nSEO básico", 2500m, 15, 0);
        var d = p.ToProposalDescription();
        Assert.Contains("Site institucional", d);
        Assert.Contains("• Até 5 páginas", d);
        Assert.Contains("• SEO básico", d);
        Assert.Contains("15 dias", d);
    }
}
