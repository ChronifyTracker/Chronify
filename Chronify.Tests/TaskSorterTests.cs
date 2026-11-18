using Chronify.Services;

namespace Chronify.Tests;

public class TaskSorterTests
{
    private static readonly DateTime Start = new(2030, 1, 10, 12, 0, 0);

    [Fact]
    public void EarlierDeadlineGoesFirst()
    {
        var later = new TaskItem("later", dueDate: Start.AddDays(3));
        var sooner = new TaskItem("sooner", dueDate: Start.AddDays(1));

        var sorted = TaskSorter.SortByUrgency([later, sooner]);

        Assert.Equal(["sooner", "later"], Titles(sorted));
    }

    [Fact]
    public void HigherPriorityGetsHeadStart()
    {
        var medium = new TaskItem("medium", dueDate: Start.AddDays(4), priority: TaskPriority.Medium);
        var critical = new TaskItem("critical", dueDate: Start.AddDays(5), priority: TaskPriority.Critical);

        var sorted = TaskSorter.SortByUrgency([medium, critical]);

        Assert.Equal(["critical", "medium"], Titles(sorted));
    }

    [Fact]
    public void PriorityDoesNotBeatMuchEarlierDeadline()
    {
        var critical = new TaskItem("critical", dueDate: Start.AddDays(6), priority: TaskPriority.Critical);
        var low = new TaskItem("low", dueDate: Start.AddDays(1), priority: TaskPriority.Low);

        var sorted = TaskSorter.SortByUrgency([critical, low]);

        Assert.Equal(["low", "critical"], Titles(sorted));
    }

    [Fact]
    public void LongerTaskGoesFirstWhenDeadlinesMatch()
    {
        var quick = new TaskItem("quick", dueDate: Start.AddDays(4), estimatedMinutes: 5);
        var longTask = new TaskItem("long", dueDate: Start.AddDays(4), estimatedMinutes: 600);

        var sorted = TaskSorter.SortByUrgency([quick, longTask]);

        Assert.Equal(["long", "quick"], Titles(sorted));
    }

    [Fact]
    public void HigherPriorityGoesFirstWhenStartTimesMatch()
    {
        var low = new TaskItem("low", dueDate: Start.AddDays(2), priority: TaskPriority.Low);
        var critical = new TaskItem("critical", dueDate: Start.AddDays(5), priority: TaskPriority.Critical);

        var sorted = TaskSorter.SortByUrgency([low, critical]);

        Assert.Equal(["critical", "low"], Titles(sorted));
    }

    [Fact]
    public void TasksWithoutDeadlineGoAfterTasksWithDeadline()
    {
        var noDeadline = new TaskItem("no deadline", priority: TaskPriority.Critical);
        var withDeadline = new TaskItem("with deadline", dueDate: Start.AddDays(30), priority: TaskPriority.Postponed);

        var sorted = TaskSorter.SortByUrgency([noDeadline, withDeadline]);

        Assert.Equal(["with deadline", "no deadline"], Titles(sorted));
    }

    [Fact]
    public void TasksWithoutDeadlineAreOrderedByPriority()
    {
        var low = new TaskItem("low", priority: TaskPriority.Low);
        var high = new TaskItem("high", priority: TaskPriority.High);

        var sorted = TaskSorter.SortByUrgency([low, high]);

        Assert.Equal(["high", "low"], Titles(sorted));
    }

    [Fact]
    public void CompletedTasksGoLast()
    {
        var completed = new TaskItem("completed", dueDate: Start.AddDays(-5),
            priority: TaskPriority.Critical, status: TaskStatus.Completed);
        var open = new TaskItem("open", priority: TaskPriority.Postponed);

        var sorted = TaskSorter.SortByUrgency([completed, open]);

        Assert.Equal(["open", "completed"], Titles(sorted));
    }

    [Fact]
    public void VeryEarlyDeadlineIsSortedFirst()
    {
        var normal = new TaskItem("normal", dueDate: Start);
        var yearOne = new TaskItem("year one", dueDate: DateTime.MinValue.AddHours(18),
            estimatedMinutes: TaskItem.MaxEstimatedMinutes, priority: TaskPriority.Critical);

        var sorted = TaskSorter.SortByUrgency([normal, yearOne]);

        Assert.Equal(["year one", "normal"], Titles(sorted));
    }

    [Fact]
    public void EmptyListStaysEmpty()
    {
        var sorted = TaskSorter.SortByUrgency([]);

        Assert.Empty(sorted);
    }

    private static List<string> Titles(List<TaskItem> tasks)
    {
        return tasks.Select(task => task.Title).ToList();
    }
}
