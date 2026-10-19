using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private List<Order>? _cachedList;

    public IReadOnlyList<Order> List()
    {
        _cachedList ??= _inner.List().ToList();
        return _cachedList;
    }

    public Order? GetById(string id) => _inner.GetById(id);

    public void Add(Order item)
    {
        _inner.Add(item);
        _cachedList = null;
    }

    public void Update(Order item)
    {
        _inner.Update(item);
        _cachedList = null;
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed) _cachedList = null;
        return removed;
    }

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate) => _inner.Find(predicate);
}