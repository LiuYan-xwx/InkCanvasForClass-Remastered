using InkCanvasForClass_Remastered.Models;

namespace InkCanvasForClass_Remastered.Services;

public class NotificationService : INotificationService
{
    public event Action<NotificationEventArgs>? NotificationRequested;

    public void ShowNotification(string message, int durationMs = 2500)
    {
        NotificationRequested?.Invoke(new NotificationEventArgs(message, durationMs));
    }
}