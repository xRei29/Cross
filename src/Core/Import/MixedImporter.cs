using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class MixedImporter
{
    private const char Separator = ';';

    public static ImportResult<object> Load(string path)
    {
        var items = new List<object>();
        var errors = new List<string>();
        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

            object? result = parts switch
            {
                ["P", var id, var name, var price] when decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p) && p >= 0
                    => new ProductDto(id, name, p),
                ["C", var id, var name, var email]
                    => new CustomerDto(id, name, string.IsNullOrWhiteSpace(email) ? null : email),
                _ => null
            };

            if (result is not null)
                items.Add(result);
            else
                errors.Add($"рядок {number}: невідомий формат '{line}'");
        }

        return new ImportResult<object>(items, errors);
    }
}