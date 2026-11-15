using Domain;
using OrderGenerator.Api.Dtos;
using OrderGenerator.Api.Fix;

namespace OrderGenerator.Api.Tests;

public class FakeOrderGateway : IOrderGateway
{
    private readonly List<NewOrderRequest> _received = [];

    public bool IsConnected { get; set; } = true;

    public Func<NewOrderRequest, OrderResponse> Respond { get; set; } = Accepted;

    public IReadOnlyList<NewOrderRequest> Received
    {
        get { lock (_received) return [.. _received]; }
    }

    public Task<OrderResponse> SendAsync(NewOrderRequest request, CancellationToken cancellationToken)
    {
        lock (_received)
            _received.Add(request);

        return Task.FromResult(Respond(request));
    }

    public static OrderResponse Accepted(NewOrderRequest request) =>
      Build(request, OrderStatus.New, "Order accepted.", rejectReason: null);

    public static OrderResponse Rejected(NewOrderRequest request, string text) =>
      Build(request, OrderStatus.Rejected, text, rejectReason: 3);

    private static OrderResponse Build(NewOrderRequest request, OrderStatus status, string text, int? rejectReason) => new(
      ClOrdId: Guid.NewGuid().ToString("N"),
      OrderId: "ORD-00000001",
      ExecId: "EXE-00000001",
      Status: status,
      Symbol: request.Symbol!,
      Side: request.Side ?? OrderSide.Buy,
      Quantity: request.Quantity,
      Price: request.Price,
      Text: text,
      RejectReason: rejectReason,
      ReceivedAt: DateTimeOffset.UtcNow);
}