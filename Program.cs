using Chronify;
using TaskStatus = Chronify.TaskStatus; // Чтобы C# не путал с системным System.Threading.Tasks.TaskStatus

// 1. Создаём репозиторий через интерфейс!
ITaskRepository repository = new TaskRepository();

// 2. Создаём и добавляем пару задач
var task1 = new TaskItem(
    "Сдать лабораторную",
    "Подготовить отчет и загрузить в систему",
    DateTime.Now.AddDays(2),
    TaskPriority.High,
    TaskStatus.InProgress
);

var task2 = new TaskItem(
    "Купить кофе",
    "Зерновой для кофемашины",
    null,
    TaskPriority.Low,
    TaskStatus.InProgress
);

repository.Add(task1);
repository.Add(task2);

// 3. Проверяем получение всех задач
Console.WriteLine($"Всего задач в трекере: {repository.GetAll().Count}");

foreach (var task in repository.GetAll())
{
    var hoursRemaining = task.TimeRemaining?.Hours.ToString() ?? "Без дедлайна";
    Console.WriteLine($"[{task.Priority}] {task.Title} (Осталось: {hoursRemaining})");
}

// 4. Проверяем работу завершения задачи
task1.CompleteTask();
Console.WriteLine($"\nСтатус первой задачи после завершения: {task1.Status}");
