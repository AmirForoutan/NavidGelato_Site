namespace NavidGelato_Site.Services;

public static class IranClock
{
    private static readonly TimeZoneInfo IranTimeZone = ResolveIranTimeZone();

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IranTimeZone);

    public static DateTime FromUtc(DateTime utc)
    {
        var value = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(value, IranTimeZone);
    }

    public static DateTime ToUtc(DateTime iranLocal)
    {
        var value = DateTime.SpecifyKind(iranLocal, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(value, IranTimeZone);
    }

    private static TimeZoneInfo ResolveIranTimeZone()
    {
        foreach (var id in new[] { "Iran Standard Time", "Asia/Tehran" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("IranFallback", TimeSpan.FromHours(3.5), "Iran", "Iran");
    }
}
