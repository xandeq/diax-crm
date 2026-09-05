using System.Net;
using System.Text;
using Diax.Application.Customers.Services;
using Diax.Shared.Interfaces;
using Diax.Shared.Results;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using IConfigurationProvider = Diax.Shared.Interfaces.IConfigurationProvider;

namespace Diax.Tests.Customers;

/// <summary>
/// Testes de autenticação server-to-server do ExtractorService.
/// Cobre: 401 → retry único → falha acionável; 403 → falha sem retry; 200 → sucesso.
/// Garante que o token nunca é logado e que o retry acontece exatamente uma vez.
/// </summary>
public class ExtractorServiceAuthTests
{
    private const string FakeUrl = "http://extrator.test:8000";
    private const string FakeToken = "super-secret-token-value";

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handler determinístico: devolve as respostas na ordem dada; quando a fila esgota,
    /// repete a última. Conta quantas chamadas HTTP foram feitas (para provar nº de retries).
    /// </summary>
    private sealed class SequencedHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;
        public int CallCount { get; private set; }

        public SequencedHandler(params HttpResponseMessage[] responses)
            => _responses = new Queue<HttpResponseMessage>(responses);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var response = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
            return Task.FromResult(response);
        }
    }

    private static HttpResponseMessage Resp(HttpStatusCode code, string? json = null)
    {
        var msg = new HttpResponseMessage(code);
        if (json != null)
            msg.Content = new StringContent(json, Encoding.UTF8, "application/json");
        else
            msg.Content = new StringContent(string.Empty);
        return msg;
    }

    private static IHttpClientFactory FactoryFor(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    private static Mock<IConfigurationProvider> ConfigProviderOk()
    {
        var mock = new Mock<IConfigurationProvider>();
        mock.Setup(c => c.GetExtractorConfigAsync())
            .ReturnsAsync(Result.Success((FakeUrl, FakeToken)));
        mock.Setup(c => c.GetConfigSource()).Returns("test");
        return mock;
    }

    private static ExtractorService BuildSut(HttpMessageHandler handler, IConfigurationProvider configProvider)
        => new(
            configProvider,
            FactoryFor(handler),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<ExtractorService>.Instance);

    // ─── 401: retry único, depois falha acionável ────────────────────────────────

    [Fact]
    public async Task FetchLeads_When401Twice_ReturnsUnauthorized_AfterExactlyOneRetry()
    {
        var handler = new SequencedHandler(
            Resp(HttpStatusCode.Unauthorized),
            Resp(HttpStatusCode.Unauthorized));
        var configProvider = ConfigProviderOk();
        var sut = BuildSut(handler, configProvider.Object);

        var result = await sut.FetchLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorServiceUnauthorized", result.Error.Code);
        // Exatamente 2 chamadas HTTP = 1 original + 1 retry (nunca mais).
        Assert.Equal(2, handler.CallCount);
        // Config recarregada uma vez para o retry (além da leitura inicial).
        configProvider.Verify(c => c.GetExtractorConfigAsync(), Times.Exactly(2));
        // Mensagem acionável, sem vazar o token.
        Assert.Contains("tools/diax-extrator", result.Error.Message);
        Assert.Contains("extratordedados/prod", result.Error.Message);
        Assert.DoesNotContain(FakeToken, result.Error.Message);
    }

    [Fact]
    public async Task FetchLeads_When401ThenSuccess_ReturnsSuccess_AfterOneRetry()
    {
        var handler = new SequencedHandler(
            Resp(HttpStatusCode.Unauthorized),
            Resp(HttpStatusCode.OK, "{\"leads\":[{\"id\":1,\"contact_name\":\"Ok\",\"email\":\"ok@ok.com\"}],\"total\":1,\"page\":1,\"per_page\":100}"));
        var sut = BuildSut(handler, ConfigProviderOk().Object);

        var result = await sut.FetchLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Leads!);
        Assert.Equal(2, handler.CallCount);
    }

    // ─── 403: falha sem retry ─────────────────────────────────────────────────────

    [Fact]
    public async Task FetchLeads_When403_ReturnsForbidden_WithNoRetry()
    {
        var handler = new SequencedHandler(Resp(HttpStatusCode.Forbidden));
        var configProvider = ConfigProviderOk();
        var sut = BuildSut(handler, configProvider.Object);

        var result = await sut.FetchLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorServiceForbidden", result.Error.Code);
        // Uma única chamada HTTP: 403 NÃO faz retry.
        Assert.Equal(1, handler.CallCount);
        // Config lida só uma vez (sem reload de retry).
        configProvider.Verify(c => c.GetExtractorConfigAsync(), Times.Once);
        Assert.Contains("tools/diax-extrator", result.Error.Message);
        Assert.DoesNotContain(FakeToken, result.Error.Message);
    }

    // ─── 200: sucesso direto ──────────────────────────────────────────────────────

    [Fact]
    public async Task FetchLeads_When200_ReturnsSuccess_NoRetry()
    {
        var handler = new SequencedHandler(
            Resp(HttpStatusCode.OK, "{\"leads\":[{\"id\":7,\"contact_name\":\"Alpha\",\"email\":\"a@a.com\"}],\"total\":1,\"page\":1,\"per_page\":100}"));
        var configProvider = ConfigProviderOk();
        var sut = BuildSut(handler, configProvider.Object);

        var result = await sut.FetchLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
        Assert.Single(result.Value.Leads!);
        Assert.Equal(7, result.Value.Leads![0].Id);
        Assert.Equal(1, handler.CallCount);
        configProvider.Verify(c => c.GetExtractorConfigAsync(), Times.Once);
    }

    // ─── Wire format real (snake_case do Flask jsonify) — trava o P1 ─────────────
    // Antes do [JsonPropertyName], contact_name/company_name/crm_status bindavam
    // null e TODO lead puxado era descartado (import retornava NoLeads).

    [Fact]
    public async Task FetchLeads_DeserializesSnakeCaseWireFormat_PopulatesMultiWordFields()
    {
        const string wire =
            "{\"leads\":[{\"id\":42,\"company_name\":\"Acme\",\"contact_name\":\"Joao\"," +
            "\"email\":\"a@a.com\",\"phone\":\"27999\",\"whatsapp\":\"5527999\"," +
            "\"crm_status\":\"novo\",\"tags\":\"x\",\"website\":\"acme.com\",\"state\":\"ES\",\"city\":\"Vitoria\"}]," +
            "\"total\":1,\"page\":1,\"per_page\":100}";
        var handler = new SequencedHandler(Resp(HttpStatusCode.OK, wire));
        var sut = BuildSut(handler, ConfigProviderOk().Object);

        var result = await sut.FetchLeadsAsync();

        Assert.True(result.IsSuccess);
        var lead = Assert.Single(result.Value.Leads!);
        Assert.Equal(42, lead.Id);
        Assert.Equal("Acme", lead.CompanyName);   // company_name → CompanyName (era null antes do fix)
        Assert.Equal("Joao", lead.ContactName);   // contact_name → ContactName (era null antes do fix)
        Assert.Equal("novo", lead.CrmStatus);     // crm_status  → CrmStatus
        Assert.Equal("5527999", lead.WhatsApp);   // whatsapp    → WhatsApp
        Assert.Equal("acme.com", lead.Website);
        Assert.Equal("ES", lead.State);           // state       → State
        Assert.Equal("Vitoria", lead.City);       // city        → City
        Assert.Equal(100, result.Value.PerPage);  // per_page    → PerPage
    }

    // ─── 401 → (retry) → 403: vira Forbidden, sem 3ª chamada ─────────────────────
    // Antes, um 403 no retry pós-401 caía no handler genérico em vez do erro acionável.

    [Fact]
    public async Task FetchLeads_When401ThenForbidden_ReturnsForbidden_NoFurtherRetry()
    {
        var handler = new SequencedHandler(
            Resp(HttpStatusCode.Unauthorized),
            Resp(HttpStatusCode.Forbidden));
        var configProvider = ConfigProviderOk();
        var sut = BuildSut(handler, configProvider.Object);

        var result = await sut.FetchLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorServiceForbidden", result.Error.Code);
        // 2 chamadas: 401 original + 1 retry que devolveu 403. Nenhuma terceira.
        Assert.Equal(2, handler.CallCount);
        Assert.DoesNotContain(FakeToken, result.Error.Message);
    }
}
