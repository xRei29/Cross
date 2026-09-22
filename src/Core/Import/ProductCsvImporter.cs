using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    private const char Separator = ';';

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();
        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 3 } => new ParseFailed($"очікую 3 колонки, отримав {parts.Length}"),
            [_, "", _] => new ParseFailed("назва порожня"),
            [_, _, var price] when !decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p) || p < 0
                => new ParseFailed($"ціна '{price}' не є коректним невід'ємним числом"),
            [var id, var name, var price] => new ParseOk(new ProductDto(id, name,
                decimal.Parse(price, NumberStyles.Number, CultureInfo.InvariantCulture))),
            _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
        };
    }

    private static object ParseMixedLine(string line)
{
    string[] parts = line.Split(';', StringSplitOptions.TrimEntries);
    return parts switch
    {
        ["P", var id, var name, var price] when decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p)
            => new ProductDto(id, name, p),
        ["C", var id, var name, var email]
            => new CustomerDto(id, name, string.IsNullOrWhiteSpace(email) ? null : email),
        _ => throw new FormatException($"Невідомий тип рядка: {line}")
    };
}

    private abstract record ParseOutcome;
    private sealed record ParseOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}

