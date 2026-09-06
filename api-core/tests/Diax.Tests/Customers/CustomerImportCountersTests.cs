using Diax.Domain.Customers;
using Diax.Domain.Customers.Enums;
using Xunit;

namespace Diax.Tests.Customers;

public class CustomerImportCountersTests
{
    [Fact]
    public void NewImport_CountersStartAtZero()
    {
        var import = new CustomerImport("f", ImportType.CSV, 10);

        Assert.Equal(0, import.GeoRejectedCount);
        Assert.Equal(0, import.LowQualityEmailRejectedCount);
        Assert.Equal(0, import.NoMxRejectedCount);
        Assert.Equal(0, import.DuplicateRejectedCount);
    }

    [Fact]
    public void RecordRejectionCounts_SetsAllFourCounters()
    {
        var import = new CustomerImport("f", ImportType.CSV, 10);

        import.RecordRejectionCounts(geo: 5, lowQualityEmail: 3, noMx: 2, duplicate: 7);

        Assert.Equal(5, import.GeoRejectedCount);
        Assert.Equal(3, import.LowQualityEmailRejectedCount);
        Assert.Equal(2, import.NoMxRejectedCount);
        Assert.Equal(7, import.DuplicateRejectedCount);
    }

    [Fact]
    public void RecordRejectionCounts_NegativeValues_NormalizeToZero()
    {
        var import = new CustomerImport("f", ImportType.CSV, 10);

        import.RecordRejectionCounts(geo: -1, lowQualityEmail: -5, noMx: -2, duplicate: -3);

        Assert.Equal(0, import.GeoRejectedCount);
        Assert.Equal(0, import.LowQualityEmailRejectedCount);
        Assert.Equal(0, import.NoMxRejectedCount);
        Assert.Equal(0, import.DuplicateRejectedCount);
    }

    [Fact]
    public void RecordRejectionCounts_DoesNotChangeStatusOrSuccessOrFailedCount()
    {
        var import = new CustomerImport("f", ImportType.CSV, 10);
        var statusBefore = import.Status;

        import.RecordRejectionCounts(geo: 1, lowQualityEmail: 1, noMx: 1, duplicate: 1);

        Assert.Equal(statusBefore, import.Status);
        Assert.Equal(0, import.SuccessCount);
        Assert.Equal(0, import.FailedCount);
    }

    [Fact]
    public void Complete_AfterRecordRejectionCounts_PreservesTheFourCounters()
    {
        var import = new CustomerImport("f", ImportType.CSV, 10);
        import.RecordRejectionCounts(geo: 5, lowQualityEmail: 3, noMx: 2, duplicate: 7);

        import.Complete(successCount: 8, failedCount: 2, errors: null);

        Assert.Equal(5, import.GeoRejectedCount);
        Assert.Equal(3, import.LowQualityEmailRejectedCount);
        Assert.Equal(2, import.NoMxRejectedCount);
        Assert.Equal(7, import.DuplicateRejectedCount);
    }

    [Fact]
    public void NewCustomer_HasWebsiteKindUnknownAndNullExternalId()
    {
        var customer = new Customer("Acme");

        Assert.Equal(WebsiteKind.Unknown, customer.WebsiteKind);
        Assert.Null(customer.ExternalId);
    }

    [Fact]
    public void SetWebsiteKind_UpdatesWebsiteKind()
    {
        var customer = new Customer("Acme");

        customer.SetWebsiteKind(WebsiteKind.Directory);

        Assert.Equal(WebsiteKind.Directory, customer.WebsiteKind);
    }

    [Fact]
    public void SetExternalId_SetsTrimmedValue()
    {
        var customer = new Customer("Acme");

        customer.SetExternalId("12345");

        Assert.Equal("12345", customer.ExternalId);
    }

    [Fact]
    public void SetExternalId_WhitespaceBecomesNull()
    {
        var customer = new Customer("Acme");

        customer.SetExternalId("  ");

        Assert.Null(customer.ExternalId);
    }
}
