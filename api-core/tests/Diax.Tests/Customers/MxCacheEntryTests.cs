using Diax.Domain.Customers;
using Xunit;

namespace Diax.Tests.Customers;

public class MxCacheEntryTests
{
    [Fact]
    public void Constructor_SetsProperties_AndCheckedAtIsNow()
    {
        var before = DateTime.UtcNow;

        var entry = new MxCacheEntry("acme.com.br", 0);

        var after = DateTime.UtcNow;

        Assert.Equal("acme.com.br", entry.Domain);
        Assert.Equal(0, entry.ResultCode);
        Assert.InRange(entry.CheckedAt, before.AddSeconds(-1), after.AddSeconds(1));
    }

    [Fact]
    public void Constructor_NormalizesDomain_TrimAndLowercase()
    {
        var entry = new MxCacheEntry("  ACME.COM.BR ", 1);

        Assert.Equal("acme.com.br", entry.Domain);
    }

    [Fact]
    public void Refresh_UpdatesResultCodeAndCheckedAt()
    {
        var entry = new MxCacheEntry("acme.com.br", 0);

        entry.Refresh(2);

        Assert.Equal(2, entry.ResultCode);
        Assert.True(DateTime.UtcNow - entry.CheckedAt < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsFresh_ForRecentEntry_ReturnsTrue()
    {
        var entry = new MxCacheEntry("acme.com.br", 0);

        Assert.True(entry.IsFresh(DateTime.UtcNow, TimeSpan.FromDays(30)));
    }

    [Fact]
    public void IsFresh_ThirtyOneDaysLater_ReturnsFalse()
    {
        var entry = new MxCacheEntry("acme.com.br", 0);

        Assert.False(entry.IsFresh(DateTime.UtcNow.AddDays(31), TimeSpan.FromDays(30)));
    }

    [Fact]
    public void IsFresh_TwentyFiveHoursLater_WithTwentyFourHourWindow_ReturnsFalse()
    {
        var entry = new MxCacheEntry("acme.com.br", 0);

        Assert.False(entry.IsFresh(DateTime.UtcNow.AddHours(25), TimeSpan.FromHours(24)));
    }
}
