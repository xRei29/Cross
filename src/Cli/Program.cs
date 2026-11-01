using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Notifications;
using Core.Reports;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

IOrderStore store = useFile ? new FileOrderStore(dataPath) : new InMemoryOrderStore(SampleData.Orders());
if (args.Contains("--cache"))
    store = new CachingOrderStore(store);

var service = new OrderService(store);
Console.WriteLine($"Сховище: {store.GetType().Name}");

// === Підписка на подію Changed: вивід у консоль + запис у лог-файл ===
string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

void OnOrderChanged(object? sender, OrderChangedEventArgs e)
{
    string line = $"{e.At:HH:mm:ss} {e.Kind,-10} {e.OrderId} поз.: {e.LinesCount} сума: {e.Total:F2}";
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

service.Changed -= OnOrderChanged;

Order paid = service.CreateOrder("CUST-998");
service.AddLine(paid.Id, "PR-998", "Підтверджений товар", 50m, 3);
Console.WriteLine($"Подій отримано: {changes}");

// === Пошук за id ===
Console.WriteLine();
Console.WriteLine("=== Пошук за id ===");
Order? found = service.Find(created.Id);
Console.WriteLine(found is not null ? $"  Знайдено: {found}" : "  Не знайдено");

// === Сценарій відмови ===
Console.WriteLine();
Console.WriteLine("=== Сценарій відмови ===");
TryDo("AddLine до неіснуючого замовлення", () => service.AddLine("ORD-NOPE", "PR-001", "Товар", 10m, 1));
Order empty = service.CreateOrder("CUST-997");
TryDo("Confirm порожнього замовлення", () => service.Confirm(empty.Id));

// === Імпорт CSV з колбеком про пропущені рядки ===
Console.WriteLine();
Console.WriteLine("=== Імпорт CSV з колбеком про пропущені рядки ===");
var importResult = ProductCsvImporter.Load(
    Path.Combine("data", "sample.csv"),
    line => Console.WriteLine($"[пропущено] {line}"));
Console.WriteLine($"Імпортовано: {importResult.Items.Count}, помилок: {importResult.Errors.Count}");

// === FindAll: замовлення без жодного підтвердженого рядка (Draft) ===
Console.WriteLine();
Console.WriteLine("=== FindAll: замовлення без жодного підтвердженого рядка (Draft) ===");
var drafts = service.Search(o => o.Status == OrderStatus.Draft);
Console.WriteLine($"Знайдено: {drafts.Count}");
foreach (var d in drafts)
    Console.WriteLine($"  {d}");

// === INotificationSink: ConsoleSink + FileSink як підписник Changed ===
Console.WriteLine();
Console.WriteLine("=== INotificationSink: ConsoleSink + FileSink як підписник Changed ===");
string sinkLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
var dispatcher = new NotificationDispatcher([new ConsoleSink(), new FileSink(sinkLogPath)]);
service.Changed += dispatcher.Handle;

Order viaSink = service.CreateOrder("CUST-996");
service.AddLine(viaSink.Id, "PR-996", "Товар через sink", 75m, 1);

service.Changed -= dispatcher.Handle;

// === Демо відкладеного виконання ===
Console.WriteLine();
Console.WriteLine("=== Демо відкладеного виконання ===");
var source = service.All().ToList();
var deferredQuery = source.Where(o => o.Total > 300m);
Console.WriteLine($"Дорогих до змін: {deferredQuery.Count()}");

var tmp = Order.Create("TMP-001", "CUST-999");
tmp.AddLine("PR-999", "Тимчасове замовлення", 500m, 1);
source.Add(tmp);
Console.WriteLine($"Дорогих після змін: {deferredQuery.Count()}");

var snapshot = deferredQuery.ToList();
source.Clear();
Console.WriteLine($"У знімку лишилось: {snapshot.Count}");

// === Звіти ===
Console.WriteLine();
Console.WriteLine("=== Звіти ===");
var items = service.All();
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

switch (command)
{
    case "report-top":
        PrintCustomers(ReportService.TopCustomers(items, 5));
        break;
    case "report-expensive":
        PrintOrders(ReportService.ExpensiveOrders(items, 300m));
        break;
    case "report-status":
        foreach (var g in ReportService.ByStatus(items))
            Console.WriteLine($"  {g.Key,-10} позицій: {g.Count,3}  сумарно: {g.TotalAmount,10:F2}");
        break;
    case "report-sum":
        var t = ReportService.Totals(items);
        Console.WriteLine($"  позицій {t.Items}, всього {t.Total:F2}, середній чек {t.Average:F2}, макс {t.Max:F2}");
        break;
    default:
        Console.WriteLine("report-top | report-expensive | report-status | report-sum");
        break;
}

// === Додаткове завдання 1: GroupJoin клієнтів і замовлень ===
Console.WriteLine();
Console.WriteLine("=== GroupJoin: клієнти × замовлення ===");
foreach (var c in ReportService.CustomersWithTotals(SampleData.Customers(), items))
    Console.WriteLine($"  {c.CustomerId,-10} {c.CustomerName,-20} замовлень: {c.OrdersCount,3}  сума: {c.TotalAmount,10:F2}");

// === Додаткове завдання 2: власний extension-метод TopBy<T> ===
Console.WriteLine();
Console.WriteLine("=== TopBy<T>: топ-5 найдорожчих замовлень ===");
PrintOrders(ReportService.TopOrders(items, 5));

Console.WriteLine();
Console.WriteLine("=== TopBy<T>: топ-3 клієнтів за кількістю замовлень ===");
PrintCustomers(ReportService.TopCustomersByOrderCount(items, 3));

// === Додаткове завдання 3: query syntax, звірка з report-status ===
Console.WriteLine();
Console.WriteLine("=== Query syntax: групування за статусом (має збігатись з report-status) ===");
foreach (var g in ReportService.ByStatusQuerySyntax(items))
    Console.WriteLine($"  {g.Key,-10} позицій: {g.Count,3}  сумарно: {g.TotalAmount,10:F2}");

// === Додаткове завдання 4: Stopwatch на 100 000 записів (лише з --bench) ===
if (args.Contains("--bench"))
{
    Console.WriteLine();
    Console.WriteLine("=== Stopwatch: 100 000 записів, з ToList у середині ланцюжка і без нього ===");

    List<Order> bigSource = GenerateManyOrders(100_000);

    var sw1 = System.Diagnostics.Stopwatch.StartNew();
    var withoutMidToList = bigSource
        .Where(o => o.Total > 300m)
        .OrderByDescending(o => o.Total)
        .Take(5)
        .ToList();
    sw1.Stop();

    var sw2 = System.Diagnostics.Stopwatch.StartNew();
    var filteredFirst = bigSource.Where(o => o.Total > 300m).ToList();
    var withMidToList = filteredFirst
        .OrderByDescending(o => o.Total)
        .Take(5)
        .ToList();
    sw2.Stop();

    Console.WriteLine($"Без проміжного ToList: {sw1.ElapsedMilliseconds} мс, знайдено {withoutMidToList.Count}");
    Console.WriteLine($"З проміжним ToList:    {sw2.ElapsedMilliseconds} мс, знайдено {withMidToList.Count}");
}

static List<Order> GenerateManyOrders(int count)
{
    var list = new List<Order>(count);
    for (int i = 1; i <= count; i++)
    {
        var o = Order.Create($"GEN-{i}", $"CUST-{(i % 50) + 1:000}");
        o.AddLine($"PR-{i}", "Товар", 10m + (i % 500), (i % 5) + 1);
        list.Add(o);
    }
    return list;
}

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

static void PrintCustomers(IReadOnlyList<CustomerTotal> lines)
{
    foreach (var l in lines)
        Console.WriteLine($"  {l.CustomerId,-10} замовлень: {l.OrdersCount,3}  сума: {l.TotalAmount,10:F2}");
}

static void PrintOrders(IReadOnlyList<OrderReportLine> lines)
{
    foreach (var l in lines)
        Console.WriteLine($"  {l.Id,-10} {l.CustomerId,-10} {l.Total,10:F2}  {l.Status}");
}