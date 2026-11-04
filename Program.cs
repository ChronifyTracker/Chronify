using Chronify;
using TaskStatus = Chronify.TaskStatus;

ITaskRepository repository = new SqliteTaskRepository();

var university = new Category("Университет", "#4A90E2");
var personal = new Category("Личное", "#50E3C2");

var task1 = new TaskItem(
    "Сдать лабораторную", 
    "Подготовить отчет и загрузить в систему", 
    DateTime.Now.AddDays(3), 
    TaskPriority.High, 
    TaskStatus.InProgress,
    estimatedMinutes: 120,
    category: university
);

var task2 = new TaskItem(
    "Купить кофе", 
    "Зерновой для кофемашины", 
    DateTime.Now.AddHours(3), 
    TaskPriority.Low, 
    TaskStatus.InProgress,
    estimatedMinutes: 20,
    category: personal
);

repository.Add(task1);
repository.Add(task2);

Console.WriteLine($"Всего задач в базе: {repository.GetAll().Count}");

foreach (var task in repository.GetAll())
{
    var categoryName = task.Category != null ? task.Category.Name : "Без категории";
    Console.WriteLine($"[{task.Priority}] {task.Title} | Категория: {categoryName} | Оценка: {task.EstimatedMinutes} мин.");
}
