using Diax.Application.Customers;
using Diax.Domain.Customers;

namespace Diax.Tests.Application.Customers;

public class PixBrCodeTests
{
    [Fact]
    public void Generate_ProducesValidEmvStructure()
    {
        var payload = PixBrCode.Generate(
            "12345678900", 2500.00m, "Alexandre Queiroz", "Vitoria", "PROP123");

        Assert.StartsWith("000201", payload);           // Payload Format Indicator
        Assert.Contains("br.gov.bcb.pix", payload);     // GUI do PIX
        Assert.Contains("12345678900", payload);        // chave
        Assert.Contains("5303986", payload);            // moeda BRL
        Assert.Contains("54072500.00", payload);        // valor com tamanho (07)
        Assert.Contains("5802BR", payload);             // país
        Assert.Contains("ALEXANDRE QUEIROZ", payload);  // nome uppercase
        Assert.Contains("PROP123", payload);            // txid
        Assert.Matches(@"6304[0-9A-F]{4}$", payload);   // CRC no fim
    }

    [Fact]
    public void Generate_CrcIsConsistent()
    {
        var payload = PixBrCode.Generate("chave@pix.com", 100m, "Nome", "Cidade", "TX1");
        var body = payload[..^4];
        var crc = payload[^4..];
        Assert.Equal(PixBrCode.Crc16Ccitt(body), crc);
    }

    [Fact]
    public void Generate_SanitizesAccentsAndLength()
    {
        var payload = PixBrCode.Generate(
            "k", 10m, "João da Silva Çedilha Muito Longo Nome", "São Paulo", "abc-123!!");
        Assert.Contains("JOAO DA SILVA CEDILHA MUI", payload); // 25 chars sem acento
        Assert.Contains("SAO PAULO", payload);
        Assert.Contains("abc123", payload); // txid só alfanumérico
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("key", 0)]
    [InlineData("key", -5)]
    public void Generate_RejectsInvalidInput(string key, decimal amount)
    {
        Assert.Throws<ArgumentException>(() => PixBrCode.Generate(key, amount, "N", "C", "T"));
    }
}

public class ProposalEntityTests
{
    private static Proposal Make(decimal amount = 1000m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Site institucional", "Escopo...", amount, "pix@x.com", null);

    [Fact]
    public void Ctor_GeneratesOpaqueToken_AndStartsDraft()
    {
        var p = Make();
        Assert.Equal(ProposalStatus.Draft, p.Status);
        Assert.Equal(40, p.PublicToken.Length);
        Assert.True(p.PublicToken.All(char.IsLetterOrDigit));
    }

    [Fact]
    public void RegisterView_MarksSent_AndCountsViews()
    {
        var p = Make();
        p.RegisterView();
        p.RegisterView();
        Assert.Equal(ProposalStatus.Sent, p.Status);
        Assert.Equal(2, p.ViewCount);
        Assert.NotNull(p.SentAt);
    }

    [Fact]
    public void Accept_ThenMarkPaid_FlowsCorrectly()
    {
        var p = Make();
        p.RegisterView();
        p.Accept();
        Assert.Equal(ProposalStatus.Accepted, p.Status);
        p.MarkPaid();
        Assert.Equal(ProposalStatus.Paid, p.Status);
        Assert.NotNull(p.PaidAt);
    }

    [Fact]
    public void Accept_Expired_Throws()
    {
        var p = new Proposal(Guid.NewGuid(), Guid.NewGuid(), "T", "D", 100m, null,
            DateTime.UtcNow.AddDays(-1));
        Assert.Throws<InvalidOperationException>(() => p.Accept());
    }

    [Fact]
    public void Cancel_Paid_Throws()
    {
        var p = Make();
        p.MarkPaid();
        Assert.Throws<InvalidOperationException>(() => p.Cancel());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(200_000_000)]
    public void Ctor_RejectsInvalidAmount(decimal amount)
    {
        Assert.Throws<ArgumentException>(() => Make(amount));
    }
}
