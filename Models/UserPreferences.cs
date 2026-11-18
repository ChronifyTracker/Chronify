namespace Chronify;

public class UserPreferences
{
    public Guid UserId { get; set; }
    public int DailyLimitMinutes { get; set; } = 480;
    public int PlanLimitMinutes { get; set; } = 180;
}
