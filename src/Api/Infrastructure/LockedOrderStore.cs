using Core.Abstractions;
using Core.Domain;

namespace Api.Infrastructure;

/// <summary>
/// Декоратор: серіалізує доступ до сховища, бо Singleton одночасно обслуговує кілька HTTP-запитів,
/// а InMemoryOrderStore побудований на звичайному Dictionary.
/// </summary>
public sealed class LockedOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly object _gate = new();

    public IReadOnlyList<Order> List()
    {
        lock (_gate) { return inner.List(); }
    }

    public Order? GetById(string id)
    {
        lock (_gate) { return inner.GetById(id); }
    }

    public void Add(Order item)
    {
        lock (_gate) { inner.Add(item); }
    }

    public void Update(Order item)
    {
        lock (_gate) { inner.Update(item); }
    }

    public bool Remove(string id)
    {
        lock (_gate) { return inner.Remove(id); }
    }

    public IReadOnlyList<Order> Find(Func<Order, bool> predicate)
    {
        lock (_gate) { return inner.Find(predicate); }
    }
}
