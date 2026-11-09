using Core.Domain;
using Core.Events;
using Xunit;

namespace Core.Tests;

public class OrderServiceTests
{
    // ---------- Бізнес-правила (DomainException) ----------

    [Fact]
    public void AddLine_ToConfirmedOrder_ThrowsDomainException()
    {
        // Arrange
        var service = TestData.NewService();
        Order order = service.CreateOrder("C-1");
        service.AddLine(order.Id, "P-1", "Товар", 50m, 1);
        service.Confirm(order.Id);

        // Act + Assert
        Assert.Throws<DomainException>(() => service.AddLine(order.Id, "P-2", "Інший", 10m, 1));
    }

    [Fact]
    public void Confirm_EmptyOrder_ThrowsDomainException()
    {
        var service = TestData.NewService();
        Order order = service.CreateOrder("C-1");

        Assert.Throws<DomainException>(() => service.Confirm(order.Id));
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_ThrowsDomainException()
    {
        var service = TestData.NewService();
        Order order = service.CreateOrder("C-1");
        service.AddLine(order.Id, "P-1", "Товар", 50m, 1);
        service.Confirm(order.Id);

        Assert.Throws<DomainException>(() => service.Confirm(order.Id));
    }

    [Fact]
    public void Cancel_ConfirmedOrder_IsAllowed_ButCancelTwice_Throws()
    {
        Order order = TestData.OrderWithLine();
        order.Confirm();
        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void AddLine_UnknownOrder_ThrowsDomainException()
    {
        var service = TestData.NewService();

        var ex = Assert.Throws<DomainException>(() => service.AddLine("NOPE", "P-1", "Товар", 10m, 1));
        Assert.Contains("NOPE", ex.Message);
    }

    // ---------- Некоректні аргументи (ArgumentException та нащадки) ----------

    [Theory]
    [InlineData("", "Товар", 10, 1)]      // порожній productId
    [InlineData("P-1", "", 10, 1)]        // порожня назва
    [InlineData("P-1", "Товар", -5, 1)]   // від'ємна ціна
    [InlineData("P-1", "Товар", 10, 0)]   // нульова кількість
    [InlineData("P-1", "Товар", 10, -3)]  // від'ємна кількість
    public void AddLine_InvalidArguments_ThrowsArgumentException(string productId, string name, int price, int quantity)
    {
        Order order = Order.Create("T-1", "C-1");

        // ThrowsAny: ArgumentOutOfRangeException — нащадок ArgumentException, а Throws<T> вимагає точного типу.
        Assert.ThrowsAny<ArgumentException>(() => order.AddLine(productId, name, price, quantity));
    }

    [Fact]
    public void Create_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Order.Create("  ", "C-1"));
    }

    // ---------- Обчислення ----------

    [Fact]
    public void Total_SumsAllLines()
    {
        var service = TestData.NewService();
        Order order = service.CreateOrder("C-1");

        service.AddLine(order.Id, "P-1", "Товар", 50m, 2);   // 100
        service.AddLine(order.Id, "P-2", "Інший", 25.5m, 4); // 102

        Assert.Equal(202m, service.Find(order.Id)!.Total);
    }

    // ---------- Події ----------

    [Fact]
    public void Changed_IsRaised_ForCreateAddLineAndConfirm()
    {
        var service = TestData.NewService();
        var kinds = new List<OrderChangeKind>();
        service.Changed += (_, e) => kinds.Add(e.Kind);

        Order order = service.CreateOrder("C-1");
        service.AddLine(order.Id, "P-1", "Товар", 50m, 1);
        service.Confirm(order.Id);

        Assert.Equal(new[] { OrderChangeKind.Created, OrderChangeKind.LineAdded, OrderChangeKind.Confirmed }, kinds);
    }

    [Fact]
    public void Changed_IsNotRaised_WhenOperationFails()
    {
        var service = TestData.NewService();
        Order order = service.CreateOrder("C-1");
        int count = 0;
        service.Changed += (_, _) => count++;

        Assert.Throws<DomainException>(() => service.Confirm(order.Id)); // порожнє замовлення

        Assert.Equal(0, count);
    }

    // ---------- DTO round-trip ----------

    [Fact]
    public void ToDto_FromDto_PreservesData()
    {
        Order original = TestData.OrderWithLine("R-1", "C-9");
        original.Confirm();

        Order restored = Order.FromDto(original.ToDto());

        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Status, restored.Status);
        Assert.Equal(original.Total, restored.Total);
    }
}