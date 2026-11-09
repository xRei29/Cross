using Core.Domain;
using Core.Dto;

namespace Core.Reports;

public static class ReportService
{
    //  топ-N клієнтів за сумарною вартістю замовлень: GroupBy + Sum + стабільний топ-N
    public static IReadOnlyList<CustomerTotal> TopCustomers(IEnumerable<Order> source, int n) =>
        source.GroupBy(o => o.CustomerId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CustomerTotal(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(c => c.TotalAmount)
            .ThenBy(c => c.CustomerId, StringComparer.Ordinal)
            .Take(n)
            .ToList();

    //  фільтр + сортування + проєкція: замовлення дорожче 
    public static IReadOnlyList<OrderReportLine> ExpensiveOrders(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total > threshold)
            .OrderByDescending(o => o.Total)
            .ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status))
            .ToList();

    //  групування за статусом з підрахунком і сумою
    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status.ToString())
            .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(s => s.TotalAmount)
            .ToList();

    //  агрегати з обробкою порожньої послідовності
    public static (int Items, decimal Total, decimal Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0m, 0m, 0m)
            : (list.Count, list.Sum(o => o.Total), list.Average(o => o.Total), list.Max(o => o.Total));
    }

public static IReadOnlyList<CustomerOrderSummary> CustomersWithTotals(
    IEnumerable<CustomerDto> customers, IEnumerable<Order> orders) =>
    customers.GroupJoin(orders,
            c => c.Id, o => o.CustomerId,
            (c, custOrders) => new CustomerOrderSummary(c.Id, c.Name, custOrders.Count(), custOrders.Sum(o => o.Total)))
        .OrderByDescending(s => s.TotalAmount)
        .ToList();

        public static IReadOnlyList<OrderReportLine> TopOrders(IEnumerable<Order> source, int n) =>
    source.TopBy(o => o.Total, n)
        .Select(o => new OrderReportLine(o.Id, o.CustomerId, o.Total, o.Status))
        .ToList();

public static IReadOnlyList<CustomerTotal> TopCustomersByOrderCount(IEnumerable<Order> source, int n) =>
    source.GroupBy(o => o.CustomerId)
        .Select(g => new CustomerTotal(g.Key, g.Count(), g.Sum(o => o.Total)))
        .TopBy(c => c.OrdersCount, n);

        public static IReadOnlyList<GroupSummary> ByStatusQuerySyntax(IEnumerable<Order> source)
{
    var query =
        from o in source  
        group o by o.Status.ToString() into g // Групуємо за статусом
        orderby g.Sum(x => x.Total) descending // Сортуємо групи за сумою
        select new GroupSummary(g.Key, g.Count(), g.Sum(x => x.Total)); // формуєм результат

    return query.ToList(); 
}

}