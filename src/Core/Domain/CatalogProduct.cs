using Core.Dto;

namespace Core.Domain;

public sealed class CatalogProduct
{
    public string Id { get; }
    public string Name { get; }
    public decimal Price { get; }

    private CatalogProduct(string id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public static CatalogProduct Create(string id, string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор товару обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва товару обов'язкова", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price, "Ціна не може бути від'ємною");

        return new CatalogProduct(id.Trim(), name.Trim(), price);
    }

    public ProductDto ToDto() => new(Id, Name, Price);
    public static CatalogProduct FromDto(ProductDto dto) => Create(dto.Id, dto.Name, dto.Price);

    public override string ToString() => $"{Id} {Name} — {Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}";
}