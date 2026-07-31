using Diax.Application.Finance;

namespace Diax.Tests.Application.Finance;

/// <summary>
/// Regressão do bug de contrato do opportunities-daily: o InvestIQ retorna um
/// envelope {"generated_at","opportunities":[...]}, e o CRM desserializava a
/// resposta como lista direta — parse falhava silenciosamente e a Zona 2 caía
/// sempre nas ideias curadas.
/// </summary>
public class InvestIQIntegrationServiceTests
{
    // Formato real capturado em produção (2026-07-31) — envelope com snake_case.
    private const string EnvelopeJson = """
    {
        "generated_at": "2026-07-31T14:32:54.442184+00:00",
        "opportunities": [
            {
                "asset_class": "Acao",
                "ticker": "MGLU3",
                "title": "Diversificacao: MGLU3",
                "thesis": "Setor Retail Trade nao presente na carteira. DY 12m de 2.0%.",
                "score": 88.4,
                "suggested_allocation_pct": null,
                "risk": "medio",
                "source": "complementary"
            },
            {
                "asset_class": "RendaFixa",
                "ticker": null,
                "title": "Caixa ocioso",
                "thesis": "Aloque o caixa parado em Tesouro Selic.",
                "score": 75.0,
                "suggested_allocation_pct": 10.5,
                "risk": "baixo",
                "source": "cash_parking"
            }
        ]
    }
    """;

    [Fact]
    public void ParseOpportunitiesPayload_Envelope_ReturnsItems()
    {
        var items = InvestIQIntegrationService.ParseOpportunitiesPayload(EnvelopeJson);

        Assert.NotNull(items);
        Assert.Equal(2, items!.Count);
        Assert.Equal("MGLU3", items[0].Ticker);
        Assert.Equal("Acao", items[0].AssetClass);
        Assert.Equal(88.4m, items[0].Score);
        Assert.Equal(10.5m, items[1].SuggestedAllocationPct);
    }

    [Fact]
    public void ParseOpportunitiesPayload_BareList_ReturnsItems()
    {
        const string bareList = """
        [{ "asset_class": "Ouro", "ticker": null, "title": "Ouro", "thesis": "t", "score": 70, "suggested_allocation_pct": null, "risk": "medio" }]
        """;

        var items = InvestIQIntegrationService.ParseOpportunitiesPayload(bareList);

        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal("Ouro", items![0].AssetClass);
    }

    [Fact]
    public void ParseOpportunitiesPayload_InvalidJson_ReturnsNull()
    {
        Assert.Null(InvestIQIntegrationService.ParseOpportunitiesPayload("<html>error</html>"));
        Assert.Null(InvestIQIntegrationService.ParseOpportunitiesPayload(""));
    }

    [Fact]
    public void ParseOpportunitiesPayload_EnvelopeWithoutOpportunities_ReturnsNull()
    {
        Assert.Null(InvestIQIntegrationService.ParseOpportunitiesPayload("""{ "generated_at": "x" }"""));
    }
}
