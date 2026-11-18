using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace Chronify.ViewModels;

public partial class PageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

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
                DbUpdateException => "Не удалось сохранить данные. Повторите попытку.",
                SqliteException => "Не удалось прочитать базу данных. Проверьте доступ к файлу программы.",
                ModelValidationException model => model.Error switch
                {
                    ModelError.CategoryNameTaken => "Такая категория уже существует.",
                    ModelError.CategoryNameRequired => "Введите название категории.",
                    ModelError.CategoryColorRequired => "Выберите цвет категории.",
                    ModelError.TaskCompleted => "Завершённую задачу нельзя изменить.",
                    ModelError.TaskTitleRequired => "Введите название задачи.",
                    ModelError.DueDateInPast => "Дедлайн не может быть в прошлом.",
                    _ => "Проверьте введённые данные."
                },
                _ => ex.Message
            };
            return false;
        }
    }
}
