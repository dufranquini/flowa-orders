using Domain;
using OrderGenerator.Api.Dtos;
using OrderGenerator.Api.Repositories;

namespace OrderGenerator.Api.Tests.Repositories;

public class InMemoryOrderHistoryRepositoryTests
{
    private static OrderResponse CreateOrder(string clOrdId) => new(
      ClOrdId: clOrdId, OrderId: "ORD", ExecId: "EXE", Status: OrderStatus.New,
      Symbol: "PETR4", Side: OrderSide.Buy, Quantity: 1, Price: 1m,
      Text: null, RejectReason: null, ReceivedAt: DateTimeOffset.UtcNow);


    [Fact]
    public async Task GetAllAsync_AfterThreeOrders_ReturnsAllNewestFirst()
    {
        // Arrange
        var repository = new InMemoryOrderHistoryRepository();
        await repository.AddAsync(CreateOrder("A"));
        await repository.AddAsync(CreateOrder("B"));
        await repository.AddAsync(CreateOrder("C"));

        // Act
        var all = await repository.GetAllAsync();

        // Assert
        string[] expected = ["C", "B", "A"];
        Assert.Equal(expected, all.Select(o => o.ClOrdId).ToArray());
    }

    [Fact]
    public async Task GetAllAsync_ReturnedList_DoesNotChangeWhenNewOrdersArrive()
    {
        // Arrange
        var repository = new InMemoryOrderHistoryRepository();
        await repository.AddAsync(CreateOrder("A"));
        var snapshot = await repository.GetAllAsync();

        // Act
        await repository.AddAsync(CreateOrder("B"));

        // Assert
        Assert.Single(snapshot);
    }

    [Fact]
    public async Task GetAllAsync_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        var repository = new InMemoryOrderHistoryRepository();

        // Act
        var all = await repository.GetAllAsync();

        // Assert
        Assert.Empty(all);
    }
}
