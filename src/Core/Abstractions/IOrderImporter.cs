using Core.Domain;

namespace Core.Abstractions;

/// <summary>
/// Порт імпорту замовлень із зовнішнього джерела. Асинхронний, підтримує скасування і прогрес.
/// </summary>
public interface IOrderImporter
{
    /// <param name="path">Шлях до файлу з замовленнями.</param>
    /// <param name="ct">Токен скасування.</param>
    /// <param name="progress">Необов'язковий звіт про прогрес у відсотках (0..100).</param>
    Task<IReadOnlyList<Order>> ImportAsync(
        string path,
        CancellationToken ct = default,
        IProgress<int>? progress = null);
}