using Core.Domain;
using Core.Dto;

namespace Core.Tests;

public class OrderCreatedAtTests
{
    [Fact]
    public void Create_SetsCreatedAt_ToCurrentMoment()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;
        Order order = Order.Create("O-1", "C-1");
        DateTimeOffset after = DateTimeOffset.UtcNow;

        Assert.InRange(order.CreatedAt, before, after);
    }

    [Fact]
    public void Create_WithExplicitCreatedAt_KeepsIt()
    {
        var moment = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

        Order order = Order.Create("O-1", "C-1", moment);

        Assert.Equal(moment, order.CreatedAt);
    }

    [Fact]
    public void ToDto_FromDto_PreservesCreatedAt()
    {
        var moment = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
        Order original = Order.Create("O-1", "C-1", moment);
        original.AddLine("P-1", "Товар", 10m, 2);

        Order restored = Order.FromDto(original.ToDto());

        Assert.Equal(moment, restored.CreatedAt);
        Assert.Equal(20m, restored.Total);
    }

    [Fact]
    public void FromDto_WithoutCreatedAt_UsesCurrentMoment()
    {
        var dto = new OrderDto("O-1", "C-1", OrderStatus.Draft, new List<OrderLineDto>());
        DateTimeOffset before = DateTimeOffset.UtcNow;

        Order order = Order.FromDto(dto);

        Assert.InRange(order.CreatedAt, before, DateTimeOffset.UtcNow);
    }
}