using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

IOrderStore store = useFile
    ? new FileOrderStore(dataPath)
    : new InMemoryOrderStore(SampleData.Orders());

if (args.Contains("--cache"))
    store = new CachingOrderStore(store);

// Альтернатива (додаткове завдання 3): IOrderStore store = StoreFactory.Create(args, dataPath);

var service = new OrderService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");

Order created = service.CreateOrder("CUST-999");
service.AddLine(created.Id, "PR-999", "Тестовий товар", 199.99m, 2);

Order paid = service.CreateOrder("CUST-998");
service.AddLine(paid.Id, "PR-998", "Підтверджений товар", 50m, 3);
service.Confirm(paid.Id);

Console.WriteLine();
Console.WriteLine("=== Усі замовлення ===");
foreach (Order o in service.All())
    Console.WriteLine($"  {o}");

Console.WriteLine();
Console.WriteLine("=== Пошук за id ===");
Console.WriteLine($"  {service.Find(created.Id)}");

Console.WriteLine();
Console.WriteLine("=== Пошук: підтверджені замовлення ===");
var confirmed = service.Search(o => o.Status == OrderStatus.Confirmed);
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