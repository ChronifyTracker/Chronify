using System.Collections.ObjectModel;
using Chronify.Services.Imports;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class ImportRow(ExternalTask task) : LocalizedObservableObject
{
    public ExternalTask Task { get; } = task;
    public string Title => Task.Title;

    public string Deadline => Task.DueDate is DateTime dueDate ? Texts.DateTime(dueDate) : Texts.Get("Tasks.NoDueDate");

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public partial class ImportViewModel(Guid userId, Func<AppDbContext>? createContext = null) : AsyncPageViewModel
{
    private IExternalTaskProvider? _provider;
    private CancellationTokenSource? _cancellation;
    [ObservableProperty]
    public partial string ProviderName { get; set; } = "";
    [ObservableProperty]
    public partial ExternalTaskSource? SelectedSource { get; set; }
    [ObservableProperty]
    public partial bool AllSources { get; set; }
    public ObservableCollection<ExternalTaskSource> Sources { get; } = [];
    public ObservableCollection<ImportRow> Tasks { get; } = [];
    public event Action? DataChanged;

    public Task Open(string provider)
    {
        return RunAsync(async () =>
        {
            ProviderName = provider;
            Sources.Clear();
            Tasks.Clear();
            SelectedSource = null;
            _provider = provider == "Classroom" ? new ClassroomProvider(userId, createContext) : new NotionProvider(
                userId,
                createContext
            );
            using var cancellation = _cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            foreach (var source in await _provider.Sources(cancellation.Token))
            {
                Sources.Add(source);
            }
            SelectedSource = Sources.FirstOrDefault();
            Message = Sources.Count == 0 ? Texts.Get("Import.NoSources") : Texts.Get("Import.SelectSourceHint");
        });
    }

    [RelayCommand]
    private Task LoadTasks()
    {
        return RunAsync(async () =>
        {
            if (_provider is null || !AllSources && SelectedSource is null)
            {
                throw new ArgumentException(Texts.Get("Errors.SelectSource"));
            }
            using var cancellation = _cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            var loaded = new List<ExternalTask>();
            foreach (var source in AllSources ? Sources.ToList() : new List<ExternalTaskSource> { SelectedSource! })
            {
                loaded.AddRange(await _provider.Tasks(source, cancellation.Token));
            }
            Tasks.Clear();
            foreach (var task in loaded.DistinctBy(t => t.Id))
            {
                Tasks.Add(new ImportRow(task));
            }
            Message = Texts.Format("Import.Found", Tasks.Count);
        });
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var task in Tasks)
        {
            task.IsSelected = true;
        }
    }
    [RelayCommand]
    private void SelectNone()
    {
        foreach (var task in Tasks)
        {
            task.IsSelected = false;
        }
    }
    [RelayCommand]
    private Task ImportSelected()
    {
        return Import(false);
    }

    [RelayCommand]
    private Task ImportAll()
    {
        return Import(true);
    }

    private Task Import(bool all)
    {
        return RunAsync(async () =>
        {
            if (_provider is null)
            {
                throw new ArgumentException(Texts.Get("Errors.LoadTasksFirst"));
            }
            var selected = Tasks.Where(t => all || t.IsSelected).Select(t => t.Task).ToList();
            if (selected.Count == 0)
            {
                throw new ArgumentException(Texts.Get("Errors.NoSelectedTasks"));
            }
            using var cancellation = _cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            int count = await new TaskImportService(userId, createContext).Import(
                _provider,
                selected,
                cancellation.Token
            );
            foreach (var row in Tasks)
            {
                row.IsSelected = false;
            }
            Message = Texts.Format("Import.Result", count);
            DataChanged?.Invoke();
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        if (IsBusy)
        {
            _cancellation?.Cancel();
        }
    }
}
