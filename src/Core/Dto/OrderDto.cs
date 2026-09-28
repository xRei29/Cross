using Core.Domain;

namespace Core.Dto;

public record OrderLineDto(string ProductId, string Name, decimal Price, int Quantity);

public record OrderDto(string Id, string CustomerId, OrderStatus Status, IReadOnlyList<OrderLineDto> Lines);