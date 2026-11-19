namespace Chronify.Tests;

public class TaskItemTests
{
    [Fact]
    public void NewTaskIsNotStartedAndNotCompleted()
    {
        var task = new TaskItem("task");

        Assert.Equal(TaskStatus.New, task.Status);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void StartTaskMakesItInProgress()
    {
        var task = new TaskItem("task");

        task.StartTask();

        Assert.Equal(TaskStatus.InProgress, task.Status);
    }

    [Fact]
    public void CompleteTaskStoresCompletionTime()
    {
        var task = new TaskItem("task");

        task.CompleteTask();

        Assert.Equal(TaskStatus.Completed, task.Status);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void ReopenTaskClearsCompletionTime()
    {
        var task = new TaskItem("task");
        task.CompleteTask();

        task.ReopenTask();

        Assert.Equal(TaskStatus.InProgress, task.Status);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void CompletedTaskCannotBeChanged()
    {
        var task = new TaskItem("task");
        task.CompleteTask();

        Assert.Throws<ModelValidationException>(() => task.SetTitle("new title"));
        Assert.Throws<ModelValidationException>(() => task.SetDescription("new description"));
        Assert.Throws<ModelValidationException>(() => task.SetDueDate(DateTime.Now));
        Assert.Throws<ModelValidationException>(() => task.SetEstimatedMinutes(30));
        Assert.Throws<ModelValidationException>(() => task.SetPriority(TaskPriority.High));
        Assert.Throws<ModelValidationException>(() => task.SetCategory(new Category("Дом", "#F34F78")));
        Assert.Equal("task", task.Title);
    }

    [Fact]
    public void CompletedTaskCannotBeStartedAgain()
    {
        var task = new TaskItem("task");
        task.CompleteTask();

        var error = Assert.Throws<ModelValidationException>(() => task.StartTask());

        Assert.Equal(ModelError.TaskCompleted, error.Error);
    }

    [Fact]
    public void EmptyTitleIsRejected()
    {
        var error = Assert.Throws<ModelValidationException>(() => new TaskItem("   "));

        Assert.Equal(ModelError.TaskTitleRequired, error.Error);
    }

    [Fact]
    public void EstimateOutsideTheRangeIsRejected()
    {
        var task = new TaskItem("task");

        var error = Assert.Throws<ModelValidationException>(
            () => task.SetEstimatedMinutes(TaskItem.MaxEstimatedMinutes + 1));

        Assert.Equal(ModelError.EstimatedMinutesOutOfRange, error.Error);
    }

    [Fact]
    public void HighPriorityTaskIsUrgent()
    {
        var high = new TaskItem("high", priority: TaskPriority.High);
        var medium = new TaskItem("medium", priority: TaskPriority.Medium);

        Assert.True(high.IsUrgent);
        Assert.False(medium.IsUrgent);
    }

    [Fact]
    public void OverdueTaskIsUrgent()
    {
        var task = TaskItem.FromImport("task", dueDate: DateTime.Now.AddDays(-1), priority: TaskPriority.Low);

        Assert.True(task.IsUrgent);
    }

    [Fact]
    public void TaskDueWithinDayIsUrgent()
    {
        var dueSoon = TaskItem.FromImport(
            "due soon",
            dueDate: DateTime.Now.AddHours(12),
            priority: TaskPriority.Low
        );
        var dueLater = TaskItem.FromImport(
            "due later",
            dueDate: DateTime.Now.AddDays(3),
            priority: TaskPriority.Low
        );

        Assert.True(dueSoon.IsUrgent);
        Assert.False(dueLater.IsUrgent);
    }

    [Fact]
    public void CompletedTaskIsNeverUrgent()
    {
        var task = TaskItem.FromImport("task", dueDate: DateTime.Now.AddDays(-1), priority: TaskPriority.Critical);

        task.CompleteTask();

        Assert.False(task.IsUrgent);
    }

    [Fact]
    public void TaskPastDeadlineIsOverdueUntilCompleted()
    {
        var task = TaskItem.FromImport("task", dueDate: DateTime.Now.AddDays(-1));

        Assert.True(task.IsOverdue);

        task.CompleteTask();

        Assert.False(task.IsOverdue);
    }
}
