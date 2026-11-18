namespace Chronify.Teams;

public enum TeamRole
{
    // Старі числові значення зберігаємо для сумісності з локальним кешем.
    NoRole = -1, Executor = 0, TeamLead = 1, Administrator = 2
}

// Ці ж правила перевіряє функція PostgreSQL. Прихованої кнопки у View недостатньо.
public static class TeamRules
{
    public static bool CanAssign(TeamRole sender, TeamRole receiver)
    {
        return sender == TeamRole.Administrator && receiver is TeamRole.Executor or TeamRole.TeamLead
        || sender == TeamRole.TeamLead && receiver == TeamRole.Executor;
    }
}

public record CloudCredentials(
    string Email,
    string Password,
    string Name = ""
);
public record CloudSession(
    Guid UserId,
    string Token,
    string Name = ""
);
public record CreateGroupRequest(string Name);
public record JoinGroupRequest(string Code);
public record ChangeRoleRequest(TeamRole Role);
public record CompleteAssignmentRequest(DateTime CompletedAtUtc);
public record CreateAssignmentRequest(
    Guid AssigneeId,
    string Title,
    string Description,
    DateTime? DueDate,
    int EstimatedMinutes,
    int Priority,
    int Status
);
public record GroupInfo(
    Guid Id,
    string Name,
    string InviteCode,
    TeamRole Role
);
public record MemberInfo(
    Guid Id,
    string Name,
    TeamRole Role
);
public record AssignmentInfo(
    Guid Id,
    Guid GroupId,
    string Title,
    string Description,
    DateTime? DueDate,
    int EstimatedMinutes,
    int Priority,
    int Status,
    Guid AssigneeId,
    string AssigneeName,
    Guid AssignedById,
    string AssignedByName,
    DateTime? CompletedAt
);
public record GroupDetails(
    GroupInfo Group,
    List<MemberInfo> Members,
    List<AssignmentInfo> Tasks
);
