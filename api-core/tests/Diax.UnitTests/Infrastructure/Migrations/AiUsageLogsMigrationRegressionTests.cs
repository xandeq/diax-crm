using Diax.Domain.AI;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Configurations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Diax.UnitTests.Infrastructure.Migrations;

public class AiUsageLogsMigrationRegressionTests
{
    private readonly IModel _model;

    public AiUsageLogsMigrationRegressionTests()
    {
        var conventionSet = SqlServerConventionSetBuilder.Build();
        var modelBuilder = new ModelBuilder(conventionSet);

        // Apply the AiUsageLog configuration to get the model EF Core would generate
        new AiUsageLogConfiguration().Configure(modelBuilder.Entity<AiUsageLog>());

        _model = modelBuilder.FinalizeModel();
    }

    [Fact]
    public void AiUsageLog_TableName_IsCorrect()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("ai_usage_logs");
    }

    [Fact]
    public void AiUsageLog_HasAllRequiredProperties()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        entityType.Should().NotBeNull();

        var propertyNames = entityType!.GetProperties().Select(p => p.Name).ToList();

        propertyNames.Should().Contain("Id");
        propertyNames.Should().Contain("UserId");
        propertyNames.Should().Contain("ProviderId");
        propertyNames.Should().Contain("ModelId");
        propertyNames.Should().Contain("TokensInput");
        propertyNames.Should().Contain("TokensOutput");
        propertyNames.Should().Contain("TotalTokens");
        propertyNames.Should().Contain("CostEstimated");
        propertyNames.Should().Contain("RequestType");
        propertyNames.Should().Contain("CreatedAt");
    }

    [Fact]
    public void AiUsageLog_UserId_IsNullable()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var userIdProp = entityType!.FindProperty("UserId");

        userIdProp.Should().NotBeNull();
        userIdProp!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void AiUsageLog_ProviderId_IsRequired()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var providerIdProp = entityType!.FindProperty("ProviderId");

        providerIdProp.Should().NotBeNull();
        providerIdProp!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void AiUsageLog_ModelId_IsRequired()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var modelIdProp = entityType!.FindProperty("ModelId");

        modelIdProp.Should().NotBeNull();
        modelIdProp!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void AiUsageLog_CostEstimated_HasHighPrecision()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var costProp = entityType!.FindProperty("CostEstimated");

        costProp.Should().NotBeNull();
        costProp!.GetPrecision().Should().Be(18);
        costProp.GetScale().Should().Be(6);
    }

    [Fact]
    public void AiUsageLog_RequestType_HasMaxLength50()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var requestTypeProp = entityType!.FindProperty("RequestType");

        requestTypeProp.Should().NotBeNull();
        requestTypeProp!.GetMaxLength().Should().Be(50);
    }

    [Fact]
    public void AiUsageLog_CreatedAt_IsDatetime2()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var createdAtProp = entityType!.FindProperty("CreatedAt");

        createdAtProp.Should().NotBeNull();
        createdAtProp!.GetColumnType().Should().Be("datetime2");
    }

    [Fact]
    public void AiUsageLog_HasExpectedIndexes()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var indexes = entityType!.GetIndexes().ToList();

        var indexNames = indexes
            .Select(i => i.GetDatabaseName())
            .ToList();

        indexNames.Should().Contain("IX_AiUsageLogs_CreatedAt");
        indexNames.Should().Contain("IX_AiUsageLogs_ModelId");
        indexNames.Should().Contain("IX_AiUsageLogs_ProviderId");
        indexNames.Should().Contain("IX_AiUsageLogs_ModelId_CreatedAt");
        indexNames.Should().Contain("IX_AiUsageLogs_ProviderId_CreatedAt");
        indexNames.Should().Contain("IX_AiUsageLogs_UserId_CreatedAt");
    }

    [Fact]
    public void AiUsageLog_HasSixIndexes()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().HaveCount(6);
    }

    [Fact]
    public void AiUsageLog_ForeignKeys_UseRestrictDeleteBehavior()
    {
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var foreignKeys = entityType!.GetForeignKeys().ToList();

        foreach (var fk in foreignKeys)
        {
            fk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict,
                $"FK to {fk.PrincipalEntityType.Name} should use Restrict to preserve historical data");
        }
    }

    [Fact]
    public void AiUsageLog_Entity_MatchesMigrationColumns()
    {
        // Regression test: entity properties must match what the migration creates
        var entityType = _model.FindEntityType(typeof(AiUsageLog));
        var properties = entityType!.GetProperties().ToList();

        // The migration creates exactly these columns
        var expectedColumns = new[]
        {
            "Id", "UserId", "ProviderId", "ModelId",
            "TokensInput", "TokensOutput", "TotalTokens",
            "CostEstimated", "RequestType", "CreatedAt"
        };

        properties.Select(p => p.Name).Should().BeEquivalentTo(expectedColumns);
    }
}
