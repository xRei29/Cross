namespace Api.Contracts;

public sealed record OrderLineResponse(string ProductId, string Name, decimal Price, int Quantity, decimal LineTotal);

public sealed record OrderResponse(
    string Id,
    string CustomerId,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineResponse> Lines,
    decimal Total);

public sealed record OrderTotalResponse(string OrderId, decimal Total, string Currency);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
