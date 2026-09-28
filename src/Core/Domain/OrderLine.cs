namespace Core.Domain;

public sealed class OrderLine
{
    public string ProductId { get; }
    public string Name { get; }
    public decimal Price { get; }
    public int Quantity { get; }

    public OrderLine(string productId, string name, decimal price, int quantity)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        Quantity = quantity;
    }

    public decimal LineTotal => Price * Quantity;

    public override string ToString() => $"{ProductId} {Name} x{Quantity} @ {Price:F2} = {LineTotal:F2}";
}