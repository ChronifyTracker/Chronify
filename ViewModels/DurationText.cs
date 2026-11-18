namespace Chronify.ViewModels;

public static class DurationText
{
    private const int MinutesInHour = 60;
    private const int MinutesInDay = 24 * MinutesInHour;

    public static string From(int minutes)
    {
        if (minutes <= 0)
            return "";

        if (minutes < 10)
            return $"{minutes} мин";

        var toFiveMinutes = RoundTo(minutes, 5);
        if (toFiveMinutes < MinutesInHour)
            return $"{toFiveMinutes} мин";

        var toHalfHour = RoundTo(minutes, 30);
        if (toHalfHour < 5 * MinutesInHour)
        {
            var wholeHours = toHalfHour / MinutesInHour;
            var hasHalf = toHalfHour % MinutesInHour != 0;

            return hasHalf ? $"{wholeHours},5 ч" : $"{wholeHours} ч";
        }

        var toHour = RoundTo(minutes, MinutesInHour);
        if (toHour < 2 * MinutesInDay)
            return $"{toHour / MinutesInHour} ч";

        var toDay = RoundTo(minutes, MinutesInDay);
        return $"{toDay / MinutesInDay} дн";
    }

    private static int RoundTo(int value, int step)
    {
        return (value + step / 2) / step * step;
    }
}
