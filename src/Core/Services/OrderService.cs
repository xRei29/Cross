using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;
// Викликати Changed напряму може тільки сам OrderService
public sealed class OrderService(IOrderStore store)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public event EventHandler<OrderChangedEventArgs>? Changed;

    private void OnChanged(Order order, OrderChangeKind kind) =>
        Changed?.Invoke(this, new OrderChangedEventArgs(order.Id, kind, order.Lines.Count, order.Total));

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create(Guid.NewGuid().ToString("N")[..8], customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.Created);
        return order;
    }

    public void AddLine(string orderId, string productId, string name, decimal price, int quantity)
    {
        Order order = _store.GetById(orderId)
            ?? throw new DomainException($"Немає замовлення з id={orderId}.");

        order.AddLine(productId, name, price, quantity);
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
    }

    public void Confirm(string orderId)
    {
        Order order = _store.GetById(orderId)
            ?? throw new DomainException($"Немає замовлення з id={orderId}.");

        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.Confirmed);
    }

    /// <summary>
    /// Асинхронний імпорт замовлень. Повторний імпорт того самого файлу безпечний:
    /// замовлення з уже наявним id пропускаються (ідемпотентність).
    /// </summary>
    public async Task<(int Added, int Skipped)> ImportAsync(
        IOrderImporter importer,
        string path,
        CancellationToken ct = default,
        IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<Order> imported = await importer.ImportAsync(path, ct, progress);

        int added = 0, skipped = 0;
        foreach (Order order in imported)
        {
            if (_store.GetById(order.Id) is not null)
            {
                skipped++;
                continue;
            }

            _store.Add(order);
            OnChanged(order, OrderChangeKind.Created);
            added++;
        }

        return (added, skipped);
    }

    public IReadOnlyList<Order> All() => _store.List();
    public Order? Find(string id) => _store.GetById(id);
    public IReadOnlyList<Order> Search(Func<Order, bool> predicate) => _store.Find(predicate);
}