using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace Chronify.ViewModels;

public record AttachmentInfo(Guid Id, string Name);
public partial class TaskDetailsViewModel : AsyncPageViewModel
{
    private readonly Guid _userId;
    private readonly Guid _taskId;
    private readonly Func<AppDbContext> _createContext;
    public string Title { get; }
    public string Description { get; }
    public List<AttachmentInfo> Attachments { get; }
    public TaskDetailsViewModel(
        TaskItem task,
        Guid userId,
        Func<AppDbContext>? createContext = null
    )
    {
        _userId = userId;
        _taskId = task.Id;
        _createContext = createContext ?? (() => new AppDbContext());
        Title = task.Title;
        Description = task.Description;
        using var db = _createContext();
        Attachments = db.TaskAttachments.Where(a => a.UserId == userId && a.TaskId == task.Id)
            .Select(a => new AttachmentInfo(a.Id, a.Name)).ToList();
    }
    [RelayCommand]
    private Task OpenAttachment(Guid id)
    {
        return RunAsync(async () =>
        {
            using var db = _createContext();
            var file = db.TaskAttachments.Single(a => a.Id == id && a.UserId == _userId && a.TaskId == _taskId);
            string name = Path.GetFileName(file.Name);
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }
            string[] supported =
            [
                ".pdf",
                ".txt",
                ".md",
                ".docx",
                ".xlsx",
                ".pptx",
                ".png",
                ".jpg",
                ".jpeg",
                ".webp"
            ];
            if (!supported.Contains(Path.GetExtension(name).ToLowerInvariant()))
            {
                throw new InvalidOperationException(Texts.Get("Errors.AttachmentType"));
            }
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Chronify",
                "Attachments",
                _userId.ToString("N"),
                file.Id.ToString("N")
            );
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, name);
            await File.WriteAllBytesAsync(path, file.Content);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        });
    }
}
