namespace Chronify.Localization;

// PostgreSQL повертає ці повідомлення незалежно від мови інтерфейсу.
public static class ServerErrors
{
    private static readonly Dictionary<string, string> Keys = new()
    {
        ["Проверьте почту и пароль (8–72 байта UTF-8)."] = "Errors.CloudCredentials",
        ["Имя: от 1 до 80 символов."] = "Errors.DisplayName",
        ["Неверная почта или пароль."] = "Errors.InvalidCredentials",
        ["Сессия истекла. Войдите в Chronify снова."] = "Errors.SessionExpired",
        ["Название группы: от 1 до 80 символов."] = "Errors.GroupName",
        ["Группа с таким кодом не найдена."] = "Errors.GroupNotFound",
        ["Нет доступа к группе."] = "Errors.GroupAccess",
        ["Роль ещё не назначена администратором."] = "Errors.RoleNotAssigned",
        ["Только администратор назначает исполнителя или тимлида."] = "Errors.RolePermission",
        ["Недостаточно прав для назначения этому участнику."] = "Errors.AssignmentPermission",
        ["Проверьте название, срок и статус."] = "Errors.AssignmentInput",
        ["Начать новую задачу может только постановщик."] = "Errors.StartPermission",
        ["Время завершения не может быть в будущем."] = "Errors.FutureCompletion",
        ["Завершить начатую задачу может только её исполнитель."] = "Errors.CompletionPermission",
        ["Неизвестное действие."] = "Errors.UnknownAction",
    };

    public static string Translate(string message)
    {
        return Keys.TryGetValue(message, out string? key) ? Texts.Get(key) : Texts.Get("Errors.ServerOperation");
    }
}
