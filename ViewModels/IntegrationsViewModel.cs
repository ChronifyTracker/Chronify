using Chronify.Services;
using Chronify.Services.Imports;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public partial class IntegrationsViewModel(Guid userId, Func<AppDbContext>? createContext = null) : AsyncPageViewModel
{
    private readonly ConnectionStore _connections = new(userId, createContext);
    private CancellationTokenSource? _cancellation;
    [ObservableProperty]
    public partial string GoogleClientId { get; set; } = NeonConfiguration.Read("CHRONIFY_GOOGLE_CLIENT_ID") ?? "";
    [ObservableProperty]
    public partial string GoogleClientSecret { get; set; } = NeonConfiguration.Read("CHRONIFY_GOOGLE_CLIENT_SECRET") ?? "";
    [ObservableProperty]
    public partial string NotionToken { get; set; } = "";
    public bool HasClassroom => _connections.Find("Classroom") is not null;

    public bool HasNotion => _connections.Find("Notion") is not null;

    public string Status
    {
        get
        {
            string classroom = Texts.Get(HasClassroom ? "Integrations.Connected" : "Integrations.Disconnected");
            string notion = Texts.Get(HasNotion ? "Integrations.Connected" : "Integrations.Disconnected");
            return $"Classroom: {classroom} · Notion: {notion}";
        }
    }

    public event Action? ConnectionsChanged;

    [RelayCommand]
    private Task ConnectClassroom()
    {
        return RunAsync(async () =>
        {
            using var cancellation = _cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await new ClassroomProvider(userId, createContext).Connect(
                GoogleClientId,
                GoogleClientSecret,
                cancellation.Token
            );
            Updated();
        });
    }

    [RelayCommand]
    private Task ConnectNotion()
    {
        return RunAsync(async () =>
        {
            using var cancellation = _cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            string token = NotionToken;
            NotionToken = "";
            await new NotionProvider(userId, createContext).Connect(token, cancellation.Token);
            Updated();
        });
    }

    [RelayCommand]
    private void DisconnectClassroom()
    {
        if (Run(() => _connections.Remove("Classroom")))
        {
            Updated();
        }
    }
    [RelayCommand]
    private void DisconnectNotion()
    {
        if (Run(() => _connections.Remove("Notion")))
        {
            Updated();
        }
    }
    [RelayCommand]
    private void Cancel()
    {
        if (IsBusy)
        {
            _cancellation?.Cancel();
        }
    }
    private void Updated()
    {
        OnPropertyChanged(nameof(HasClassroom));
        OnPropertyChanged(nameof(HasNotion));
        OnPropertyChanged(nameof(Status));
        Message = Texts.Get("Integrations.Changed");
        ConnectionsChanged?.Invoke();
    }
}
