using Chronify.Services;
using Chronify.ViewModels;

namespace Chronify.Tests;

public class WorkspaceSortTests
{
    [Fact]
    public void CategorySortIsAvailableThroughWorkspaceQuery()
    {
        var category = new Category("Study", "#123456");
        var plain = new TaskItem("No category");
        var study = new TaskItem("Study task", category: category);
        var result = TaskFilteringService.Apply(
            [plain, study],
            new TaskQuery { SortBy = TaskSortBy.Category },
            DateTime.Now
        );
        Assert.Equal([study.Id, plain.Id], result.Select(t => t.Id));
    }

    [Fact]
    public void WeightedUrgencySortIncludesPriorityHeadStart()
    {
        var tomorrow = new TaskItem("Tomorrow", dueDate: DateTime.Now.AddDays(1), priority: TaskPriority.Low);
        var important = new TaskItem(
            "Important",
            dueDate: DateTime.Now.AddDays(2),
            priority: TaskPriority.Critical
        );
        var result = TaskFilteringService.Apply(
            [tomorrow, important],
            new TaskQuery { SortBy = TaskSortBy.WeightedUrgency },
            DateTime.Now
        );
        Assert.Equal(important.Id, result[0].Id);
    }

    [Fact]
    public void TaskCardsKeepReadableDurationFromDevelop()
    {
        var card = new TaskCard(new TaskItem("Study", estimatedMinutes: 150));
        Assert.Equal("2,5 ч", card.Estimate);
        Assert.Contains("2,5 ч", card.Details);
    }
}
