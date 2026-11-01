using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileOrderStore(string path) : IOrderStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
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

        // Якщо файл існує на диску, то зчитуємо його
        if (File.Exists(_path))
        {
            // Десеріалізуємо JSON у список DTO (якщо файл пустий — повертаємо порожній список)
            var dtos = JsonSerializer.Deserialize<List<OrderDto>>(File.ReadAllText(_path), Options) ?? [];
            foreach (OrderDto dto in dtos)
            {
                // Перетворюємо DTO назад у бізнес-сутність Order через фабричний метод
                Order order = Order.FromDto(dto);
                _cache[order.Id] = order; // Записуємо у внутрішній кеш
            }
        }

        _loaded = true; // Ставимо мітку, що завантаження відбулося
    }

    // Приватний метод для скидання (запису) поточного стану кешу назад у файл (Flush/Persistence)
    private void Flush()
    {
        // Автоматично створюємо папку для файлу, якщо вона ще не існує
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        
        // Серіалізуємо всі сутності з кешу у DTO, перетворюємо в список і записуємо у файл
        File.WriteAllText(_path,
            JsonSerializer.Serialize(_cache.Values.Select(o => o.ToDto()).ToList(), Options));
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
