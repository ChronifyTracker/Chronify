using System.Collections.ObjectModel;
using Chronify.Services;
using Chronify.Teams;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class GroupsViewModel : AsyncPageViewModel
{
    private readonly TeamService _service;
    public event Action? DataChanged;
    [ObservableProperty]
    public partial string GroupName { get; set; } = "";
    [ObservableProperty]
    public partial string InviteCode { get; set; } = "";
    [ObservableProperty]
    public partial CachedGroup? SelectedGroup { get; set; }
    [ObservableProperty]
    public partial CachedMember? SelectedMember { get; set; }
    [ObservableProperty]
    public partial CachedMember? Assignee { get; set; }
    [ObservableProperty]
    public partial CachedAssignment? SelectedTask { get; set; }
    [ObservableProperty]
    public partial string TaskTitle { get; set; } = "";
    [ObservableProperty]
    public partial string TaskDescription { get; set; } = "";
    [ObservableProperty]
    public partial DateTime? DueDate { get; set; }
    [ObservableProperty]
    public partial decimal EstimatedMinutes { get; set; }
    [ObservableProperty]
    public partial int PriorityIndex { get; set; } = 2;
    [ObservableProperty]
    public partial int InitialStatusIndex { get; set; } = 1;
    public IReadOnlyList<LocalizedOption> Priorities { get; } =
    [
        new("Priority.Deferred"),
        new("Priority.Low"),
        new("Priority.Medium"),
        new("Priority.High"),
        new("Priority.Critical")
    ];
    public IReadOnlyList<LocalizedOption> InitialStatuses { get; } =
    [
        new("Groups.StartLater"),
        new("Groups.StartNow")
    ];
    public ObservableCollection<CachedGroup> Groups { get; } = [];
    public ObservableCollection<CachedMember> Members { get; } = [];
    public ObservableCollection<CachedMember> Assignees { get; } = [];
    public ObservableCollection<CachedAssignment> Tasks { get; } = [];
    public bool IsConnected => _service.Connection is not null;

    public bool HasGroup => SelectedGroup is not null;

    public bool CanViewGroup => SelectedGroup?.Role is TeamRole.Executor or TeamRole.TeamLead or TeamRole.Administrator;

    public bool IsAdministrator => SelectedGroup?.Role == TeamRole.Administrator;

    public bool CanAssign => SelectedGroup?.Role is TeamRole.Administrator or TeamRole.TeamLead;

    public string ConnectionNotice => IsConnected ? "" : Texts.Get("Groups.ConnectionNotice");

    public string GroupDetails
    {
        get
        {
            if (SelectedGroup is null)
            {
                return Texts.Get("Groups.SelectGroup");
            }

            string details = Texts.Format(
                "Groups.Snapshot",
                SelectedGroup.Name,
                Texts.Role(SelectedGroup.Role),
                SelectedGroup.UpdatedAt
            );
            if (IsAdministrator)
            {
                details += Texts.Get("Groups.InviteCodeLine") + SelectedGroup.InviteCode;
            }
            if (SelectedGroup.Role == TeamRole.NoRole)
            {
                details += Texts.Get("Groups.WaitingRoleLine");
            }
            return details;
        }
    }

    public string SelectedTaskDetails
    {
        get
        {
            if (SelectedTask is null)
            {
                return Texts.Get("Groups.SelectTaskDetails");
            }

            string dueDate = SelectedTask.DueDate is DateTime date
                ? Texts.DateTime(date)
                : Texts.Get("Tasks.NoDueDateLower");
            return Texts.Format(
                "Groups.TaskDetails",
                SelectedTask.Title,
                SelectedTask.AssignedByName,
                SelectedTask.AssigneeName,
                dueDate,
                Priorities[SelectedTask.Priority],
                SelectedTask.Description
            );
        }
    }

    public bool CanStartSelected
    {
        get
        {
            return SelectedTask is { Status: 0 } task && task.AssignedById == _service.Connection?.RemoteUserId;
        }
    }

    public GroupsViewModel(UserAccount user, Func<AppDbContext>? createContext = null)
    {
        _service = new TeamService(user, createContext);
        RefreshCache();
    }

    public override void RefreshLanguage()
    {
        Guid? memberId = SelectedMember?.Id;
        Guid? assigneeId = Assignee?.Id;
        Guid? taskId = SelectedTask?.Id;
        OnSelectedGroupChanged(SelectedGroup);
        SelectedMember = Members.FirstOrDefault(member => member.Id == memberId);
        Assignee = Assignees.FirstOrDefault(member => member.Id == assigneeId);
        SelectedTask = Tasks.FirstOrDefault(task => task.Id == taskId);
        base.RefreshLanguage();
    }

    public void RefreshCache()
    {
        Guid? selectedId = SelectedGroup?.Id;
        Groups.Clear();
        foreach (var group in _service.Groups())
        {
            Groups.Add(group);
        }

        SelectedGroup = Groups.FirstOrDefault(g => g.Id == selectedId) ?? Groups.FirstOrDefault();
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(ConnectionNotice));
    }
    partial void OnSelectedGroupChanged(CachedGroup? value)
    {
        Members.Clear();
        Assignees.Clear();
        Tasks.Clear();
        SelectedMember = null;
        Assignee = null;
        SelectedTask = null;
        if (value is not null && CanViewGroup)
        {
            foreach (var member in _service.Members(value.Id))
            {
                Members.Add(member);
                if (TeamRules.CanAssign(value.Role, member.Role) && member.Id != _service.Connection?.RemoteUserId)
                {
                    Assignees.Add(member);
                }
            }
            foreach (var task in _service.Tasks(value.Id))
            {
                Tasks.Add(task);
            }
        }
        foreach (string property in new[] { nameof(HasGroup), nameof(CanViewGroup), nameof(IsAdministrator), nameof(CanAssign), nameof(GroupDetails) })
        {
            OnPropertyChanged(property);
        }
    }
    partial void OnSelectedTaskChanged(CachedAssignment? value)
    {
        OnPropertyChanged(nameof(SelectedTaskDetails));
        OnPropertyChanged(nameof(CanStartSelected));
    }
    [RelayCommand]
    private Task Synchronize()
    {
        return RunAsync(Sync);
    }

    private async Task Sync()
    {
        await _service.Synchronize();
        RefreshCache();
        DataChanged?.Invoke();
        Message = Texts.Get("Groups.Synchronized");
    }
    [RelayCommand]
    private Task CreateGroup()
    {
        return RunAsync(async () =>
        {
            await _service.CreateGroup(GroupName);
            GroupName = "";
            await Sync();
        });
    }

    [RelayCommand]
    private Task JoinGroup()
    {
        return RunAsync(async () =>
        {
            await _service.Join(InviteCode);
            InviteCode = "";
            await Sync();
        });
    }

    [RelayCommand]
    private Task MakeTeamLead()
    {
        return SetRole(TeamRole.TeamLead);
    }

    [RelayCommand]
    private Task MakeExecutor()
    {
        return SetRole(TeamRole.Executor);
    }

    private Task SetRole(TeamRole role)
    {
        return RunAsync(async () =>
        {
            if (SelectedGroup is null || SelectedMember is null)
            {
                throw new ArgumentException(Texts.Get("Errors.SelectMember"));
            }
            await _service.ChangeRole(SelectedGroup.Id, SelectedMember.Id, role);
            await Sync();
        });
    }

    [RelayCommand]
    private Task AssignTask()
    {
        return RunAsync(async () =>
        {
            if (SelectedGroup is null || Assignee is null)
            {
                throw new ArgumentException(Texts.Get("Errors.SelectAssignee"));
            }
            await _service.Assign(SelectedGroup.Id, new CreateAssignmentRequest(
                Assignee.Id,
                TaskTitle,
                TaskDescription,
                DueDate?.Date.AddDays(1).AddSeconds(-1).ToUniversalTime(),
                (int)EstimatedMinutes,
                PriorityIndex,
                InitialStatusIndex
            ));
            TaskTitle = "";
            TaskDescription = "";
            await Sync();
        });
    }

    [RelayCommand]
    private Task StartTask()
    {
        return RunAsync(async () =>
        {
            if (SelectedGroup is null || SelectedTask is null)
            {
                throw new ArgumentException(Texts.Get("Errors.SelectTask"));
            }
            await _service.Start(SelectedGroup.Id, SelectedTask.Id);
            await Sync();
        });
    }
}
