using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

IOrderStore store = StoreFactory.Create(args, dataPath);

var service = new OrderService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");

Order created = service.CreateOrder("CUST-999");
service.AddLine(created.Id, "PR-999", "Тестовий товар", 199.99m, 2);

Console.WriteLine();
Console.WriteLine("=== Усі замовлення ===");
foreach (Order o in service.All())
    Console.WriteLine($"  {o}");

Console.WriteLine();
Console.WriteLine("=== Пошук: підтверджені замовлення ===");
var confirmed = store.Find(o => o.Status == OrderStatus.Confirmed);
Console.WriteLine($"Знайдено: {confirmed.Count}");
foreach (Order o in confirmed)
    Console.WriteLine($"  {o}");

Console.WriteLine();
Console.WriteLine("=== Сценарій відмови ===");
TryDo("повторне створення з тим самим id (дублікат)", () =>
{
    var dup = Order.Create(created.Id, "CUST-1");
    store.Add(dup);
});
TryDo("AddLine до неіснуючого замовлення", () => service.AddLine("ORD-NOPE", "PR-1", "X", 1m, 1));

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}