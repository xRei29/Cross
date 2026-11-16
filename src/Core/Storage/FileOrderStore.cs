using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileOrderStore(string path) : IOrderStore
{
    // CamelCase у файлі; кирилиця читабельна (не \uXXXX); при читанні регістр імен не важливий,
    // тож старі файли з "Id"/"CustomerId" теж читаються.
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    // Прапорець, чи завантажені дані з файлу в кеш хоча б раз
    private bool _loaded;

    // Приватний метод ланцюжка завантаження
    private void EnsureLoaded()
    {
        if (_loaded) return; // Якщо вже завантажено — нічого не робимо

        _cache.Clear();

        // Якщо файл існує на диску, то зчитуємо його
        if (File.Exists(_path))
        {
            try
            {
                var dtos = JsonSerializer.Deserialize<List<OrderDto?>>(File.ReadAllText(_path), Options) ?? [];
                for (int i = 0; i < dtos.Count; i++)
                {
                    OrderDto? dto = dtos[i];
                    if (dto?.Lines is null)
                        throw new StorageException($"Файл даних {_path}: запис {i + 1} порожній або без списку рядків.");

                    // Перетворюємо DTO назад у бізнес-сутність: інваріанти перевіряються повторно
                    Order order = Order.FromDto(dto);
                    _cache[order.Id] = order;
                }
            }
            catch (JsonException ex)
            {
                throw new StorageException($"Файл даних {_path} пошкоджено: некоректний JSON ({ex.Message})", ex);
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new StorageException($"Файл даних {_path} містить некоректне замовлення: {ex.Message}", ex);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new StorageException($"Не вдалося прочитати файл даних {_path}: {ex.Message}", ex);
            }
        }

        _loaded = true; // Ставимо мітку, що завантаження відбулося
    }

    // Атомарний запис: спершу повний вміст у .tmp, потім File.Move з перезаписом.
    // Якщо процес впаде посеред запису, старий файл лишається цілим.
    private void Flush()
    {
        string json = JsonSerializer.Serialize(_cache.Values.Select(o => o.ToDto()).ToList(), Options);
        string tmp = _path + ".tmp";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(tmp, json);
            File.Move(tmp, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDelete(tmp);
            throw new StorageException($"Не вдалося записати файл даних {_path}: {ex.Message}", ex);
        }
    }

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Тимчасовий файл не вдалося прибрати — не критично, головна помилка вже в дорозі.
        }
    }

    /// <summary>
    /// Експорт: повністю замінює вміст файлу переданою колекцією одним атомарним записом.
    /// Старий вміст (навіть пошкоджений) не читається.
    /// </summary>
    public void ReplaceAll(IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        var fresh = new Dictionary<string, Order>(StringComparer.OrdinalIgnoreCase);
        foreach (Order order in orders)
        {
            if (!fresh.TryAdd(order.Id, order))
                throw new DomainException($"Дубль id={order.Id} у колекції для запису.");
        }

        _cache.Clear();
        foreach (Order order in fresh.Values)
            _cache[order.Id] = order;

        _loaded = true;
        Flush();
    }

    public IReadOnlyList<Order> List()
    {
        EnsureLoaded(); // Переконуємося, що дані завантажені з файлу
        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Order item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");

        _cache.Add(item.Id, item);
        Flush(); // Одразу записуємо зміни на диск при додаванні
    }

    public void Update(Order item)
    {
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush(); // Одразу оновлюємо файл при зміні
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush(); // Одразу оновлюємо файл при видаленні
        return true;
    }

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate)
    {
        EnsureLoaded();
        return _cache.Values.Where(predicate).ToList();
    }
}