using Core.Dto;
using Core.Import;
using System.Globalization;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

if (path.Contains("mixed"))
{
    var mixedResult = MixedImporter.Load(path);
    Console.WriteLine($"Змішаний імпорт: {mixedResult.Items.Count} записів");
    foreach (var item in mixedResult.Items)
    {
        string line = item switch
        {
            ProductDto p => $"  [Товар] {p.Id} {p.Name} {p.Price.ToString("F2", CultureInfo.InvariantCulture)}",
            CustomerDto c => $"  [Клієнт] {c.Id} {c.Name} {c.Email ?? "(без email)"}",
            _ => "  [Невідомо]"
        };
        Console.WriteLine(line);
    }
    return 0;
}

ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    var ext => throw new NotSupportedException($"Формат '{ext}' не підтримується")
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
foreach (ProductDto p in result.Items.Take(5))
    Console.WriteLine($"  {p.Id,-8} {p.Name,-30} {p.Price.ToString("F2", CultureInfo.InvariantCulture),10}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($"  ! {e}");

    int total = result.Items.Count + result.Errors.Count;
    double errorRate = total > 0 ? result.Errors.Count * 100.0 / total : 0;
    Console.WriteLine($"Всього: {total}, прийнято: {result.Items.Count}, пропущено: {result.Errors.Count}, помилок: {errorRate.ToString("F1", CultureInfo.InvariantCulture)}%");
}

return 0;