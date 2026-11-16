public enum Severity { Debug, Information, Warning, Error }

/// <summary>
/// Мінімальний логер для демонстрації профілів: повідомлення нижче Logging:MinimumLevel не друкуються.
/// </summary>
public sealed class ConsoleLog(string? minimumLevel)
{
    private readonly Severity _minimum =
        Enum.TryParse<Severity>(minimumLevel, ignoreCase: true, out var parsed) ? parsed : Severity.Information;

    public Severity Minimum => _minimum;

    public void Write(Severity level, string message)
    {
        if (level < _minimum) return;
        Console.WriteLine($"[{level}] {message}");
    }

    public void Debug(string message) => Write(Severity.Debug, message);
    public void Info(string message) => Write(Severity.Information, message);
    public void Warn(string message) => Write(Severity.Warning, message);
}