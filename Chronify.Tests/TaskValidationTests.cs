namespace Chronify.Tests;

public class TaskValidationTests
{
    [Fact]
    public void InvalidPriorityIsRejected()
    {
        var task = new TaskItem("Task");
        Assert.Throws<ModelValidationException>(() => task.SetPriority((TaskPriority)999));
        Assert.Equal(TaskPriority.Medium, task.Priority);
    }

    [Fact]
    public void EditingCopyDoesNotChangeOriginal()
    {
        var original = new TaskItem("Original");
        var edited = original.Copy();
        edited.SetTitle("Edited");
        Assert.Throws<ModelValidationException>(() => edited.SetPriority((TaskPriority)999));
        Assert.Equal("Original", original.Title);
    }

    [Fact]
    public void OldDeadlineCanRemainButCannotBeChangedToAnotherPastDate()
    {
        var date = DateTime.Now.AddDays(-2);
        var task = TaskItem.FromImport("Saved task", "Description", date);
        task.SetDueDate(date);
        Assert.Throws<ModelValidationException>(() => task.SetDueDate(date.AddDays(-1)));
        Assert.Equal(date, task.DueDate);
    }
}
