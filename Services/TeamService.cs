using System.Text.Json;
using Chronify.Teams;
using Microsoft.EntityFrameworkCore;

namespace Chronify.Services;

public class TeamService(UserAccount user, Func<AppDbContext>? createContext = null)
{
    private readonly Func<AppDbContext> _createContext = createContext ?? (() => new AppDbContext());
    private readonly ConnectionStore _connections = new(user.Id, createContext);
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    public ServiceConnection? Connection
    {
        get
        {
            var connection = _connections.Find("ChronifyNeon");
            return string.IsNullOrEmpty(connection?.ProtectedSecret) ? null : connection;
        }
    }

    public List<CachedGroup> Groups()
    {
        using var db = _createContext();
        return db.CachedGroups.AsNoTracking().Where(g => g.UserId == user.Id).ToList().OrderBy(g => g.Name).ToList();
    }
    public List<CachedMember> Members(Guid groupId)
    {
        using var db = _createContext();
        return db.CachedMembers.AsNoTracking().Where(m => m.UserId == user.Id && m.GroupId == groupId).ToList();
    }
    public List<CachedAssignment> Tasks(Guid groupId)
    {
        using var db = _createContext();
        return db.CachedAssignments.AsNoTracking().Where(t => t.UserId == user.Id && t.GroupId == groupId).ToList();
    }

    public Task CreateGroup(string name)
    {
        return Command(HttpMethod.Post, "groups", new CreateGroupRequest(name));
    }

    public Task Join(string code)
    {
        return Command(HttpMethod.Post, "groups/join", new JoinGroupRequest(code));
    }

    public Task ChangeRole(
        Guid groupId,
        Guid memberId,
        TeamRole role
    )
    {
        return Command(HttpMethod.Put,
        $"groups/{groupId}/members/{memberId}/role", new ChangeRoleRequest(role));
    }

    public Task Assign(Guid groupId, CreateAssignmentRequest request)
    {
        return Command(HttpMethod.Post, $"groups/{groupId}/tasks", request);
    }

    public Task Start(Guid groupId, Guid taskId)
    {
        return Command(HttpMethod.Post, $"groups/{groupId}/tasks/{taskId}/start", null);
    }

    public async Task Synchronize(CancellationToken cancellation = default)
    {
        if (!await _syncLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            cancellation.ThrowIfCancellationRequested();
            var connection = Connection ?? throw new InvalidOperationException(Texts.Get("Groups.ConnectionNotice"));
            using (var db = _createContext())
            {
                foreach (var item in db.CachedAssignments.Where(a => a.UserId == user.Id && a.PendingCompletion)
                    .ToList())
                {
                    var task = db.Tasks.Single(t => t.UserId == user.Id && t.GroupAssignmentId == item.Id);
                    await Command(HttpMethod.Post, $"groups/{item.GroupId}/tasks/{item.Id}/complete",
                        new CompleteAssignmentRequest(task.CompletedAt?.ToUniversalTime() ?? DateTime.UtcNow), cancellation);
                    item.PendingCompletion = false;
                    await db.SaveChangesAsync(cancellation);
                }
            }
            var groups = await Request<List<GroupInfo>>(HttpMethod.Get, "groups", cancellation: cancellation);
            var details = new List<GroupDetails>();
            foreach (var group in groups)
            {
                details.Add(await Request<GroupDetails>(HttpMethod.Get, $"groups/{group.Id}", cancellation: cancellation));
            }
            cancellation.ThrowIfCancellationRequested();
            // Замінюємо кеш тільки після отримання всієї відповіді. Обрив мережі не видаляє локальні дані.
            using var context = _createContext();
            using var transaction = context.Database.BeginTransaction();
            var pending = context.CachedAssignments.AsNoTracking().Where(a => a.UserId == user.Id
                && a.PendingCompletion)
                .ToList();
            if (pending.Any(p => !details.Any(d => d.Tasks.Any(t => t.Id == p.Id))))
            {
                throw new InvalidOperationException(Texts.Get("Errors.PendingUnavailableTasks"));
            }

            context.CachedGroups.Where(g => g.UserId == user.Id).ExecuteDelete();
            context.CachedMembers.Where(g => g.UserId == user.Id).ExecuteDelete();
            context.CachedAssignments.Where(g => g.UserId == user.Id).ExecuteDelete();
            foreach (var detail in details)
            {
                context.CachedGroups.Add(new CachedGroup
                {
                    UserId = user.Id,
                    Id = detail.Group.Id,
                    Name = detail.Group.Name,
                    InviteCode = detail.Group.InviteCode,
                    Role = detail.Group.Role,
                    UpdatedAt = DateTime.Now
                });
                foreach (var member in detail.Members)
                {
                    context.CachedMembers.Add(new CachedMember { UserId = user.Id, GroupId = detail.Group.Id, Id = member.Id, Name = member.Name, Role = member.Role });
                }

                foreach (var task in detail.Tasks)
                {
                    bool completedOffline = pending.Any(p => p.Id == task.Id);
                    context.CachedAssignments.Add(new CachedAssignment
                    {
                        UserId = user.Id,
                        Id = task.Id,
                        GroupId = task.GroupId,
                        AssigneeId = task.AssigneeId,
                        AssignedById = task.AssignedById,
                        AssigneeName = task.AssigneeName,
                        AssignedByName = task.AssignedByName,
                        Title = task.Title,
                        Description = task.Description,
                        DueDate = task.DueDate?.ToLocalTime(),
                        EstimatedMinutes = task.EstimatedMinutes,
                        Priority = task.Priority,
                        Status = completedOffline ? 2 : task.Status,
                        PendingCompletion = completedOffline
                    });
                    if (task.AssigneeId == connection.RemoteUserId)
                    {
                        var local = completedOffline ? context.Tasks.FirstOrDefault(t => t.UserId == user.Id
                            && t.GroupAssignmentId == task.Id) : null;
                        SavePersonalCopy(context, detail.Group, completedOffline ? task with
                        {
                            Status = 2,
                            CompletedAt = local?.CompletedAt?.ToUniversalTime()
                        } : task);
                    }
                }
            }
            cancellation.ThrowIfCancellationRequested();
            context.SaveChanges();
            transaction.Commit();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private void SavePersonalCopy(
        AppDbContext db,
        GroupInfo group,
        AssignmentInfo remote
    )
    {
        var category = db.Categories.Local.FirstOrDefault(c => c.UserId == user.Id && c.Name == group.Name)
            ?? db.Categories.FirstOrDefault(c => c.UserId == user.Id && c.Name == group.Name);
        if (category is null)
        {
            category = new Category(group.Name, "#F34F78") { UserId = user.Id };
            db.Categories.Add(category);
        }
        var imported = TaskItem.FromImport(
            remote.Title,
            remote.Description,
            remote.DueDate?.ToLocalTime(),
            remote.EstimatedMinutes,
            (TaskPriority)remote.Priority,
            (TaskStatus)remote.Status,
            category,
            remote.CompletedAt?.ToLocalTime()
        );
        imported.UserId = user.Id;
        imported.GroupAssignmentId = remote.Id;
        imported.AssignedByName = remote.AssignedByName;
        var stored = db.Tasks.FirstOrDefault(t => t.UserId == user.Id && t.GroupAssignmentId == remote.Id);
        if (stored is null)
        {
            db.Tasks.Add(imported);
        }
        else
        {
            // Не змінюємо локальний Id: сесії обліку часу залишаються прив'язаними.
            var values = db.Entry(stored).CurrentValues;
            foreach (string name in new[] { "Title", "Description", "DueDate", "EstimatedMinutes", "Priority", "Status", "CategoryId", "AssignedByName" })
            {
                values[name] = db.Entry(imported).Property(name).CurrentValue;
            }

            values["CompletedAt"] = remote.CompletedAt?.ToLocalTime();
        }
    }

    private async Task Command(
        HttpMethod method,
        string path,
        object? data,
        CancellationToken cancellation = default
    )
    {
        await Request<JsonElement?>(method, path, data, cancellation);
    }
    private Task<T> Request<T>(
        HttpMethod method,
        string path,
        object? data = null,
        CancellationToken cancellation = default
    )
    {
        var connection = Connection ?? throw new InvalidOperationException(Texts.Get("Groups.ConnectionNotice"));
        if (connection.Address != NeonConfiguration.Workspace)
        {
            throw new InvalidOperationException(Texts.Get("Errors.WorkspaceChanged"));
        }

        string[] parts = path.Split('/');
        string action;
        object? payload = data;
        if (path == "groups")
        {
            action = method == HttpMethod.Get ? "groups/list" : "groups/create";
        }
        else if (path == "groups/join")
        {
            action = "groups/join";
        }
        else if (parts.Length == 2)
        {
            action = "groups/details";
            payload = new { groupId = parts[1] };
        }
        else if (parts.Length == 3 && parts[2] == "tasks")
        {
            action = "groups/tasks/create";
            payload = new { groupId = parts[1], request = data };
        }
        else if (parts.Length == 5 && parts[2] == "members")
        {
            action = "groups/roles/set";
            payload = new { groupId = parts[1], memberId = parts[3], request = data };
        }
        else if (parts.Length == 5 && parts[2] == "tasks")
        {
            action = "groups/tasks/" + parts[4];
            payload = new { groupId = parts[1], taskId = parts[3], request = data };
        }
        else
        {
            throw new InvalidOperationException(Texts.Get("Errors.UnknownGroupAction"));
        }

        return NeonGateway.Request<T>(action, payload, _connections.Secret("ChronifyNeon"), cancellation);
    }
}
