using Core.Domain;

namespace Core.Reports;

public sealed record OrderReportLine(string Id, string CustomerId, decimal Total, OrderStatus Status);
public sealed record CustomerTotal(string CustomerId, int OrdersCount, decimal TotalAmount);
public sealed record GroupSummary(string Key, int Count, decimal TotalAmount);
public sealed record CustomerOrderSummary(string CustomerId, string CustomerName, int OrdersCount, decimal TotalAmount);