using OrderGenerator.Api.Dtos;

namespace OrderGenerator.Api.Repositories;

public interface IOrderHistoryRepository
{
    Task AddAsync(OrderResponse order, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
