using Chronify.Services;

namespace Chronify.Tests;

public class TaskFilterTests
{
    [Fact]
    public void EmptyFilterKeepsEveryTask()
    {
        var tasks = new List<TaskItem>
        {
            new("first"),
            new("second", status: TaskStatus.Completed)
        };

        var filtered = new TaskFilter().Apply(tasks);

        Assert.Equal(["first", "second"], Titles(filtered));
    }

    [Fact]
    public void SearchFindsTitleIgnoringCase()
    {
        var tasks = new List<TaskItem>
        {
            new("Лаба по физике"),
            new("ЛАБОРАТОРНАЯ"),
            new("Купить продукты")
        };

        var filtered = new TaskFilter { Search = "лаб" }.Apply(tasks);

        Assert.Equal(["Лаба по физике", "ЛАБОРАТОРНАЯ"], Titles(filtered));
    }

    [Fact]
    public void SearchIgnoresSpacesAroundTheText()
    {
        var tasks = new List<TaskItem> { new("Отчёт"), new("Реферат") };

        var filtered = new TaskFilter { Search = "  отчёт " }.Apply(tasks);

        Assert.Equal(["Отчёт"], Titles(filtered));
    }

    [Fact]
    public void SearchDoesNotLookAtDescription()
    {
        var tasks = new List<TaskItem> { new("Отчёт", description: "лаба") };

        var filtered = new TaskFilter { Search = "лаба" }.Apply(tasks);

        Assert.Empty(filtered);
    }

    [Fact]
    public void CategoryFilterKeepsOnlyThatCategory()
    {
        var study = new Category("Учёба", "#F34F78");
        var home = new Category("Дом", "#F34F78");
        var tasks = new List<TaskItem>
        {
            new("study task", category: study),
            new("home task", category: home),
            new("no category")
        };

        var filtered = new TaskFilter { CategoryId = study.Id }.Apply(tasks);

        Assert.Equal(["study task"], Titles(filtered));
    }

    [Fact]
    public void NewFilterLeavesOutOverdueTasks()
    {
        var filtered = new TaskFilter { Status = TaskStatusFilter.New }.Apply(TasksOfEveryStatus());

        Assert.Equal(["new"], Titles(filtered));
    }

    [Fact]
    public void InProgressFilterLeavesOutOverdueTasks()
    {
        var filtered = new TaskFilter { Status = TaskStatusFilter.InProgress }.Apply(TasksOfEveryStatus());

        Assert.Equal(["in progress"], Titles(filtered));
    }

    [Fact]
    public void CompletedFilterKeepsCompletedTasks()
    {
        var filtered = new TaskFilter { Status = TaskStatusFilter.Completed }.Apply(TasksOfEveryStatus());

        Assert.Equal(["completed late"], Titles(filtered));
    }

    [Fact]
    public void OverdueFilterKeepsUnfinishedTasksPastDeadline()
    {
        var filtered = new TaskFilter { Status = TaskStatusFilter.Overdue }.Apply(TasksOfEveryStatus());

        Assert.Equal(["new overdue", "in progress overdue"], Titles(filtered));
    }

    [Fact]
    public void TodayFilterKeepsOnlyDeadlinesToday()
    {
        var filtered = new TaskFilter { Due = TaskDueFilter.Today }.Apply(TasksOfEveryDeadline());

        Assert.Equal(["today start", "today end"], Titles(filtered));
    }

    [Fact]
    public void NextThreeDaysFilterKeepsTodayAndTwoMoreDays()
    {
        var filtered = new TaskFilter { Due = TaskDueFilter.NextThreeDays }.Apply(TasksOfEveryDeadline());

        Assert.Equal(
        [
            "today start",
            "today end",
            "tomorrow",
            "day after tomorrow end"
        ], Titles(filtered));
    }

    [Fact]
    public void FiltersCombine()
    {
        var study = new Category("Учёба", "#F34F78");
        var tasks = new List<TaskItem>
        {
            new("Лаба сегодня", dueDate: DateTime.Today.AddHours(23), category: study),
            new("Лаба через неделю", dueDate: DateTime.Today.AddDays(7), category: study),
            new("Лаба без категории", dueDate: DateTime.Today.AddHours(23)),
            new("Отчёт сегодня", dueDate: DateTime.Today.AddHours(23), category: study)
        };
        var filter = new TaskFilter
        {
            Search = "лаба",
            CategoryId = study.Id,
            Due = TaskDueFilter.Today
        };

        var filtered = filter.Apply(tasks);

        Assert.Equal(["Лаба сегодня"], Titles(filtered));
    }

    private static List<TaskItem> TasksOfEveryStatus()
    {
        var yesterday = DateTime.Now.AddDays(-1);
        var tomorrow = DateTime.Now.AddDays(1);

        return

        [

            new("new", dueDate: tomorrow),

            new("new overdue", dueDate: yesterday),

            new("in progress", status: TaskStatus.InProgress),

            new("in progress overdue", dueDate: yesterday, status: TaskStatus.InProgress),

            new("completed late", dueDate: yesterday, status: TaskStatus.Completed)

        ];
    }

    private static List<TaskItem> TasksOfEveryDeadline()
    {
        var today = DateTime.Today;

        return

        [

            new("yesterday end", dueDate: today.AddMinutes(-1)),

            new("today start", dueDate: today),

            new("today end", dueDate: today.AddDays(1).AddMinutes(-1)),

            new("tomorrow", dueDate: today.AddDays(1)),

            new("day after tomorrow end", dueDate: today.AddDays(3).AddMinutes(-1)),

            new("in three days", dueDate: today.AddDays(3)),

            new("no deadline")

        ];
    }

    private static List<string> Titles(List<TaskItem> tasks)
    {
        return tasks.Select(task => task.Title).ToList();
    }
}
