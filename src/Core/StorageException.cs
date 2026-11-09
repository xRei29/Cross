namespace Core;

/// <summary>
/// Технічна помилка роботи зі сховищем/файлом (немає файлу, зіпсований JSON, недоступний диск).
/// Початкова технічна помилка зберігається в InnerException.
/// </summary>
public class StorageException : Exception
{
    public StorageException(string message) : base(message) { }

    public StorageException(string message, Exception innerException) : base(message, innerException) { }
}