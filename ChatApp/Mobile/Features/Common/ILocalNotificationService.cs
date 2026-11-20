namespace ChatApp.Mobile.Features.Common;

public interface ILocalNotificationService
{
    Task EnsureInitializedAndStartAsync();
    void ShowMessageNotification(string senderName, string messagePreview, Guid conversationId);
}

public class DefaultLocalNotificationService : ILocalNotificationService
{
    public Task EnsureInitializedAndStartAsync() => Task.CompletedTask;
    public void ShowMessageNotification(string senderName, string messagePreview, Guid conversationId) { }
}
