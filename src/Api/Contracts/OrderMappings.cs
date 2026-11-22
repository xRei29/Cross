using Core.Domain;

namespace Api.Contracts;

public static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order) => new(
        order.Id,
        order.CustomerId,
        order.Status.ToString(),
        order.CreatedAt,
        order.Lines
            .Select(l => new OrderLineResponse(l.ProductId, l.Name, l.Price, l.Quantity, l.LineTotal))
            .ToList(),
        order.Total);
}
