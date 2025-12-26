namespace Diax.Shared.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToBrazilianTime(this DateTime dateTime)
    {
        var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), brasiliaTimeZone);
    }

    public static DateTime ToUtcFromBrazilian(this DateTime dateTime)
    {
        var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        return TimeZoneInfo.ConvertTimeToUtc(dateTime, brasiliaTimeZone);
    }

    public static bool IsWeekend(this DateTime dateTime) =>
        dateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public static bool IsBusinessDay(this DateTime dateTime) =>
        !dateTime.IsWeekend();
}
