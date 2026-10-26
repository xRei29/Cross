using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
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

var service = new OrderService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");

// --- Крок 3: підписка локальним методом, консоль + лог-файл ---
string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

void OnOrderChanged(object? sender, OrderChangedEventArgs e)
{
    string line = $"{e.At:HH:mm:ss} {e.Kind,-10} {e.OrderId} поз.:{e.LinesCount,2} сума:{e.Total,8:F2}";
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

service.Changed += OnOrderChanged;

// --- Крок 4: другий підписник — лямбда із замиканням над локальною змінною ---
int changes = 0;
service.Changed += (_, _) => changes++;

// --- Крок 5: сценарій з кількома змінами, підрахунком і відпискою ---
Order created = service.CreateOrder("CUST-999");
service.AddLine(created.Id, "PR-999", "Тестовий товар", 199.99m, 2);
service.Confirm(created.Id);

Console.WriteLine($"Подій отримано: {changes}");

service.Changed -= OnOrderChanged;          // відписуємось лише від логування

Order paid = service.CreateOrder("CUST-998");   // ця операція — "тиха": у консолі й файлі нічого не буде
service.AddLine(paid.Id, "PR-998", "Підтверджений товар", 50m, 3);

Console.WriteLine($"Подій отримано: {changes}"); // зросло лише завдяки лямбді-лічильнику, яка й далі підписана

// --- Крок 6: сценарій відмови — виняток є, події немає ---
Console.WriteLine();
Console.WriteLine("=== Сценарій відмови ===");
TryDo("AddLine до неіснуючого замовлення", () => service.AddLine("ORD-NOPE", "PR-1", "X", 1m, 1));
TryDo("Confirm порожнього замовлення", () =>
{
    var empty = service.CreateOrder("CUST-997");   // ця операція сама по собі ВИКЛИЧЕ подію Created — це очікувано
    service.Confirm(empty.Id);                      // а ця — ні, впаде на інваріанті до OnChanged
});

// --- Крок 7: делегат Func замість події — коли потрібне значення у відповідь ---
Console.WriteLine();
Console.WriteLine("=== FindAll: замовлення без жодного підтвердженого рядка (Draft) ===");
var drafts = service.Search(o => o.Status == OrderStatus.Draft);
Console.WriteLine($"Знайдено: {drafts.Count}");
foreach (Order o in drafts)
    Console.WriteLine($"  {o}");

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