namespace Api.Contracts;

public sealed record SaveOrderLineRequest(string ProductId, string Name, decimal Price, int Quantity);

public sealed record SaveOrderRequest(string CustomerId, List<SaveOrderLineRequest> Lines);
