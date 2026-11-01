using Core.Notifications;

public sealed class FileSink(string path) : INotificationSink
{
    public void Notify(string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, message + Environment.NewLine);
    }
}