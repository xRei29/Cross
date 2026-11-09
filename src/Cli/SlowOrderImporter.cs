using Core.Abstractions;
using Core.Domain;

/// <summary>
/// Декоратор для демо скасування: реальний імпорт файлу миттєвий, тому після нього
/// штучно «обробляємо» кожне замовлення з паузою. Пауза через Task.Delay(…, ct) реагує на скасування.
/// </summary>
public sealed class SlowOrderImporter(IOrderImporter inner, TimeSpan delayPerOrder) : IOrderImporter
{
    public async Task<IReadOnlyList<Order>> ImportAsync(
        string path,
        CancellationToken ct = default,
        IProgress<int>? progress = null)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<Order> orders = await inner.ImportAsync(path, ct);

        for (int i = 0; i < orders.Count; i++)
        {
            await Task.Delay(delayPerOrder, ct);
            progress?.Report((i + 1) * 100 / orders.Count);
        }

        return orders;
    }
}