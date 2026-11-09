using Core.Abstractions;
using Core.Domain;

namespace Core.Tests;

/// <summary>Підміна імпортера для тестів: без файлів і без диска.</summary>
internal sealed class FakeImporter(Func<CancellationToken, Task<IReadOnlyList<Order>>> work) : IOrderImporter
{
    public Task<IReadOnlyList<Order>> ImportAsync(
        string path, CancellationToken ct = default, IProgress<int>? progress = null) => work(ct);

    /// <summary>Миттєво повертає задані замовлення.</summary>
    public static FakeImporter Returning(params Order[] orders) =>
        new(_ => Task.FromResult<IReadOnlyList<Order>>(orders));

    /// <summary>«Нескінченно довгий» імпорт, який завершується лише скасуванням.</summary>
    public static FakeImporter Endless() =>
        new(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return [];
        });
}