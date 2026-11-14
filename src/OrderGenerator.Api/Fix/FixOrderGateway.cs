using Domain;
using OrderGenerator.Api.Dtos;
using QuickFix;
using QuickFix.Fields;
using System.Collections.Concurrent;
using ExecutionReport = QuickFix.FIX44.ExecutionReport;
using NewOrderSingle = QuickFix.FIX44.NewOrderSingle;

namespace OrderGenerator.Api.Fix;

public class FixOrderGateway(IQuickFixSession session, ILogger<FixOrderGateway> logger) : IApplication, IOrderGateway
{
    private SessionID? _sessionId;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<OrderResponse>> _pendingOrders = new();
    public bool IsConnected => _sessionId != null;
    public static readonly TimeSpan DefaultResponseTimeout = TimeSpan.FromSeconds(10);

    public async Task<OrderResponse> SendAsync(NewOrderRequest request, CancellationToken cancellationToken)
    {
        var sessionId = _sessionId ?? throw new Exception("Not connected to FIX session");

        var clOrdId = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<OrderResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingOrders[clOrdId] = completion;

        try
        {
            if (!session.Send(BuildNewOrderSingle(clOrdId, request), sessionId))
                throw new Exception("Failed to send order to FIX session");

            return await completion.Task.WaitAsync(DefaultResponseTimeout, cancellationToken);
        }
        finally
        {
            _pendingOrders.TryRemove(clOrdId, out _);
        }
    }

    public void FromAdmin(Message message, SessionID sessionID)
    {
    }

    public void FromApp(Message message, SessionID sessionID)
    {
        if (message is not ExecutionReport report)
            return;

        OrderResponse response = ToResponse(report);

        if (_pendingOrders.TryGetValue(response.ClOrdId, out var completion))
            completion.TrySetResult(response);
    }
    public void OnLogon(SessionID sessionID)
    {
        _sessionId = sessionID;
        logger.LogInformation("Connected to FIX session: {SessionID}", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        _sessionId = null;
        logger.LogInformation("Disconnected from FIX session: {SessionID}", sessionID);
    }

    public void OnCreate(SessionID sessionID)
    {
    }

    public void ToAdmin(Message message, SessionID sessionID)
    {
    }

    public void ToApp(Message message, SessionID sessionID)
    {
    }

    public static NewOrderSingle BuildNewOrderSingle(string clOrdId, NewOrderRequest request)
    {
        NewOrderSingle order = new();
        order.SetField(new ClOrdID(clOrdId));
        order.SetField(new Symbol(request.Symbol));
        order.SetField(new Side(request.Side == OrderSide.Sell ? Side.SELL : Side.BUY));
        order.SetField(new TransactTime(DateTime.UtcNow));
        order.SetField(new OrdType(OrdType.LIMIT));
        order.SetField(new OrderQty(request.Quantity));
        order.SetField(new Price(request.Price));
        return order;
    }

    public static OrderResponse ToResponse(ExecutionReport report) => 
        new(ClOrdId: report.GetString(Tags.ClOrdID),
            OrderId: report.GetString(Tags.OrderID), 
            ExecId: report.GetString(Tags.ExecID), 
            Status: report.GetChar(Tags.ExecType) == ExecType.NEW ? OrderStatus.New : OrderStatus.Rejected,
            Symbol: report.GetString(Tags.Symbol), 
            Side: report.GetChar(Tags.Side) == Side.SELL ? OrderSide.Sell : OrderSide.Buy,
            Quantity: (int)report.GetDecimal(Tags.OrderQty), 
            Price: report.GetDecimal(Tags.Price), 
            Text: report.IsSetField(Tags.Text) ? report.GetString(Tags.Text) : null, 
            RejectReason: report.IsSetField(Tags.OrdRejReason) ? report.GetInt(Tags.OrdRejReason) : null, 
            ReceivedAt: DateTimeOffset.UtcNow);
}
