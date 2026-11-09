using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

internal static class TestData
{
    public static OrderService NewService() => new(new InMemoryOrderStore());

    /// <summary>Створює замовлення з одним рядком: 2 × 50 = 100.</summary>
    public static Order OrderWithLine(string id = "T-1", string customerId = "C-1")
    {
        var order = Order.Create(id, customerId);
        order.AddLine("P-1", "Товар", 50m, 2);
        return order;
    }

    public static string WriteTempFile(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"orders-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }

    public const string ValidJson = """
        [
          { "id": "J-1", "customerId": "C-1", "status": "Draft",
            "lines": [ { "productId": "P-1", "name": "Товар", "price": 10.5, "quantity": 2 } ] },
          { "id": "J-2", "customerId": "C-2", "status": "Confirmed",
            "lines": [ { "productId": "P-2", "name": "Інший", "price": 100, "quantity": 1 } ] }
        ]
        """;
}