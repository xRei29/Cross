namespace Core;

/// <summary>
/// Порушення бізнес-правила предметної області (наприклад, «замовлення вже підтверджено»).
/// Це НЕ помилка вхідних аргументів (для цього є ArgumentException) і НЕ технічний збій (IOException).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}