using Chronify.Teams;

namespace Chronify;

public class ServiceConnection
{
    public Guid UserId { get; set; }
    public string Provider { get; set; } = "";
    public string Address { get; set; } = "";
    public string ProtectedSecret { get; set; } = "";
    public Guid? RemoteUserId { get; set; }
}

public class CachedGroup
{
    public Guid UserId { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string InviteCode { get; set; } = "";
    public TeamRole Role { get; set; }
    public DateTime UpdatedAt { get; set; }
    public override string ToString()
    {
        return Name;
    }
}

public class CachedMember
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public TeamRole Role { get; set; }
    public override string ToString()
    {
        return $"{Name} ({Role switch { TeamRole.Administrator => "администратор", TeamRole.TeamLead => "тимлид", TeamRole.Executor => "исполнитель", _ => "без роли" }})";
    }
}

public class CachedAssignment
{
    public Guid UserId { get; set; }
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid AssigneeId { get; set; }
    public Guid AssignedById { get; set; }
    public string AssigneeName { get; set; } = "";
    public string AssignedByName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public int EstimatedMinutes { get; set; }
    public int Priority { get; set; }
    public int Status { get; set; }
    public bool PendingCompletion { get; set; }
    public override string ToString()
    {
        return $"{Title} · {AssigneeName} · {(PendingCompletion ? "выполнена, ожидает отправки" : Status switch { 0 => "новая", 1 => "в работе", _ => "завершена" })}";
    }
}
