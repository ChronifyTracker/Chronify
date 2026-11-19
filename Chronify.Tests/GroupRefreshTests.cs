using Chronify.Teams;
using Chronify.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Chronify.Tests;

[Collection("Localization")]
public class GroupRefreshTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly UserAccount _owner = new();
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();
    private readonly Guid _taskId = Guid.NewGuid();
    private readonly GroupsViewModel _model;

    public GroupRefreshTests()
    {
        _connection.Open();
        using (var context = CreateContext())
        {
            context.Database.EnsureCreated();
            context.Users.Add(_owner);
            context.CachedGroups.Add(new CachedGroup
            {
                UserId = _owner.Id,
                Id = _groupId,
                Name = "Student group",
                Role = TeamRole.Administrator
            });
            context.CachedMembers.Add(new CachedMember
            {
                UserId = _owner.Id,
                GroupId = _groupId,
                Id = _memberId,
                Name = "Executor",
                Role = TeamRole.Executor
            });
            context.CachedAssignments.Add(new CachedAssignment
            {
                UserId = _owner.Id,
                GroupId = _groupId,
                Id = _taskId,
                AssigneeId = _memberId,
                Title = "Saved assignment"
            });
            context.SaveChanges();
        }
        _model = new GroupsViewModel(_owner, CreateContext);
        _model.SelectedMember = _model.Members.Single();
        _model.Assignee = _model.Assignees.Single();
        _model.SelectedTask = _model.Tasks.Single();
        _model.TaskTitle = "Unsaved title";
        _model.TaskDescription = "Unsaved description";
    }

    private AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
    }

    [Fact]
    public void RefreshKeepsSelectionsAndDraftWhenANewMemberAppears()
    {
        Guid newcomer = Guid.NewGuid();
        using (var context = CreateContext())
        {
            context.CachedMembers.Add(new CachedMember
            {
                UserId = _owner.Id,
                GroupId = _groupId,
                Id = newcomer,
                Name = "New member",
                Role = TeamRole.NoRole
            });
            context.SaveChanges();
        }
        _model.RefreshCache();
        Assert.Contains(_model.Members, member => member.Id == newcomer);
        Assert.Equal(_groupId, _model.SelectedGroup?.Id);
        Assert.Equal(_memberId, _model.SelectedMember?.Id);
        Assert.Equal(_memberId, _model.Assignee?.Id);
        Assert.Equal(_taskId, _model.SelectedTask?.Id);
        Assert.Equal("Unsaved title", _model.TaskTitle);
        Assert.Equal("Unsaved description", _model.TaskDescription);
    }

    [Fact]
    public void RefreshRemovesAssigneeWhenTheirRoleNoLongerAllowsAssignment()
    {
        using (var context = CreateContext())
        {
            context.CachedMembers.Single().Role = TeamRole.NoRole;
            context.SaveChanges();
        }
        _model.RefreshCache();
        Assert.Equal(_memberId, _model.SelectedMember?.Id);
        Assert.Null(_model.Assignee);
        Assert.Empty(_model.Assignees);
        Assert.Equal("Unsaved title", _model.TaskTitle);
    }

    [Fact]
    public async Task BackgroundFailureKeepsCacheAndDoesNotReplaceManualMessages()
    {
        using (var context = CreateContext())
        {
            // Адреса навмисно не збігається з конфігурацією: мережевий запит не виконується.
            context.ServiceConnections.Add(new ServiceConnection
            {
                UserId = _owner.Id,
                Provider = "ChronifyNeon",
                Address = "unavailable-test-workspace",
                ProtectedSecret = "test-marker"
            });
            context.SaveChanges();
        }
        _model.ErrorMessage = "Manual validation error";
        _model.Message = "Previous operation result";
        await _model.RefreshAutomatically();
        Assert.True(_model.HasRefreshNotice);
        Assert.Single(_model.Members);
        Assert.Single(_model.Tasks);
        Assert.Equal("Manual validation error", _model.ErrorMessage);
        Assert.Equal("Previous operation result", _model.Message);
        Assert.False(_model.IsBusy);
        Assert.Equal(_memberId, _model.Assignee?.Id);
    }

    [Fact]
    public async Task CancelledOrStoppedRefreshLeavesCachedDataAvailable()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await _model.RefreshAutomatically(cancellation.Token);
        _model.StopAutomaticRefresh();
        await _model.RefreshAutomatically();
        Assert.Single(_model.Members);
        Assert.Single(_model.Tasks);
        Assert.Null(_model.RefreshNotice);
    }

    public void Dispose()
    {
        _model.StopAutomaticRefresh();
        _connection.Dispose();
    }
}
