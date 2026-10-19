using Core.Domain;

namespace Core;

public static class SampleData
{
    public static List<Order> Orders()
    {
        var orders = new List<Order>();

        for (int i = 1; i <= 15; i++)
        {
            var order = Order.Create($"ORD-{i:000}", $"CUST-{(i % 5) + 1:000}");
            order.AddLine($"PR-{i:000}", $"Товар {i}", 100m + i * 15.5m, (i % 5) + 1);

            if (i % 3 == 0)
                order.Confirm();

            orders.Add(order);
        }

        return orders;
    }
}