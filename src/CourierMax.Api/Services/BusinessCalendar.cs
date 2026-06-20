namespace CourierMax.Api.Services;

public interface IBusinessCalendar
{
    int CountBusinessDaysAfterStart(DateOnly startDate, DateOnly endDate);
}

public sealed class ColombianBusinessCalendar : IBusinessCalendar
{
    private static readonly HashSet<DateOnly> Holidays2026 =
    [
        new(2026, 1, 1),
        new(2026, 1, 26),
        new(2026, 1, 30),
        new(2026, 3, 24),
        new(2026, 5, 1),
        new(2026, 6, 1),
        new(2026, 6, 29),
        new(2026, 7, 20),
        new(2026, 8, 17),
        new(2026, 10, 20),
        new(2026, 11, 9),
        new(2026, 12, 8)
    ];

    public int CountBusinessDaysAfterStart(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
        {
            return 0;
        }

        var days = 0;
        for (var date = startDate.AddDays(1); date <= endDate; date = date.AddDays(1))
        {
            if (IsBusinessDay(date))
            {
                days++;
            }
        }

        return days;
    }

    private static bool IsBusinessDay(DateOnly date)
    {
        return date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday
            && !Holidays2026.Contains(date);
    }
}
