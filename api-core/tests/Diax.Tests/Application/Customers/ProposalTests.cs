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
    public void BuildProposalEmailHtml_ContainsLinkValueAndName()
    {
        var p = Make(2500m);
        var html = Diax.Application.Customers.ProposalService.BuildProposalEmailHtml(
            p, "Padaria Pão Quente", "https://crm.x.com/proposta?t=abc123");

        Assert.Contains("https://crm.x.com/proposta?t=abc123", html);
        Assert.Contains("Padaria Pão Quente", html);
        Assert.Contains("Site institucional", html);
        Assert.Contains("2.500,00", html); // BRL formatado
        Assert.Contains("Ver proposta completa", html);
    }

    [Fact]
    public void BuildProposalEmailHtml_EscapesHtmlInNames()
    {
        var p = new Proposal(Guid.NewGuid(), Guid.NewGuid(), "Site <script>", "d", 100m, null, null);
        var html = Diax.Application.Customers.ProposalService.BuildProposalEmailHtml(
            p, "Cliente <b>X</b> & Cia", "https://x.com");

        Assert.Contains("Site &lt;script&gt;", html);
        Assert.Contains("Cliente &lt;b&gt;X&lt;/b&gt; &amp; Cia", html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public void BuildProposalEmailHtml_IncludesValidUntil_WhenSet()
    {
        var p = new Proposal(Guid.NewGuid(), Guid.NewGuid(), "T", "d", 100m, null,
            new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc));
        var html = Diax.Application.Customers.ProposalService.BuildProposalEmailHtml(p, "C", "https://x.com");
        Assert.Contains("20/07/2026", html);
    }

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
