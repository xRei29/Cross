using Core.Domain;
using Core.Reports;
using Xunit;

namespace Core.Tests;

public class ReportServiceTests
{
    private static Order Make(string id, string customer, decimal price)
    {
        var o = Order.Create(id, customer);
        o.AddLine("P-1", "Товар", price, 1);
        return o;
    }

    [Fact]
    public void Totals_EmptySource_ReturnsZeros()
    {
        var t = ReportService.Totals([]);

        Assert.Equal((0, 0m, 0m, 0m), t);
    }

    [Fact]
    public void TopCustomers_SumsPerCustomer_AndSortsDescending()
    {
        Order[] source = [Make("1", "A", 100m), Make("2", "B", 500m), Make("3", "A", 150m)];

        var top = ReportService.TopCustomers(source, 5);

        Assert.Equal(2, top.Count);
        Assert.Equal("B", top[0].CustomerId);
        Assert.Equal(500m, top[0].TotalAmount);
        Assert.Equal("A", top[1].CustomerId);
        Assert.Equal(250m, top[1].TotalAmount);
    }

    [Fact]
    public void ExpensiveOrders_FiltersByThreshold()
    {
        Order[] source = [Make("1", "A", 100m), Make("2", "B", 500m), Make("3", "C", 300m)];

        var result = ReportService.ExpensiveOrders(source, 300m);

        Assert.Single(result);
        Assert.Equal("2", result[0].Id);
    }
}