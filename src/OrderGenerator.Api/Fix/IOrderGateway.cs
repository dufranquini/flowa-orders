using OrderGenerator.Api.Dtos;

namespace OrderGenerator.Api.Fix;

public interface IOrderGateway
{
    bool IsConnected { get; }
    Task<OrderResponse> SendAsync(NewOrderRequest request, CancellationToken cancellationToken);
}
