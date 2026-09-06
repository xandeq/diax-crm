using System;
using System.Linq;
using System.Threading.Tasks;
using Diax.Application.Common;
using Diax.Application.Customers.Dtos;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Diax.Tests.Customers;

/// <summary>
/// Cobertura do filtro de período (from/to) em ICustomerImportRepository.GetPagedAsync e do
/// mapeamento dos 4 contadores de rejeição em ImportHistoryResponse.FromEntity (EXTR-02, plano
/// 07-06).
/// </summary>
public class ImportHistoryQueryTests
{
    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId) { UserId = userId; }
        public Guid? UserId { get; }
        public bool IsAuthenticated => UserId.HasValue;
    }

    private static DiaxDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseInMemoryDatabase($"import-history-{Guid.NewGuid()}")
            .Options;
        return new DiaxDbContext(options, new FakeCurrentUserService(Guid.NewGuid()));
    }

    /// <summary>
    /// Cria um CustomerImport e força seu CreatedAt para uma data determinística, usando a API
    /// de tracking do próprio EF Core (Entry(...).Property(...).CurrentValue) — não é reflexão
    /// ad-hoc de teste, é o mecanismo padrão do EF para popular propriedades com setter protegido.
    /// </summary>
    private static CustomerImport SeedImport(DiaxDbContext db, DateTime createdAtUtc, string fileName = "rodada.csv")
    {
        var import = new CustomerImport(fileName, ImportType.CSV, totalRecords: 10);
        db.CustomerImports.Add(import);
        db.Entry(import).Property(nameof(CustomerImport.CreatedAt)).CurrentValue = createdAtUtc;
        return import;
    }

    // ===== Filtro de período =====

    [Fact]
    public async Task GetPagedAsync_NoFromNoTo_ReturnsAll()
    {
        using var db = CreateDbContext();
        SeedImport(db, new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc));
        SeedImport(db, new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync();

        var repository = new CustomerImportRepository(db);
        var (items, totalCount) = await repository.GetPagedAsync(1, 20);

        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetPagedAsync_WithFrom_ReturnsOnlyRoundsOnOrAfter()
    {
        using var db = CreateDbContext();
        SeedImport(db, new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc), "rodada-31-08.csv");
        SeedImport(db, new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc), "rodada-02-09.csv");
        await db.SaveChangesAsync();

        var repository = new CustomerImportRepository(db);
        var (items, totalCount) = await repository.GetPagedAsync(
            1, 20, from: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, totalCount);
        Assert.Equal("rodada-02-09.csv", Assert.Single(items).FileName);
    }

    [Fact]
    public async Task GetPagedAsync_WithTo_ReturnsOnlyRoundsOnOrBefore()
    {
        using var db = CreateDbContext();
        SeedImport(db, new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc), "rodada-31-08.csv");
        SeedImport(db, new DateTime(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc), "rodada-02-09.csv");
        await db.SaveChangesAsync();

        var repository = new CustomerImportRepository(db);
        var (items, totalCount) = await repository.GetPagedAsync(
            1, 20, to: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, totalCount);
        Assert.Equal("rodada-31-08.csv", Assert.Single(items).FileName);
    }

    [Fact]
    public async Task GetPagedAsync_FromAndToSameDay_ReturnsRoundsInclusiveOfBorders()
    {
        using var db = CreateDbContext();
        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 9, 1, 23, 59, 59, DateTimeKind.Utc);

        SeedImport(db, from, "borda-inicio.csv");             // borda inicial exata
        SeedImport(db, new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc), "meio-do-dia.csv");
        SeedImport(db, to, "borda-fim.csv");                    // borda final exata
        SeedImport(db, new DateTime(2026, 9, 2, 0, 0, 1, DateTimeKind.Utc), "fora-do-periodo.csv");
        await db.SaveChangesAsync();

        var repository = new CustomerImportRepository(db);
        var (items, totalCount) = await repository.GetPagedAsync(1, 20, from, to);

        Assert.Equal(3, totalCount);
        Assert.DoesNotContain(items, x => x.FileName == "fora-do-periodo.csv");
    }

    [Fact]
    public async Task GetPagedAsync_TotalCountReflectsFilteredTotal_NotTableTotal()
    {
        using var db = CreateDbContext();
        SeedImport(db, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedImport(db, new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc));
        SeedImport(db, new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync();

        var repository = new CustomerImportRepository(db);
        var (items, totalCount) = await repository.GetPagedAsync(
            1, pageSize: 1, from: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

        // pageSize=1 corta os itens retornados, mas o total filtrado continua 1 (só há 1 rodada
        // de setembro) — não o total da tabela inteira (3).
        Assert.Equal(1, totalCount);
        Assert.Single(items);
    }

    // ===== Mapeamento dos 4 contadores em ImportHistoryResponse =====

    [Fact]
    public void FromEntity_MapsAllFourRejectionCounters()
    {
        var import = new CustomerImport("rodada.csv", ImportType.CSV, totalRecords: 20);
        import.RecordRejectionCounts(geo: 3, lowQualityEmail: 2, noMx: 1, duplicate: 7);
        import.Complete(successCount: 7, failedCount: 0, errors: null);

        var response = ImportHistoryResponse.FromEntity(import);

        Assert.Equal(3, response.GeoRejectedCount);
        Assert.Equal(2, response.LowQualityEmailRejectedCount);
        Assert.Equal(1, response.NoMxRejectedCount);
        Assert.Equal(7, response.DuplicateRejectedCount);
    }

    [Fact]
    public void FromEntity_KeepsAllNineOriginalPropertiesCorrect()
    {
        var import = new CustomerImport("rodada-original.csv", ImportType.CSV, totalRecords: 15);
        import.RecordRejectionCounts(geo: 1, lowQualityEmail: 1, noMx: 1, duplicate: 1);
        import.Complete(successCount: 10, failedCount: 5, errors: "[{\"row\":1}]");

        var response = ImportHistoryResponse.FromEntity(import);

        Assert.Equal(import.Id, response.Id);
        Assert.Equal("rodada-original.csv", response.FileName);
        Assert.Equal(ImportType.CSV.ToString(), response.Type);
        Assert.Equal(import.Status.ToString(), response.Status);
        Assert.Equal(15, response.TotalRecords);
        Assert.Equal(10, response.SuccessCount);
        Assert.Equal(5, response.FailedCount);
        Assert.Equal("[{\"row\":1}]", response.ErrorDetails);
        Assert.Equal(import.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public void FromEntity_WithoutRecordRejectionCounts_DefaultsToZero()
    {
        var import = new CustomerImport("rodada-sem-rejeicao.csv", ImportType.CSV, totalRecords: 5);
        import.Complete(successCount: 5, failedCount: 0, errors: null);

        var response = ImportHistoryResponse.FromEntity(import);

        Assert.Equal(0, response.GeoRejectedCount);
        Assert.Equal(0, response.LowQualityEmailRejectedCount);
        Assert.Equal(0, response.NoMxRejectedCount);
        Assert.Equal(0, response.DuplicateRejectedCount);
    }

    [Fact]
    public void FromEntity_NegativeCountersAreNormalizedToZero()
    {
        var import = new CustomerImport("rodada-negativa.csv", ImportType.CSV, totalRecords: 5);
        import.RecordRejectionCounts(geo: -1, lowQualityEmail: -2, noMx: -3, duplicate: -4);
        import.Complete(successCount: 5, failedCount: 0, errors: null);

        var response = ImportHistoryResponse.FromEntity(import);

        Assert.Equal(0, response.GeoRejectedCount);
        Assert.Equal(0, response.LowQualityEmailRejectedCount);
        Assert.Equal(0, response.NoMxRejectedCount);
        Assert.Equal(0, response.DuplicateRejectedCount);
    }
}
