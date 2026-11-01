using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;
using Core.Import;

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

string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

void OnOrderChanged(object? sender, OrderChangedEventArgs e)
{
    string line = $"{e.At:HH:mm:ss} {e.Kind,-10} {e.OrderId} поз.:{e.LinesCount,2} сума:{e.Total,8:F2}";
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

service.Changed += OnOrderChanged;

int changes = 0;
service.Changed += (_, _) => changes++;

Order created = service.CreateOrder("CUST-999");
service.AddLine(created.Id, "PR-999", "Тестовий товар", 199.99m, 2);
service.Confirm(created.Id);

Console.WriteLine($"Подій отримано: {changes}");

// service.Changed -= OnOrderChanged;          

Order paid = service.CreateOrder("CUST-998");   
service.AddLine(paid.Id, "PR-998", "Підтверджений товар", 50m, 3);

Console.WriteLine($"Подій отримано: {changes}"); 

//  сценарій відмови 
Console.WriteLine();
Console.WriteLine("=== Сценарій відмови ===");
TryDo("AddLine до неіснуючого замовлення", () => service.AddLine("ORD-NOPE", "PR-1", "X", 1m, 1));
TryDo("Confirm порожнього замовлення", () =>
{
    var empty = service.CreateOrder("CUST-997");   // ця операція сама по собі викличе подію Created — це очікувано
    service.Confirm(empty.Id);                      // а ця — ні, впаде на інваріанті до OnChanged
});

Console.WriteLine();
Console.WriteLine("=== Імпорт CSV з колбеком про пропущені рядки ===");
var importResult = ProductCsvImporter.Load(
    Path.Combine("data", "sample.csv"),
    line => Console.WriteLine($"[пропущено] {line}"));
Console.WriteLine($"Імпортовано: {importResult.Items.Count}, помилок: {importResult.Errors.Count}");


Console.WriteLine();
Console.WriteLine("=== FindAll: замовлення без жодного підтвердженого рядка (Draft) ===");
var drafts = service.Search(o => o.Status == OrderStatus.Draft);
Console.WriteLine($"Знайдено: {drafts.Count}");
foreach (Order o in drafts)
    Console.WriteLine($"  {o}");

//  Додаткове завдання 1
Console.WriteLine("=== INotificationSink: ConsoleSink + FileSink як підписник Changed ===");

string sinkLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
var dispatcher = new NotificationDispatcher([new ConsoleSink(), new FileSink(sinkLogPath)]);

service.Changed += dispatcher.Handle;

Order viaSink = service.CreateOrder("CUST-996");
service.AddLine(viaSink.Id, "PR-996", "Товар через sink", 75m, 1);

service.Changed -= dispatcher.Handle;


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

