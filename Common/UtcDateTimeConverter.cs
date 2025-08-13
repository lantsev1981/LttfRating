namespace Common;

public class UtcDateTimeConverter() : ValueConverter<DateTimeOffset, DateTime>(
    write => write.UtcDateTime,
    read => new DateTimeOffset(DateTime.SpecifyKind(read, DateTimeKind.Utc), TimeSpan.FromHours(0)))
{
    private static readonly Lazy<ValueConverter<DateTimeOffset, DateTime>> _instance =
        new(() => new UtcDateTimeConverter());

    public static ValueConverter<DateTimeOffset, DateTime> Instance => _instance.Value;
}