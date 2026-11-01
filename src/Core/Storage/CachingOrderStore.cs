using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    // Кеш для списку замовлень у пам'яті
    private List<Order>? _cachedList;

    public IReadOnlyList<Order> List()
    {
        _cachedList ??= _inner.List().ToList();
        return _cachedList;
    }

    // GetById делегуємо напряму внутрішньому сховищу (тут декоратор не кешує окремі елементи)
    public Order? GetById(string id) => _inner.GetById(id);

    public void Add(Order item)
    {
        _inner.Add(item); // Виконуємо дію у внутрішньому сховищі
        _cachedList = null; // Інвалідуємо (скидаємо) кеш списку, бо дані змінилися!
    }

    public void Update(Order item)
    {
        _inner.Update(item);
        _cachedList = null; // Скидаємо кеш
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed) _cachedList = null; // Скидаємо кеш тільки якщо елемент реально був видалений
        return removed;
    }

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate) => _inner.Find(predicate);
}