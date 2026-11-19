namespace Chronify.Localization;

public static class Texts
{
    public static string Get(string key)
    {
        return LocalizationService.Current[key];
    }

    public static string Format(string key, params object?[] arguments)
    {
        return string.Format(LocalizationService.Current.Culture, Get(key), arguments);
    }

    public static string Date(DateTime value)
    {
        return value.ToString("d", LocalizationService.Current.Culture);
    }

    public static string DateTime(DateTime value)
    {
        return value.ToString("g", LocalizationService.Current.Culture);
    }

    public static string Role(Teams.TeamRole role)
    {
        string key = role switch
        {
            Teams.TeamRole.Administrator => "Roles.Administrator",
            Teams.TeamRole.TeamLead => "Roles.TeamLead",
            Teams.TeamRole.Executor => "Roles.Executor",
            _ => "Roles.None"
        };
        return Get(key);
    }
}
