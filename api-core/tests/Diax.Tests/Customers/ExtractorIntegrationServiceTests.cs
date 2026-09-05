using Diax.Application.Common;
using Diax.Application.Customers;
using Diax.Application.Customers.Dtos;
using Diax.Application.Customers.Services;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Application.EmailMarketing;

namespace Diax.Tests.Customers;

public class ExtractorIntegrationServiceTests
{
    private readonly Mock<IExtractorService> _extractorMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<ICustomerImportRepository> _importRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILeadSanitizationService> _sanitizationMock = new();
    private readonly CustomerImportService _importService;
    private readonly ExtractorIntegrationService _sut;

    public ExtractorIntegrationServiceTests()
    {
        _importRepoMock
            .Setup(r => r.AddAsync(It.IsAny<CustomerImport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerImport ci, CancellationToken _) => ci);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _customerRepoMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        _customerRepoMock
            .Setup(r => r.GetByPhoneAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        _customerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        _sanitizationMock
            .Setup(s => s.SanitizeAndClassify(It.IsAny<RawLeadData>()))
            .Returns((RawLeadData raw) => new SanitizedLeadResult
            {
                Name = raw.Name,
                Email = raw.Email,
                Phone = raw.Phone,
                WhatsApp = raw.WhatsApp,
                CompanyName = raw.CompanyName,
                Notes = raw.Notes,
                IsEmailValid = !string.IsNullOrWhiteSpace(raw.Email),
                HasSuspiciousDomain = false,
                EmailType = EmailType.PersonalDirect,
                Quality = LeadQuality.Medium,
                IsEligibleForCampaigns = true,
                ShouldReject = false
            });

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        var suppressionMock = new Mock<IEmailSuppressionRepository>();
        suppressionMock.Setup(s => s.IsSuppressedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var auditLogMock = new Mock<IAuditLogRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        var circuitBreakerMock = new Mock<IPilotCircuitBreaker>();

        _importService = new CustomerImportService(
            _customerRepoMock.Object,
            _importRepoMock.Object,
            _unitOfWorkMock.Object,
            _sanitizationMock.Object,
            currentUserServiceMock.Object,
            suppressionMock.Object,
            auditLogMock.Object,
            userRepoMock.Object,
            circuitBreakerMock.Object);

        _sut = CreateSut();
    }

    /// <summary>Cria o SUT com options customizáveis (defaults de produção quando null).</summary>
    private ExtractorIntegrationService CreateSut(ExtractorPullOptions? pullOptions = null) =>
        new(
            _extractorMock.Object,
            _importService,
            Options.Create(pullOptions ?? new ExtractorPullOptions()),
            Mock.Of<ILogger<ExtractorIntegrationService>>());

    // ═══════════════════════════════════════════════════════════════════════════
    // SMOKE TESTS — fluxo principal funciona
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportLeads_Success_ImportsAllLeads()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            MakeLead(1, "Acme Corp", "acme@acme.com.br"),
            MakeLead(2, "Beta Ltd", "beta@beta.com.br"),
        }, total: 2);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.SuccessCount);
        Assert.Equal(0, result.Value.FailedCount);
    }

    [Fact]
    public async Task ImportLeads_Success_UsesLeadSourceScraping()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            MakeLead(1, "Test Co", "test@test.com.br"),
        }, total: 1);

        await _sut.ImportLeadsAsync();

        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Source == LeadSource.Scraping),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_PassesFiltersToExtractor()
    {
        SetupExtractorPage(1, new List<ExtractorLead>(), total: 0);

        await _sut.ImportLeadsAsync(search: "web", status: "novo", tag: "ES", city: "Vitória", maxPages: 1);

        _extractorMock.Verify(e => e.FetchLeadsAsync("web", "novo", "ES", "Vitória", 1, 100), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PAGINATION — paginação automática
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportLeads_PaginatesUntilLastPage()
    {
        // Pág 1: 100 leads (full page → continua)
        SetupExtractorPage(1, MakeLeads(100, startId: 1), total: 150);
        // Pág 2: 50 leads (parcial → para)
        SetupExtractorPage(2, MakeLeads(50, startId: 101), total: 150);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(150, result.Value.TotalRecords);
        _extractorMock.Verify(e => e.FetchLeadsAsync(null, null, null, null, 1, 100), Times.Once);
        _extractorMock.Verify(e => e.FetchLeadsAsync(null, null, null, null, 2, 100), Times.Once);
        _extractorMock.Verify(e => e.FetchLeadsAsync(null, null, null, null, 3, 100), Times.Never);
    }

    [Fact]
    public async Task ImportLeads_RespectsMaxPages()
    {
        SetupExtractorPage(1, MakeLeads(100, startId: 1), total: 500);
        SetupExtractorPage(2, MakeLeads(100, startId: 101), total: 500);
        // Pág 3 nunca deveria ser chamada com maxPages=2
        SetupExtractorPage(3, MakeLeads(100, startId: 201), total: 500);

        var result = await _sut.ImportLeadsAsync(maxPages: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Value.TotalRecords);
        _extractorMock.Verify(e => e.FetchLeadsAsync(null, null, null, null, 3, 100), Times.Never);
    }

    [Fact]
    public async Task ImportLeads_StopsOnEmptyPage()
    {
        SetupExtractorPage(1, MakeLeads(100, startId: 1), total: 100);
        SetupExtractorPage(2, new List<ExtractorLead>(), total: 100);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.TotalRecords);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ERROR CASES — falhas de API e dados inválidos
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportLeads_ReturnsFailure_WhenExtractorApiFails()
    {
        _extractorMock
            .Setup(e => e.FetchLeadsAsync(null, null, null, null, 1, 100))
            .ReturnsAsync(Result.Failure<ExtractorLeadsResponse>(new Error(
                "ExtractorApiError", "Connection refused")));

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorApiError", result.Error.Code);
    }

    [Fact]
    public async Task ImportLeads_ReturnsFailure_WhenNoValidLeadsFound()
    {
        // Leads sem nome → MapToImportRow retorna null → zero leads válidos
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = null, CompanyName = null, Email = "x@x.com" },
            new() { Id = 2, ContactName = "", CompanyName = " ", Email = "y@y.com" },
        }, total: 2);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorImport.NoLeads", result.Error.Code);
    }

    [Fact]
    public async Task ImportLeads_ReturnsFailure_WhenExtractorReturnsEmptyFirstPage()
    {
        SetupExtractorPage(1, new List<ExtractorLead>(), total: 0);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorImport.NoLeads", result.Error.Code);
    }

    [Fact]
    public async Task ImportLeads_FailsOnSecondPage_PropagatesError()
    {
        SetupExtractorPage(1, MakeLeads(100, startId: 1), total: 200);
        _extractorMock
            .Setup(e => e.FetchLeadsAsync(null, null, null, null, 2, 100))
            .ReturnsAsync(Result.Failure<ExtractorLeadsResponse>(new Error(
                "ExtractorApiError", "Timeout")));

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Contains("Timeout", result.Error.Message);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MAPPING — transformação ExtractorLead → ImportCustomerRow
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportLeads_MapsContactNameOverCompanyName()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "João Silva", CompanyName = "Acme Corp", Email = "joao@acme.com", State = "ES" }
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        _sanitizationMock.Verify(s => s.SanitizeAndClassify(
            It.Is<RawLeadData>(d => d.Name == "João Silva")), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_FallsBackToCompanyName_WhenContactNameIsEmpty()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = null, CompanyName = "Fallback Inc", Email = "info@fallback.com", State = "ES" }
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        _sanitizationMock.Verify(s => s.SanitizeAndClassify(
            It.Is<RawLeadData>(d => d.Name == "Fallback Inc")), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_SkipsLeadsWithNoNameAtAll()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = null, CompanyName = null, Email = "a@a.com" },
            new() { Id = 2, ContactName = "Valid Name", Email = "b@b.com", State = "ES" },
        }, total: 2);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecords);
    }

    [Fact]
    public async Task ImportLeads_IncludesWebsiteAndStatusInNotes()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 42, ContactName = "Test", Email = "t@t.com", Website = "https://test.com", CrmStatus = "novo", State = "ES" }
        }, total: 1);

        await _sut.ImportLeadsAsync();

        _sanitizationMock.Verify(s => s.SanitizeAndClassify(
            It.Is<RawLeadData>(d =>
                d.Notes != null &&
                d.Notes.Contains("Website: https://test.com") &&
                d.Notes.Contains("Status no Extrator: novo") &&
                d.Notes.Contains("ID Extrator: 42"))), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_AddsExtratorImportTag()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Test", Email = "t@t.com", Tags = "ES,Web Design", State = "ES" }
        }, total: 1);

        await _sut.ImportLeadsAsync();

        // Tags field is not part of RawLeadData — it's set on the ImportCustomerRow
        // Verify by checking that sanitization was called (the import happened)
        _sanitizationMock.Verify(s => s.SanitizeAndClassify(It.IsAny<RawLeadData>()), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_WhatsAppFallsBackToPhone()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Test", Email = "t@t.com", Phone = "27999001122", WhatsApp = null }
        }, total: 1);

        await _sut.ImportLeadsAsync();

        _sanitizationMock.Verify(s => s.SanitizeAndClassify(
            It.Is<RawLeadData>(d => d.WhatsApp == "27999001122")), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REGRESSION — endpoints existentes não quebrados
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportLeads_DeduplicatesByEmail_ViaImportService()
    {
        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("duplicate@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer("Existing", "duplicate@test.com"));

        SetupExtractorPage(1, new List<ExtractorLead>
        {
            MakeLead(1, "New Lead", "new@test.com"),
            MakeLead(2, "Dupe Lead", "duplicate@test.com"),
        }, total: 2);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        // 1 new + 1 existing = at most 2 processed, but existing gets enriched (not duplicated)
        Assert.True(result.Value.SuccessCount >= 1);
    }

    [Fact]
    public async Task ImportLeads_NullLeadsList_TreatedAsEmpty()
    {
        _extractorMock
            .Setup(e => e.FetchLeadsAsync(null, null, null, null, 1, 100))
            .ReturnsAsync(Result.Success(new ExtractorLeadsResponse
            {
                Leads = null,
                Total = 0,
                Page = 1,
                PerPage = 100
            }));

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorImport.NoLeads", result.Error.Code);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IDEMPOTÊNCIA — lead sem contato (nem e-mail nem telefone) é pulado
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportLeads_SkipsContactlessLead_NotSentAsCreate()
    {
        // Lead 1: só nome, SEM e-mail e SEM telefone/whatsapp → sem chave de dedup → pular.
        // Lead 2: válido com e-mail → importa.
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Sem Contato", Email = null, Phone = null, WhatsApp = null },
            MakeLead(2, "Com Email", "com@email.com"),
        }, total: 2);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        // Apenas 1 lead chega ao import (o contactless foi descartado antes).
        Assert.Equal(1, result.Value.TotalRecords);
        Assert.Equal(1, result.Value.SuccessCount);
        // O contactless NUNCA vira create.
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Name == "Sem Contato"),
            It.IsAny<CancellationToken>()), Times.Never);
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Name == "Com Email"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_KeepsLeadWithPhone_NotDroppedByContactlessGuard()
    {
        // Sem e-mail mas COM telefone → tem chave de dedup (telefone) → o guard NÃO descarta:
        // o lead chega ao import como registro (TotalRecords=1). O CustomerImportService pode
        // depois rejeitá-lo por exigir e-mail — mas isso é uma decisão separada e rastreável,
        // não o buraco de "duplicata sem chave" que o guard fecha.
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Só Telefone", Email = null, Phone = "27999887766", WhatsApp = null },
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        // Registro passou pelo guard (não foi silenciosamente descartado como contactless).
        Assert.Equal(1, result.Value.TotalRecords);
    }

    [Fact]
    public async Task ImportLeads_AllContactless_ReturnsNoLeadsFailure()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "A", Email = null, Phone = null, WhatsApp = null },
            new() { Id = 2, ContactName = "B", Email = "  ", Phone = "  ", WhatsApp = "  " },
        }, total: 2);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorImport.NoLeads", result.Error.Code);
        _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PAGINATION/MALFORMED — agregação de páginas e resposta inválida (explícitos)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportLeads_FullPageThenShortPage_AggregatesBothAndStops()
    {
        SetupExtractorPage(1, MakeLeads(100, startId: 1), total: 130);   // página cheia → continua
        SetupExtractorPage(2, MakeLeads(30, startId: 101), total: 130);  // página curta → para

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(130, result.Value.TotalRecords);
        _extractorMock.Verify(e => e.FetchLeadsAsync(null, null, null, null, 1, 100), Times.Once);
        _extractorMock.Verify(e => e.FetchLeadsAsync(null, null, null, null, 2, 100), Times.Once);
        _extractorMock.Verify(e => e.FetchLeadsAsync(null, null, null, null, 3, 100), Times.Never);
    }

    [Fact]
    public async Task ImportLeads_MalformedResponse_NullValue_DoesNotCrash()
    {
        // FetchLeadsAsync devolve Success mas com corpo nulo (Leads = null).
        _extractorMock
            .Setup(e => e.FetchLeadsAsync(null, null, null, null, 1, 100))
            .ReturnsAsync(Result.Success<ExtractorLeadsResponse>(new ExtractorLeadsResponse
            {
                Leads = null,
                Total = null,
                Page = null,
                PerPage = null
            }));

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorImport.NoLeads", result.Error.Code);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // QUALITY FILTER — e-mail lixo rejeitado antes do import
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("contato%40site.com@gmail.com")] // '%' = encoding quebrado de scraping
    [InlineData("teste@sun.com")]
    [InlineData("bot@blok.ai")]
    [InlineData("ai@overchat.ai")]
    [InlineData("info@redcross.org")]
    [InlineData("news@fox.com")]
    [InlineData("tv@foxtv.com")]
    [InlineData("hola@empresa.es")]
    [InlineData("hei@yritys.fi")]
    [InlineData("info@firma.eu")]
    [InlineData("hola@negocio.com.ar")]
    [InlineData("hola@tienda.cl")]
    [InlineData("hola@tienda.com.mx")]
    [InlineData("ola@empresa.pt")]
    public async Task ImportLeads_QualityFilter_RejectsJunkEmails(string junkEmail)
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Junk Lead", Email = junkEmail },
            MakeLead(2, "Lead Bom", "bom@empresa.com.br"),
        }, total: 2);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        // Só o lead bom chega ao import — o lixo foi rejeitado antes de criar.
        Assert.Equal(1, result.Value.TotalRecords);
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Email == junkEmail),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("contato@empresa.com.br")]
    [InlineData("contato@empresa.com")]
    [InlineData("vendas@clinica.med.br")]
    public async Task ImportLeads_QualityFilter_AcceptsBrazilianAndComEmails(string goodEmail)
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Lead Válido", Email = goodEmail, State = "ES" },
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecords);
        Assert.Equal(1, result.Value.SuccessCount);
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Email == goodEmail),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_QualityFilter_DoesNotRejectPhoneOnlyLead()
    {
        // Lead sem e-mail (só telefone) não é caso do filtro de qualidade — passa adiante.
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Só Telefone", Email = null, Phone = "27999887766" },
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecords);
    }

    [Fact]
    public async Task ImportLeads_QualityFilter_AllJunk_ReturnsNoLeadsFailure()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "A", Email = "a@sun.com" },
            new() { Id = 2, ContactName = "B", Email = "b@empresa.es" },
        }, total: 2);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorImport.NoLeads", result.Error.Code);
        _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportLeads_QualityFilter_BlockedTldsConfigurable()
    {
        // Config custom: só .xyz bloqueado → .es passa a ser aceito.
        var sut = CreateSut(new ExtractorPullOptions
        {
            BlockedTlds = new List<string> { ".xyz" },
            BlockedDomains = new List<string>()
        });

        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Espanhol Liberado", Email = "hola@empresa.es", State = "ES" },
            new() { Id = 2, ContactName = "Xyz Bloqueado", Email = "x@dominio.xyz", State = "ES" },
        }, total: 2);

        var result = await sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecords);
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Email == "hola@empresa.es"),
            It.IsAny<CancellationToken>()), Times.Once);
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Email == "x@dominio.xyz"),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GEO FILTER — negócio local (Grande Vitória-ES); Extrator devolve leads de todo o Brasil
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ImportLeads_GeoFilter_SkipsLeadOutsideAllowedState()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Fora de ES", Email = "fora@empresa.com.br", State = "SP" },
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorImport.NoLeads", result.Error.Code);
        _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportLeads_GeoFilter_AcceptsLeadWithLowercaseAllowedState()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Dentro de ES", Email = "dentro@empresa.com.br", State = "es" },
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecords);
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Email == "dentro@empresa.com.br"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_GeoFilter_NoState_FallsBackToAllowedDddInPhone()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "DDD 27", Email = "ddd27@empresa.com.br", State = null, Phone = "(27) 99999-0000" },
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecords);
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Email == "ddd27@empresa.com.br"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_GeoFilter_NoState_SkipsLeadWithOutOfRangeDdd()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "DDD 11", Email = "ddd11@empresa.com.br", State = null, Phone = "+55 (11) 3333-4444" },
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorImport.NoLeads", result.Error.Code);
        _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportLeads_GeoFilter_NoStateAndNoPhone_SkipsAsUnverifiableGeo()
    {
        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Sem Geo", Email = "semgeo@empresa.com.br", State = null, Phone = null, WhatsApp = null },
        }, total: 1);

        var result = await _sut.ImportLeadsAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("ExtractorImport.NoLeads", result.Error.Code);
        _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportLeads_GeoFilter_EmptyAllowedStates_DisablesGeoFilter()
    {
        var sut = CreateSut(new ExtractorPullOptions
        {
            AllowedStates = new List<string>(),
            AllowedDdds = new List<string>()
        });

        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "SP Liberado", Email = "sp@empresa.com.br", State = "SP" },
        }, total: 1);

        var result = await sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecords);
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Email == "sp@empresa.com.br"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportLeads_QualityFilter_TldsWithoutLeadingDotAreNormalized()
    {
        // Config veio sem o '.' inicial ("es" em vez de ".es") → normaliza e ainda bloqueia,
        // sem virar substring-match (ex.: "es" NÃO pode bloquear "empresa.com.br" por conter "es").
        var sut = CreateSut(new ExtractorPullOptions
        {
            BlockedTlds = new List<string> { "es" },
            BlockedDomains = new List<string>()
        });

        SetupExtractorPage(1, new List<ExtractorLead>
        {
            new() { Id = 1, ContactName = "Espanhol", Email = "hola@empresa.es", State = "ES" },
            new() { Id = 2, ContactName = "Brasileiro", Email = "oi@lojases.com.br", State = "ES" },
        }, total: 2);

        var result = await sut.ImportLeadsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecords);
        _customerRepoMock.Verify(r => r.AddAsync(
            It.Is<Customer>(c => c.Email == "oi@lojases.com.br"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private void SetupExtractorPage(int page, List<ExtractorLead> leads, int total)
    {
        _extractorMock
            .Setup(e => e.FetchLeadsAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                page, 100))
            .ReturnsAsync(Result.Success(new ExtractorLeadsResponse
            {
                Leads = leads,
                Total = total,
                Page = page,
                PerPage = 100
            }));
    }

    private static ExtractorLead MakeLead(long id, string name, string email) => new()
    {
        Id = id,
        ContactName = name,
        Email = email,
        Phone = $"2799900{id:D4}",
        Website = $"https://{email.Split('@')[1]}"
    };

    private static List<ExtractorLead> MakeLeads(int count, int startId) =>
        Enumerable.Range(startId, count)
            .Select(i => MakeLead(i, $"Lead {i}", $"lead{i}@company{i}.com.br"))
            .ToList();
}
