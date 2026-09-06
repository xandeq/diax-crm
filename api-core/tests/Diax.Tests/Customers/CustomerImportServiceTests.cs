using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Diax.Application.Common;
using Diax.Application.Customers;
using Diax.Application.Customers.Dtos;
using Diax.Application.Customers.Services;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Domain.EmailMarketing;
using Diax.Domain.EmailMarketing.Enums;
using Moq;
using Xunit;

using Diax.Domain.Audit;
using Diax.Domain.Auth;
using Diax.Application.EmailMarketing;

namespace Diax.Tests.Customers;

public class CustomerImportServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<ICustomerImportRepository> _importRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILeadSanitizationService> _sanitizationMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IEmailSuppressionRepository> _suppressionRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPilotCircuitBreaker> _circuitBreakerMock = new();
    private readonly CustomerImportService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public CustomerImportServiceTests()
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

        _currentUserServiceMock.Setup(s => s.UserId).Returns(_userId);
        _suppressionRepoMock
            .Setup(s => s.IsSuppressedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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

        _sut = new CustomerImportService(
            _customerRepoMock.Object,
            _importRepoMock.Object,
            _unitOfWorkMock.Object,
            _sanitizationMock.Object,
            _currentUserServiceMock.Object,
            _suppressionRepoMock.Object,
            _auditLogRepoMock.Object,
            _userRepoMock.Object,
            _circuitBreakerMock.Object);
    }

    [Fact]
    public async Task Import_ShouldReject_WhenSourceIsUnknown()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Unknown);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("Origem inválida", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_ShouldReject_WhenEmailIsInvalid()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos_invalid_email")
        {
            ValidationStatus = "valido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("E-mail inválido ou ausente", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_ShouldReject_WhenValidationStatusIsMissing()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = null
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("Validation status é obrigatório", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_ShouldReject_WhenValidationStatusIsInadequate()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "bounced_status"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("Validation status inadequado", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_ShouldReject_WhenCustomerAlreadyHasOptOut()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        var existing = new Customer("Carlos", "carlos@agencia.com.br");
        existing.OptOutEmail();

        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("carlos@agencia.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("opt-out ativo", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_ShouldReject_WhenEmailIsSuppressed()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        _suppressionRepoMock
            .Setup(s => s.IsSuppressedAsync(_userId, "carlos@agencia.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("lista de supressão", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_ShouldSucceed_AndAddPilotCandidateTag()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido",
            Tags = "tag1, tag2"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        Customer? savedCustomer = null;
        _customerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => savedCustomer = c)
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.NotNull(savedCustomer);
        Assert.Contains("pilot_candidate", savedCustomer.Tags);
        Assert.Contains("tag1", savedCustomer.Tags);
        Assert.Contains("tag2", savedCustomer.Tags);
    }

    [Fact]
    public async Task Import_ShouldMapExtendedFields()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos Oliveira", "carlos@agencianova.com.br")
        {
            CompanyName = "Agência Nova",
            Website = "www.agencianova.com.br",
            City = "Rio de Janeiro",
            CurrentTool = "Pipedrive",
            MainPain = "Falta de integração com o WhatsApp",
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        Customer? savedCustomer = null;
        _customerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => savedCustomer = c)
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.Equal(1, result.SuccessCount);
        Assert.NotNull(savedCustomer);
        Assert.Equal("www.agencianova.com.br", savedCustomer.Website);
        Assert.Contains("pipedrive", savedCustomer.Tags);
        Assert.Contains("whatsapp", savedCustomer.Tags);
        Assert.Contains("validation_status_valido", savedCustomer.Tags);
        Assert.Contains("consent_status_consentido", savedCustomer.Tags);
        Assert.Contains("Cidade: Rio de Janeiro", savedCustomer.Notes);
    }

    [Fact]
    public async Task Import_ShouldSucceedButNotPersist_WhenDryRunIsTrue()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import, DryRun: true);

        bool addCalled = false;
        _customerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback(() => addCalled = true)
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.False(addCalled);
    }

    [Fact]
    public async Task Import_ShouldReject_WhenLimitOf10LeadsExceeded()
    {
        // Arrange
        var rows = Enumerable.Range(1, 11)
            .Select(i => new ImportCustomerRow($"Carlos {i}", $"carlos{i}@agencia.com.br")
            {
                ValidationStatus = "valido",
                ConsentStatus = "consentido"
            })
            .ToList();
        var request = new BulkImportRequest(rows, LeadSource.Import, DryRun: false);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("excede o limite máximo de 10 leads", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_ShouldReject_WhenConsentStatusIsMissing()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = null
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("Consent status é obrigatório", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_ShouldReject_WhenConsentStatusIsRefused()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "recusado"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("Consent status inválido ou recusado", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_ShouldAbortWholeBatch_WhenOneLeadIsInvalid()
    {
        // Arrange
        var row1 = new ImportCustomerRow("Carlos Valido", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var row2 = new ImportCustomerRow("Carlos Invalido", "carlos_invalid_email")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row1, row2 }, LeadSource.Import);

        bool addCalled = false;
        _customerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback(() => addCalled = true)
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(1, result.SuccessCount); // Carlos Valido was validated successfully in dry-run pass
        Assert.Equal(1, result.FailedCount); // Carlos Invalido failed
        Assert.False(addCalled); // But because it failed validation, we did not call AddAsync at all!
    }

    [Fact]
    public async Task Import_ShouldReject_WhenLeadIsDuplicateInDB()
    {
        // Arrange
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        var existing = new Customer("Carlos Duplicado", "carlos@agencia.com.br");
        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("carlos@agencia.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains("Lead duplicado", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Import_DoublePull_IsIdempotent_NoDuplicateCreatedOnSecondImport()
    {
        // Simula dois PULLs consecutivos do Extrator com o MESMO conjunto de leads.
        // 1º pull: repositório não conhece os e-mails → cria. 2º pull: repositório devolve
        // os customers já criados → deve enriquecer/ignorar, nunca duplicar.
        var rows = new List<ImportCustomerRow>
        {
            new("Lead A", "a@test.com", Phone: "27999000001"),
            new("Lead B", "b@test.com", Phone: "27999000002"),
        };
        var request = new BulkImportRequest(rows, LeadSource.Scraping);

        var created = new List<Customer>();
        _customerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => created.Add(c))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        // 1º PULL — nenhum existente
        var first = await _sut.ImportAsync(request, "pull-1.json");

        Assert.Equal(2, first.SuccessCount);
        Assert.Equal(2, created.Count);

        // 2º PULL — repositório agora conhece os leads criados (dedup por e-mail)
        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("a@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(created[0]);
        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("b@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(created[1]);

        var createdCountBeforeSecondPull = created.Count;
        var second = await _sut.ImportAsync(request, "pull-2.json");

        // Nenhum customer NOVO criado no 2º pull → zero duplicatas.
        Assert.Equal(createdCountBeforeSecondPull, created.Count);
        Assert.Equal(0, second.SuccessCount);   // nada de novo para enriquecer (dados idênticos)
        Assert.Equal(2, second.SkippedCount);   // ambos reconhecidos como duplicata já existente
    }

    [Fact]
    public async Task Import_ShouldReject_WhenLeadIsDuplicateInBatch()
    {
        // Arrange
        var row1 = new ImportCustomerRow("Carlos 1", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var row2 = new ImportCustomerRow("Carlos 2", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row1, row2 }, LeadSource.Import);

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(1, result.SuccessCount); // First one validated successfully in first pass/dry-run step
        Assert.Equal(1, result.SkippedCount); // Second one detected as duplicate in batch
        Assert.Contains("Duplicata no lote", result.Errors[0].ErrorMessage);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REJECTION COUNTS + WEBSITE CLASSIFICATION — EXTR-02 / EXTR-03 (07-05)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Import_NullRejectionCounts_CountersDefaultZero()
    {
        // Arrange — sem RejectionCounts (default) e sem duplicatas.
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        CustomerImport? captured = null;
        _importRepoMock
            .Setup(r => r.AddAsync(It.IsAny<CustomerImport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerImport ci, CancellationToken _) => { captured = ci; return ci; });

        // Act
        await _sut.ImportAsync(request, "test.json");

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(0, captured!.GeoRejectedCount);
        Assert.Equal(0, captured.LowQualityEmailRejectedCount);
        Assert.Equal(0, captured.NoMxRejectedCount);
        Assert.Equal(0, captured.DuplicateRejectedCount);
    }

    [Fact]
    public async Task Import_RecordsRejectionCountsAndDuplicateCount_SurvivesComplete()
    {
        // Arrange — 1 lead novo + 2 leads que casam com Customer já existente (sem nada novo
        // para enriquecer, para isolar a contagem de duplicados do efeito colateral do
        // recálculo de WebsiteKind). RejectionCounts pré-computados pelo Extrator.
        // Tags pré-preenchida com "pilot_candidate" para que o enrich não ache "informação nova"
        // nesse campo — isola a contagem de duplicados de qualquer outro efeito colateral do enrich.
        var existing1 = new Customer("Existing One", "dup1@agencia.com.br");
        existing1.UpdateContactInfo(phone: "27999000001", whatsApp: "27999000001");
        existing1.UpdateTags("pilot_candidate");
        var existing2 = new Customer("Existing Two", "dup2@agencia.com.br");
        existing2.UpdateContactInfo(phone: "27999000002", whatsApp: "27999000002");
        existing2.UpdateTags("pilot_candidate");

        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("dup1@agencia.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing1);
        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("dup2@agencia.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing2);

        var rows = new List<ImportCustomerRow>
        {
            new("Lead Novo", "novo@agencia.com.br", Phone: "27999000000"),
            new("Dup One", "dup1@agencia.com.br", Phone: "27999000001"),
            new("Dup Two", "dup2@agencia.com.br", Phone: "27999000002"),
        };
        var request = new BulkImportRequest(
            rows,
            LeadSource.Scraping,
            RejectionCounts: new ImportRejectionCounts(GeoRejected: 3, LowQualityEmailRejected: 2, NoMxRejected: 1));

        CustomerImport? captured = null;
        _importRepoMock
            .Setup(r => r.AddAsync(It.IsAny<CustomerImport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerImport ci, CancellationToken _) => { captured = ci; return ci; });

        // Act
        var result = await _sut.ImportAsync(request, "test.json");

        // Assert — contadores de rejeição pré-computados chegaram intactos...
        Assert.NotNull(captured);
        Assert.Equal(3, captured!.GeoRejectedCount);
        Assert.Equal(2, captured.LowQualityEmailRejectedCount);
        Assert.Equal(1, captured.NoMxRejectedCount);
        Assert.Equal(2, captured.DuplicateRejectedCount);
        // ...e sobrevivem ao Complete() (que grava Success/Failed/Status por cima do mesmo import).
        Assert.Equal(1, result.SuccessCount);   // só o lead novo criou
        Assert.Equal(2, result.SkippedCount);   // os 2 duplicados, sem nada novo, foram ignorados
    }

    [Fact]
    public async Task Import_CreateCustomer_WithDirectoryWebsite_SetsWebsiteKindDirectory()
    {
        var row = new ImportCustomerRow("Carlos", "carlos@diretorio.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido",
            Website = "https://econodata.com.br/x"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        Customer? savedCustomer = null;
        _customerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => savedCustomer = c)
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        await _sut.ImportAsync(request, "test.json");

        Assert.NotNull(savedCustomer);
        Assert.Equal("https://econodata.com.br/x", savedCustomer!.Website);
        Assert.Equal(WebsiteKind.Directory, savedCustomer.WebsiteKind);
    }

    [Fact]
    public async Task Import_CreateCustomer_WithOwnSiteWebsite_SetsWebsiteKindOwnSite()
    {
        var row = new ImportCustomerRow("Carlos", "carlos@clinicaodonto.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido",
            Website = "https://clinicaodonto.com.br"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        Customer? savedCustomer = null;
        _customerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => savedCustomer = c)
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        await _sut.ImportAsync(request, "test.json");

        Assert.NotNull(savedCustomer);
        Assert.Equal(WebsiteKind.OwnSite, savedCustomer!.WebsiteKind);
    }

    [Fact]
    public async Task Import_CreateCustomer_WithoutWebsite_SetsWebsiteKindUnknown()
    {
        var row = new ImportCustomerRow("Carlos", "carlos@agencia.com.br")
        {
            ValidationStatus = "valido",
            ConsentStatus = "consentido"
        };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Import);

        Customer? savedCustomer = null;
        _customerRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => savedCustomer = c)
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        await _sut.ImportAsync(request, "test.json");

        Assert.NotNull(savedCustomer);
        Assert.Equal(WebsiteKind.Unknown, savedCustomer!.WebsiteKind);
    }

    [Fact]
    public async Task Import_EnrichExistingCustomerWithoutWebsite_ReceivesDirectoryWebsite_SetsWebsiteKindDirectory()
    {
        // Fonte precisa ser Scraping — LeadSource.Import rejeita duplicata antes de enriquecer.
        var existing = new Customer("Existing", "enrich@agencia.com.br");
        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("enrich@agencia.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var row = new ImportCustomerRow("Existing", "enrich@agencia.com.br") { Website = "linktr.ee/x" };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Scraping);

        var result = await _sut.ImportAsync(request, "test.json");

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal("linktr.ee/x", existing.Website);
        Assert.Equal(WebsiteKind.Directory, existing.WebsiteKind);
    }

    [Fact]
    public async Task Import_EnrichExistingCustomerWithWebsite_PreservesWebsite_RecalculatesWebsiteKind()
    {
        // Customer legado importado antes desta fase: já tem website mas nunca teve WebsiteKind
        // calculado (fica Unknown por default). Um novo pull deve recalcular a partir do
        // website FINAL, que é o já existente (preservado, não sobrescrito pelo da linha).
        var existing = new Customer("Existing", "legado@agencia.com.br");
        existing.UpdateContactInfo(website: "https://clinicaodonto.com.br");
        Assert.Equal(WebsiteKind.Unknown, existing.WebsiteKind); // baseline antes do enrich

        _customerRepoMock
            .Setup(r => r.GetByEmailAsync("legado@agencia.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var row = new ImportCustomerRow("Existing", "legado@agencia.com.br") { Website = "https://outrositequalquer.com.br" };
        var request = new BulkImportRequest(new List<ImportCustomerRow> { row }, LeadSource.Scraping);

        var result = await _sut.ImportAsync(request, "test.json");

        Assert.Equal(1, result.SuccessCount);
        // Website existente preservado (a regra de enrich só usa o novo se o existente for vazio).
        Assert.Equal("https://clinicaodonto.com.br", existing.Website);
        Assert.Equal(WebsiteKind.OwnSite, existing.WebsiteKind);
    }
}
