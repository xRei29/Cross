namespace Core.Notifications;

public interface INotificationSink
{
    void Notify(string message);
}