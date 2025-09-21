namespace Booking.Application.Utility
{
    public static class DateRange
    {
        public static IEnumerable<DateOnly> Closed(DateOnly start, DateOnly end)
        {
            if (end < start) yield break;
            for (var d = start; d <= end; d = d.AddDays(1)) yield return d;
        }
    }
}
