namespace Core.Domain;

// Цей інваріант навмисно НЕ в Order: він потребує знання про ІНШІ замовлення клієнта,
// а окрема сутність Order бачить лише себе. Такі правила належать сервісу (тиждень 5),
// який матиме доступ до сховища і зможе отримати всі замовлення клієнта.
public static class OrderPolicy
{
    private const int MaxDraftOrdersPerCustomer = 3;

    public static void EnsureCanCreateDraftOrder(IReadOnlyList<Order> existingOrders, string customerId)
    {
        int draftCount = existingOrders.Count(o => o.CustomerId == customerId && o.Status == OrderStatus.Draft);

        if (draftCount >= MaxDraftOrdersPerCustomer)
            throw new InvalidOperationException(
                $"Клієнт {customerId} вже має {draftCount} непідтверджених замовлень — ліміт {MaxDraftOrdersPerCustomer}");
    }
}