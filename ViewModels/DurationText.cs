namespace Chronify.ViewModels;

public static class DurationText
{
    private const int MinutesInHour = 60;
    private const int MinutesInDay = 24 * MinutesInHour;

    public static string From(int minutes)
    {
        if (minutes <= 0)
        {
            return "";
        }

        if (minutes < 10)
        {
            return Texts.Format("Duration.Minutes", minutes);
        }

        var toFiveMinutes = RoundTo(minutes, 5);
        if (toFiveMinutes < MinutesInHour)
        {
            return Texts.Format("Duration.Minutes", toFiveMinutes);
        }

        var toHalfHour = RoundTo(minutes, 30);
        if (toHalfHour < 5 * MinutesInHour)
        {
            var wholeHours = toHalfHour / MinutesInHour;
            var hasHalf = toHalfHour % MinutesInHour != 0;

            return hasHalf ? Texts.Format("Duration.HalfHours", wholeHours) : Texts.Format("Duration.Hours", wholeHours);
        }

        var toHour = RoundTo(minutes, MinutesInHour);
        if (toHour < 2 * MinutesInDay)
        {
            return Texts.Format("Duration.Hours", toHour / MinutesInHour);
        }

        var toDay = RoundTo(minutes, MinutesInDay);
        return Texts.Format("Duration.Days", toDay / MinutesInDay);
    }

    private static int RoundTo(int value, int step)
    {
        return (value + step / 2) / step * step;
    }
}
