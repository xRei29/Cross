using Core.Domain;
using Core.Dto;

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
            else if (i == 5)
                order.Cancel();

            orders.Add(order);
        }

        return orders;
    }

    public static List<CustomerDto> Customers() =>
    [
        new CustomerDto("CUST-001", "Іван Петренко", "ivan.petrenko@example.com"),
        new CustomerDto("CUST-002", "Олена Коваль", "olena.koval@example.com"),
        new CustomerDto("CUST-003", "Андрій Шевченко", "andriy.shevchenko@example.com"),
        new CustomerDto("CUST-004", "Марія Бондар", "maria.bondar@example.com"),
        new CustomerDto("CUST-005", "Сергій Мельник", "serhiy.melnyk@example.com"),
    ];
}