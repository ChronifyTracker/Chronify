using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Chronify.ViewModels;

public partial class PageViewModel : LocalizedObservableObject
{
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public override void RefreshLanguage()
    {
        ErrorMessage = null;
        base.RefreshLanguage();
    }

    protected bool Run(Action action)
    {
        try
        {
            action();
            ErrorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex switch
            {
                DbUpdateException => Texts.Get("Errors.DataSave"),
                SqliteException => Texts.Get("Errors.DatabaseRead"),
                ModelValidationException model => model.Error switch
                {
                    ModelError.CategoryNameTaken => Texts.Get("Errors.CategoryExists"),
                    ModelError.CategoryNameRequired => Texts.Get("Errors.CategoryName"),
                    ModelError.CategoryColorRequired => Texts.Get("Errors.CategoryColor"),
                    ModelError.TaskCompleted => Texts.Get("Errors.TaskCompleted"),
                    ModelError.TaskTitleRequired => Texts.Get("Errors.TaskTitle"),
                    ModelError.DueDateInPast => Texts.Get("Errors.PastDueDate"),
                    _ => Texts.Get("Errors.Input")
                },
                _ => ex.Message
            };
            return false;
        }
    }
}
