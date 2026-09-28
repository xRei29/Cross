using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    // Єдиний спосіб створити замовлення: усі перевірки тут.
    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор замовлення обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        return new Order(id.Trim(), customerId.Trim());
    }

    public void AddLine(string productId, string name, decimal price, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Замовлення {Id} у статусі {Status}, рядки додавати не можна");

        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Ідентифікатор товару обов'язковий", nameof(productId));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price, "Ціна не може бути від'ємною");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Кількість у рядку має бути більшою за нуль");

        _lines.Add(new OrderLine(productId.Trim(), name.Trim(), price, quantity));
    }

    public void Confirm()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException($"Не можна підтвердити порожнє замовлення {Id}");

        ChangeStatus(OrderStatus.Confirmed);
    }

    public void Cancel()
    {
        ChangeStatus(OrderStatus.Cancelled);
    }

    // Додаткове завдання №3: явні дозволені переходи станів через switch expression (tuple pattern).
    private void ChangeStatus(OrderStatus newStatus)
    {
        bool allowed = (Status, newStatus) switch
        {
            (OrderStatus.Draft, OrderStatus.Confirmed) => true,
            (OrderStatus.Draft, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException($"Перехід зі стану {Status} у {newStatus} неможливий");

        Status = newStatus;
    }

    // Мапінг у DTO і назад — знадобиться сховищу тижня 5.
    public OrderDto ToDto() =>
        new(Id, CustomerId, Status, _lines.Select(l => new OrderLineDto(l.ProductId, l.Name, l.Price, l.Quantity)).ToList());

    public static Order FromDto(OrderDto dto)
    {
        Order order = Create(dto.Id, dto.CustomerId);
        foreach (OrderLineDto line in dto.Lines)
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);

        if (dto.Status == OrderStatus.Confirmed)
            order.Confirm();
        else if (dto.Status == OrderStatus.Cancelled)
            order.Cancel();

        return order;
    }

    public override string ToString() =>
        $"Замовлення {Id} (клієнт {CustomerId}) — {_lines.Count} поз., сума {Total.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}, статус: {Status}";
}