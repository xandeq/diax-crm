using Diax.Application.Customers;
using Diax.Domain.Customers;

namespace Diax.Tests.Application.Customers;

public class MeetingAvailabilityTests
{
    // Quarta-feira 2026-07-08 10:00 BRT = 13:00 UTC
    private static readonly DateTime WednesdayUtc = new(2026, 7, 8, 13, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ComputeAvailableSlots_OnlyBusinessHoursWeekdays()
    {
        var slots = MeetingService.ComputeAvailableSlots(WednesdayUtc, Array.Empty<DateTime>(), 7);

        Assert.NotEmpty(slots);
        foreach (var slot in slots)
        {
            var brt = slot + MeetingService.BrtOffset;
            Assert.True(brt.Hour >= MeetingService.BusinessStartHourBrt, $"antes das 9h BRT: {brt}");
            Assert.True(brt.Hour < MeetingService.BusinessEndHourBrt, $"depois das 18h BRT: {brt}");
            Assert.NotEqual(DayOfWeek.Saturday, brt.DayOfWeek);
            Assert.NotEqual(DayOfWeek.Sunday, brt.DayOfWeek);
            Assert.True(slot.Minute is 0 or 30);
        }
    }

    [Fact]
    public void ComputeAvailableSlots_RespectsLeadTime()
    {
        var slots = MeetingService.ComputeAvailableSlots(WednesdayUtc, Array.Empty<DateTime>(), 2);
        var earliest = WednesdayUtc + MeetingService.MinimumLeadTime;
        Assert.All(slots, s => Assert.True(s >= earliest));
        // 10h BRT + 4h lead = 14h BRT → slots de hoje começam 14:00 BRT
        var todaySlots = slots.Where(s => (s + MeetingService.BrtOffset).Date == new DateTime(2026, 7, 8)).ToList();
        Assert.NotEmpty(todaySlots);
        Assert.Equal(14, ((todaySlots.Min() + MeetingService.BrtOffset)).Hour);
    }

    [Fact]
    public void ComputeAvailableSlots_ExcludesBookedSlots()
    {
        var all = MeetingService.ComputeAvailableSlots(WednesdayUtc, Array.Empty<DateTime>(), 3);
        var taken = all.Take(3).ToList();
        var remaining = MeetingService.ComputeAvailableSlots(WednesdayUtc, taken, 3);

        Assert.Equal(all.Count - 3, remaining.Count);
        Assert.All(taken, t => Assert.DoesNotContain(t, remaining));
    }

    [Fact]
    public void ComputeAvailableSlots_FridayEvening_SkipsToMonday()
    {
        // Sexta 2026-07-10 17:00 BRT = 20:00 UTC; lead time 4h → sábado/domingo pulados
        var fridayLate = new DateTime(2026, 7, 10, 20, 0, 0, DateTimeKind.Utc);
        var slots = MeetingService.ComputeAvailableSlots(fridayLate, Array.Empty<DateTime>(), 4);

        Assert.NotEmpty(slots);
        var firstBrt = slots.Min() + MeetingService.BrtOffset;
        Assert.Equal(DayOfWeek.Monday, firstBrt.DayOfWeek);
    }

    [Fact]
    public void ComputeAvailableSlots_FullDay_Has18SlotsPerDay()
    {
        // 9h-18h = 9 horas × 2 slots = 18 slots por dia útil completo
        var slots = MeetingService.ComputeAvailableSlots(WednesdayUtc, Array.Empty<DateTime>(), 7);
        var byDay = slots.GroupBy(s => (s + MeetingService.BrtOffset).Date)
            .Where(g => g.Key > new DateTime(2026, 7, 8)); // dias completos (sem lead time)
        Assert.All(byDay, g => Assert.Equal(18, g.Count()));
    }
}

public class MeetingEntityTests
{
    [Fact]
    public void Ctor_NormalizesEmail_AndValidates()
    {
        var m = new Meeting(Guid.NewGuid(), DateTime.UtcNow.AddDays(1), "João", "JOAO@X.COM", null, null);
        Assert.Equal("joao@x.com", m.ContactEmail);
        Assert.Equal(MeetingStatus.Confirmed, m.Status);
        Assert.Equal(30, m.DurationMinutes);
    }

    [Theory]
    [InlineData("", "a@b.com")]
    [InlineData("Nome", "sem-arroba")]
    [InlineData("Nome", "")]
    public void Ctor_RejectsInvalidInput(string name, string email)
    {
        Assert.Throws<ArgumentException>(() =>
            new Meeting(Guid.NewGuid(), DateTime.UtcNow, name, email, null, null));
    }

    [Fact]
    public void Cancel_Completed_Throws()
    {
        var m = new Meeting(Guid.NewGuid(), DateTime.UtcNow, "N", "a@b.com", null, null);
        m.Complete();
        Assert.Throws<InvalidOperationException>(() => m.Cancel());
    }
}
