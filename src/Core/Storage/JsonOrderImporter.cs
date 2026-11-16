using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;


public sealed class JsonOrderImporter : IOrderImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<Order>> ImportAsync(
        string path,
        CancellationToken ct = default,
        IProgress<int>? progress = null)
    {
        
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);    

        List<OrderDto?> dtos;
        try
        {
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true); // вмикає справжній асинхронний ввід-вивід
            dtos = await JsonSerializer.DeserializeAsync<List<OrderDto?>>(stream, Options, ct) ?? [];
        }
        catch (FileNotFoundException ex)          // раніше за IOException
        {
            throw new StorageException($"Файл не знайдено: {path}", ex);
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл {path} містить некоректний JSON", ex);
        }
        catch (IOException ex)
        {
            throw new StorageException($"Не вдалося прочитати файл {path}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new StorageException($"Немає доступу до файлу {path}", ex);
        }

        var result = new List<Order>(dtos.Count);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested(); // перевірка на скасування всередині циклу, щоб не чекати завершення всіх ітерацій

            Order order = ToOrder(dtos[i], i + 1);
            if (!ids.Add(order.Id))
                throw new DomainException($"Запис {i + 1}: дубль id={order.Id} у файлі.");

            result.Add(order);
            progress?.Report((i + 1) * 100 / dtos.Count);
        }

        return result;
    }

    
    // Потокове читання: замовлення віддаються по одному, не завантажуючи весь файл у пам'ять.
    
    public async IAsyncEnumerable<Order> StreamAsync(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        
        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

        int index = 0;
        await foreach (OrderDto? dto in JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(stream, Options, ct))
        {
            index++;
            yield return ToOrder(dto, index);
        }
    }

    private static Order ToOrder(OrderDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожнє значення.");
        if (dto.Lines is null)
            throw new DomainException($"Запис {number}: відсутній список рядків.");

        try
        {
            return Order.FromDto(dto);
        }
        catch (Exception ex) when (ex is ArgumentException or DomainException)
        {
            // ArgumentOutOfRangeException — нащадок ArgumentException, тому покритий.
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
    }
}