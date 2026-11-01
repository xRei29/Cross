using Core.Events;              
using Core.Notifications;       

public sealed class NotificationDispatcher(IEnumerable<INotificationSink> sinks)
// sinks — список підписників, переданий через конструктор 
{
    private readonly IEnumerable<INotificationSink> _sinks = sinks;
    // зберігаємо список у поле, присвоюється один раз

    public void Handle(object? sender, OrderChangedEventArgs e)
    // сигнатура як у EventHandler — метод можна підписати на подію
    {
        string message = $"{e.At:HH:mm:ss} {e.Kind} {e.OrderId} поз.:{e.LinesCount} сума:{e.Total:F2}";
        // збираємо дані події в один рядок тексту

        foreach (INotificationSink sink in _sinks)
            sink.Notify(message);
        // кожному sink'у передаємо той самий текст
    }
}