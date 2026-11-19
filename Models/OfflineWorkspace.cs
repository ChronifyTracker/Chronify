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
        return $"{Name} ({Texts.Role(Role)})";
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
        string status = PendingCompletion
            ? Texts.Get("Status.PendingCompletion")
            : Status switch
            {
                0 => Texts.Get("Status.NewLower"),
                1 => Texts.Get("Status.InProgressLower"),
                _ => Texts.Get("Status.CompletedLower")
            };
        return $"{Title} · {AssigneeName} · {status}";
    }
}
