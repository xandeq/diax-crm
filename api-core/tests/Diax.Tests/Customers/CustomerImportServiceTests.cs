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
}
