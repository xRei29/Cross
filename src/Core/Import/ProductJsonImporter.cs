using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path)
    {
        string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var items = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? [];
            return new ImportResult<ProductDto>(items, []);
        }
        catch (JsonException ex)
        {
            return new ImportResult<ProductDto>([], [$"помилка розбору JSON: {ex.Message}"]);
        }
    }
}