using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Chronify;
using Chronify.Services;
using Chronify.Teams;
using Chronify.ViewModels;
using Chronify.Views;

public static class GroupRefreshUiChecks
{
    public static int Run(Func<MainWindowViewModel> createMain, Guid userId, Func<AppDbContext> createContext)
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
            checks++;
        }

        Check(new ConnectionStore(userId, createContext).Find("ChronifyNeon")?.Address == "test-workspace",
            "UI polling checks must use an isolated fake connection");
        Guid groupId = Guid.NewGuid();
        Guid executorId = Guid.NewGuid();
        using (var context = createContext())
        {
            context.CachedGroups.Add(new CachedGroup
            {
                UserId = userId, Id = groupId, Name = "UI refresh group", Role = TeamRole.Administrator
            });
            context.CachedMembers.Add(new CachedMember
            {
                UserId = userId, GroupId = groupId, Id = executorId, Name = "UI executor", Role = TeamRole.Executor
            });
            context.SaveChanges();
        }
        var model = createMain();
        var window = new MainWindow { DataContext = model };
        window.Show();
        try
        {
            model.ShowGroupsCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();
            var groups = model.GroupsPage;
            groups.SelectedGroup = groups.Groups.Single(group => group.Id == groupId);
            Dispatcher.UIThread.RunJobs();
            var groupView = window.GetVisualDescendants().OfType<GroupsView>().Single();
            var memberSelector = groupView.GetVisualDescendants().OfType<ComboBox>()
                .Single(control => ReferenceEquals(control.ItemsSource, groups.Members));
            var assigneeSelector = groupView.GetVisualDescendants().OfType<ComboBox>()
                .Single(control => ReferenceEquals(control.ItemsSource, groups.Assignees));
            memberSelector.SelectedItem = groups.Members.Single(member => member.Id == executorId);
            assigneeSelector.SelectedItem = groups.Assignees.Single(member => member.Id == executorId);
            groups.TaskTitle = "Assignment draft";

            Guid newcomer = Guid.NewGuid();
            using (var context = createContext())
            {
                context.CachedMembers.Add(new CachedMember
                {
                    UserId = userId, GroupId = groupId, Id = newcomer, Name = "UI newcomer", Role = TeamRole.NoRole
                });
                context.SaveChanges();
            }
            groups.RefreshCache();
            Dispatcher.UIThread.RunJobs();
            Check(groups.Members.Any(member => member.Id == newcomer), "The refreshed UI must show a new member");
            Check(groups.SelectedMember?.Id == executorId && groups.Assignee?.Id == executorId,
                "Two-way ComboBox bindings must preserve the selected member and assignee during refresh");
            Check(groups.TaskTitle == "Assignment draft", "Refreshing member controls must preserve the assignment draft");

            Check(groups.IsConnected && groups.HasRefreshNotice,
                "Opening groups must immediately attempt synchronization through the fake connection");
            groups.RefreshNotice = null;
            // RunJobs не обробляє системні події таймера. Тут потрібен справжній цикл диспетчера.
            using (var timerWait = new CancellationTokenSource(TimeSpan.FromSeconds(6)))
            {
                Dispatcher.UIThread.MainLoop(timerWait.Token);
            }
            Check(groups.HasRefreshNotice, "The open groups page must attempt another refresh through the window timer");
            groups.RefreshNotice = null;
            window.Close();
            Pump(groups.RefreshAutomatically());
            Check(groups.RefreshNotice is null, "Closing the window must stop future automatic refresh attempts");
            return checks;
        }
        finally
        {
            window.Close();
        }
    }

    private static void Pump(Task task)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(5);
        }
        if (!task.IsCompleted)
        {
            throw new TimeoutException("Group refresh UI check timed out.");
        }
        task.GetAwaiter().GetResult();
        Dispatcher.UIThread.RunJobs();
    }
}
