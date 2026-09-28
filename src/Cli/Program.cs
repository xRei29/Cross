using Core.Domain;
using Core.Dto;
using Core.Import;

Console.WriteLine("=== Сценарій 1: успіх ===");
Order order = Order.Create("ORD-001", "CUST-001");
order.AddLine("PR-001", "Ноутбук Lenovo IdeaPad", 24999.99m, 1);
order.AddLine("PR-002", "Миша бездротова Logitech", 399.50m, 2);
Console.WriteLine(order);
order.Confirm();
Console.WriteLine(order);
Console.WriteLine();

Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
TryDo("додавання рядка до підтвердженого замовлення", () => order.AddLine("PR-003", "Клавіатура", 100m, 1));
TryDo("від'ємна ціна", () => Order.Create("ORD-002", "CUST-002").AddLine("PR-004", "Тест", -10m, 1));
TryDo("нульова кількість", () => Order.Create("ORD-003", "CUST-003").AddLine("PR-005", "Тест", 50m, 0));
TryDo("підтвердження порожнього замовлення", () => Order.Create("ORD-004", "CUST-004").Confirm());
TryDo("повторне підтвердження", () => order.Confirm());
TryDo("скасування вже скасованого замовлення", () => {
    var temp = Order.Create("ORD-999", "CUST-999");
    temp.Cancel();
    temp.Cancel(); // друга спроба — має впасти
});
Console.WriteLine();

Console.WriteLine("=== Додатково: імпорт + доменні перевірки (зв'язок з тижнем 3) ===");
ImportResult<ProductDto> importResult = ProductCsvImporter.Load(Path.Combine("data", "sample.csv"));
ImportResult<CatalogProduct> catalogResult = CatalogProductBuilder.Build(importResult);
Console.WriteLine($"Каталог: {catalogResult.Items.Count} валідних товарів, {catalogResult.Errors.Count} помилок");
foreach (string e in catalogResult.Errors)
    Console.WriteLine($"  ! {e}");

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}