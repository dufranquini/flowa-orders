using OrderGenerator.Api.Dtos;

namespace OrderGenerator.Api.Repositories;

public class InMemoryOrderHistoryRepository : IOrderHistoryRepository
{
    private readonly List<OrderResponse> _orders = [];

    public Task AddAsync(OrderResponse order, CancellationToken cancellationToken = default)
    {
        _orders.Add(order);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<OrderResponse>>([.. _orders.AsEnumerable().Reverse()]);
    }
}
