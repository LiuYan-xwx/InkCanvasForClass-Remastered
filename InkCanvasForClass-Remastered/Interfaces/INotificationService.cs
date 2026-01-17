using InkCanvasForClass_Remastered.Models;

namespace InkCanvasForClass_Remastered.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// 显示通知消息
    /// </summary>
    /// <param name="message">通知内容</param>
    /// <param name="durationMs">显示时长（毫秒），默认2500ms</param>
    void ShowNotification(string message, int durationMs = 2500);

    /// <summary>
    /// 通知事件，UI层订阅此事件来显示通知
    /// </summary>
    event Action<NotificationEventArgs>? NotificationRequested;
}
