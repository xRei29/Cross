namespace Core.Configuration;

public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public OrdersOptions Orders { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "orders.json";

    /// <summary>
    /// Повний шлях до файлу даних. Відносний DataPath рахується від baseDir (а не від поточного каталогу),
    /// абсолютний використовується як є. Відсутній каталог створюється.
    /// </summary>
    public string ResolveFullPath(string baseDir)
    {
        if (string.IsNullOrWhiteSpace(FileName))
            throw new InvalidOperationException("Storage:FileName не може бути порожнім.");

        string root = string.IsNullOrWhiteSpace(DataPath)
            ? baseDir
            : Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);

        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }

    /// <summary>
    /// Проста змінна CROSSAPP_DATA_PATH перекриває розділ Storage: якщо значення задано — новий DataPath, інакше без змін.
    /// </summary>
    public StorageOptions WithDataPathOverride(string? dataPath) =>
        string.IsNullOrWhiteSpace(dataPath)
            ? this
            : new StorageOptions { DataPath = dataPath, FileName = FileName };
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
}

/// <summary>Ключ варіанту «Замовлення»: Orders:Currency.</summary>
public sealed class OrdersOptions
{
    public string Currency { get; init; } = "UAH";
}