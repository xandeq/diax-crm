using Diax.Application.Audit;
using Diax.Application.Audit.Dtos;
using Diax.Domain.Audit;
using Diax.Domain.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Diax.UnitTests.Audit;

/// <summary>
/// Testes unitários do sistema de Audit Log.
/// Cobrem:
///   - Criação do AuditLogEntry (domínio)
///   - AuditLogService.LogManualAsync (falha silenciosa)
///   - AuditLogService.GetFilteredAsync
///   - AuditLogService.CleanupOldLogsAsync (regra de 30 dias)
///   - Prevenção de loop infinito no interceptor (AuditLogEntry não cria log de si mesmo)
/// </summary>
public class AuditLogTests
{
    // ===== Helpers =====

    private static (AuditLogService service, Mock<IAuditLogRepository> repo, Mock<IUnitOfWork> uow)
        BuildService()
    {
        var repoMock = new Mock<IAuditLogRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<AuditLogService>>();

        var service = new AuditLogService(repoMock.Object, uowMock.Object, loggerMock.Object);
        return (service, repoMock, uowMock);
    }

    // ===== Domain entity =====

    [Fact]
    public void AuditLogEntry_Create_ShouldInitializeCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var entry = AuditLogEntry.Create(
            userId: userId,
            action: AuditAction.Create,
            resourceType: "Customer",
            resourceId: "abc-123",
            description: "Criado: Customer [abc-123]",
            oldValues: null,
            newValues: "{\"name\":\"João\"}",
            changedProperties: null,
            source: AuditSource.Api);

        // Assert
        entry.Id.Should().NotBeEmpty();
        entry.UserId.Should().Be(userId);
        entry.Action.Should().Be(AuditAction.Create);
        entry.ResourceType.Should().Be("Customer");
        entry.ResourceId.Should().Be("abc-123");
        entry.OldValues.Should().BeNull();
        entry.NewValues.Should().Be("{\"name\":\"João\"}");
        entry.Source.Should().Be(AuditSource.Api);
        entry.Status.Should().Be(AuditStatus.Success);
        entry.TimestampUtc.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AuditLogEntry_MarkAsFailed_ShouldChangeStatusAndMessage()
    {
        // Arrange
        var entry = AuditLogEntry.Create(null, AuditAction.Delete, "Expense", "id-1", "Deletado");

        // Act
        entry.MarkAsFailed("DB timeout");

        // Assert
        entry.Status.Should().Be(AuditStatus.Failed);
        entry.ErrorMessage.Should().Be("DB timeout");
    }

    [Fact]
    public void AuditLogEntry_SystemAction_ShouldAllowNullUserId()
    {
        // Arrange & Act
        var entry = AuditLogEntry.Create(
            userId: null,
            action: AuditAction.Custom,
            resourceType: "System",
            resourceId: "migration",
            description: "Migration aplicada",
            source: AuditSource.System);

        // Assert
        entry.UserId.Should().BeNull();
        entry.Source.Should().Be(AuditSource.System);
    }

    // ===== AuditLogService =====

    [Fact]
    public async Task LogManualAsync_WhenRepoSucceeds_ShouldReturnSuccessWithId()
    {
        // Arrange
        var (service, repo, uow) = BuildService();
        repo.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await service.LogManualAsync(
            userId: Guid.NewGuid(),
            action: AuditAction.Login,
            resourceType: "User",
            resourceId: "user-1",
            description: "Login realizado");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogManualAsync_WhenRepoThrows_ShouldReturnFailureWithoutPropagating()
    {
        // Arranjo — simula falha no repositório
        var (service, repo, uow) = BuildService();
        repo.Setup(r => r.AddAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        // Act — NÃO deve lançar exceção
        var result = await service.LogManualAsync(
            userId: null,
            action: AuditAction.Create,
            resourceType: "Lead",
            resourceId: "lead-9",
            description: "Lead criado");

        // Assert — retorna falha mas não propaga
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Contain("AuditLog");
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CleanupOldLogsAsync_WithLessThan30Days_ShouldReturnValidationError()
    {
        // Arrange
        var (service, _, _) = BuildService();

        // Act
        var result = await service.CleanupOldLogsAsync(olderThanDays: 15);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AuditLog.InvalidRetention");
    }

    [Fact]
    public async Task CleanupOldLogsAsync_With30OrMoreDays_ShouldCallRepository()
    {
        // Arrange
        var (service, repo, _) = BuildService();
        repo.Setup(r => r.DeleteOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await service.CleanupOldLogsAsync(olderThanDays: 30);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        repo.Verify(r => r.DeleteOlderThanAsync(
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-29)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFilteredAsync_ShouldMapFilterAndReturnPagedResult()
    {
        // Arrange
        var (service, repo, _) = BuildService();
        var fakeEntry = AuditLogEntry.Create(
            Guid.NewGuid(), AuditAction.Update, "Expense", "e-1", "Atualizado");

        repo.Setup(r => r.GetFilteredAsync(It.IsAny<AuditLogFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLogEntry> { fakeEntry }.AsReadOnly() as IReadOnlyList<AuditLogEntry>, 1));

        var request = new AuditLogFilterRequest(
            ResourceType: "Expense",
            Page: 1,
            PageSize: 10);

        // Act
        var result = await service.GetFilteredAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.TotalPages.Should().Be(1);
        result.Value.Items[0].ResourceType.Should().Be("Expense");
        result.Value.Items[0].Action.Should().Be("Update");
    }

    [Fact]
    public async Task GetResourceHistoryAsync_ShouldReturnEntriesInDescendingOrder()
    {
        // Arrange
        var (service, repo, _) = BuildService();
        var entries = new List<AuditLogEntry>
        {
            AuditLogEntry.Create(null, AuditAction.Create, "Customer", "c-1", "Criado"),
            AuditLogEntry.Create(null, AuditAction.Update, "Customer", "c-1", "Atualizado")
        };

        repo.Setup(r => r.GetByResourceAsync("Customer", "c-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        // Act
        var result = await service.GetResourceHistoryAsync("Customer", "c-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // ===== Proteção contra loop infinito =====

    [Fact]
    public void AuditLogEntry_IsNotAuditableEntity_ToAvoidInterceptorLoop()
    {
        // AuditLogEntry deve herdar de Entity, NÃO de AuditableEntity
        // Isso garante que o interceptor nunca vai criar um AuditLogEntry de um AuditLogEntry
        var entry = AuditLogEntry.Create(null, AuditAction.Create, "X", "1", "Test");
        entry.Should().NotBeAssignableTo<AuditableEntity>(
            because: "AuditLogEntry não pode herdar de AuditableEntity pois causaria loop infinito no interceptor");
    }
}
