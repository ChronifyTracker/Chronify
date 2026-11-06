using Chronify;
using TaskStatus = Chronify.TaskStatus;

ITaskRepository repository = new SqliteTaskRepository();

var university = new Category("Университет", "#4A90E2");
var personal = new Category("Личное", "#50E3C2");

var task1 = new TaskItem(
    "Сдать лабораторную", 
    "Подготовить отчет и загрузить в систему", 
    dueDate: DateTime.Now.AddDays(3),
    estimatedMinutes: 120,
    priority: TaskPriority.High,
    status: TaskStatus.InProgress,
    category: university
);

var task2 = new TaskItem(
    "Купить кофе", 
    "Зерновой для кофемашины", 
    dueDate: DateTime.Now.AddHours(3),
    estimatedMinutes: 20,
    priority: TaskPriority.Low,
    status: TaskStatus.InProgress,
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
