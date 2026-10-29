using System.Linq.Expressions;
using Diax.Application.EmailMarketing.Pro;
using Diax.Application.PromptGenerator;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Shared.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.Application.Customers;

/// <summary>
/// Regressão: Repository.UpdateAsync só marca a entidade, não persiste. O serviço de normalização
/// nunca chamava SaveChanges, então "Rodar normalização" e "Aprovar nome" não gravavam nada
/// (prod: 17k customers, 0 normalizados).
/// </summary>
public class LeadNormalizationPersistenceTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<INameNormalizationService> _normalizer = new();

    private LeadNormalizationService Sut() => new(
        _repo.Object, _uow.Object, _normalizer.Object,
        Array.Empty<IAiTextTransformClient>(), new PromptGeneratorSettings(),
        NullLogger<LeadNormalizationService>.Instance);

    public LeadNormalizationPersistenceTests()
    {
        _normalizer.Setup(n => n.Normalize(It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns((string? raw, string? _) => new NameNormalizationResult(
                (raw ?? "").Split(' ')[0], 90, NormalizationSource.Deterministic));
    }

    [Fact]
    public async Task RunBatch_NotForced_LoadsOnlyPendingFromRepository_AndSaves()
    {
        var pending = new[] { new Customer("joao silva", "j@x.com"), new Customer("maria souza", "m@x.com") };
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        var result = await Sut().RunBatchAsync(forceReprocess: false);

        Assert.Equal(2, result.Updated);
        Assert.All(pending, c => Assert.NotNull(c.NormalizedName));
        _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunBatch_Forced_ReprocessesAll_AndSaves()
    {
        var done = new Customer("ana lima", "a@x.com");
        done.ApplyNormalization("Ana", "manual", 100, NormalizationSource.Manual);
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { done });

        var result = await Sut().RunBatchAsync(forceReprocess: true);

        Assert.Equal(1, result.Processed);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunBatch_NothingUpdated_DoesNotSave()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Customer>());

        await Sut().RunBatchAsync();

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Approve_PersistsTheManualName()
    {
        var c = new Customer("xpto ltda", "x@x.com");
        _repo.Setup(r => r.GetByIdAsync(c.Id, It.IsAny<CancellationToken>())).ReturnsAsync(c);

        await Sut().ApproveAsync(c.Id, "  Carlos ");

        Assert.Equal("Carlos", c.NormalizedName);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
