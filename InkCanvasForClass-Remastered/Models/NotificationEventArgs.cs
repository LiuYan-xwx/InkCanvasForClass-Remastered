namespace InkCanvasForClass_Remastered.Models;

public class NotificationEventArgs(string message, int durationMs)
{
    public string Message { get; } = message;
    public int DurationMs { get; } = durationMs;
}
