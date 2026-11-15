using Domain;
using Microsoft.Extensions.Logging.Abstractions;
using OrderGenerator.Api.Dtos;
using OrderGenerator.Api.Fix;
using QuickFix;
using QuickFix.Fields;

namespace OrderGenerator.Api.Tests.Fix;

public class FixOrderGatewayTests
{
    private static readonly SessionID Session = new("FIX.4.4", "ORDER_GENERATOR", "ORDER_ACCUMULATOR");
    private static readonly NewOrderRequest Request = new("VALE3", OrderSide.Sell, 200, 61.15m);

    private class FakeQuickFixSession : IQuickFixSession
    {
        public List<Message> Sent { get; } = [];
        public bool SendResult { get; set; } = true;
        public Action<Message>? OnSend { get; set; }

        public bool Send(Message message, SessionID sessionID)
        {
            Sent.Add(message);
            OnSend?.Invoke(message);
            return SendResult;
        }
    }

    private static FixOrderGateway CreateGateway(FakeQuickFixSession session) =>
      new(session, NullLogger<FixOrderGateway>.Instance);

    [Fact]
    public void BuildNewOrderSingle_ValidRequest_SetsTheFixFields()
    {
        // Arrange
        var clOrdId = "abc";
        
        // Act
        var order = FixOrderGateway.BuildNewOrderSingle(clOrdId, Request);

        // Assert
        Assert.Equal("abc", order.GetString(Tags.ClOrdID));
        Assert.Equal("VALE3", order.GetString(Tags.Symbol));
        Assert.Equal(Side.SELL, order.GetChar(Tags.Side));
        Assert.Equal(OrdType.LIMIT, order.GetChar(Tags.OrdType));
        Assert.Equal(200m, order.GetDecimal(Tags.OrderQty));
        Assert.Equal(61.15m, order.GetDecimal(Tags.Price));
        Assert.True(order.IsSetField(Tags.TransactTime));
    }

    [Fact]
    public void BuildNewOrderSingle_BuyRequest_SetsSideBuy()
    {
        // Arrange
        var request = Request with { Side = OrderSide.Buy };
        
        // Act
        var order = FixOrderGateway.BuildNewOrderSingle("abc", request);

        // Assert
        Assert.Equal(Side.BUY, order.GetChar(Tags.Side));
    }

    [Fact]
    public void IsConnected_FollowsLogonAndLogout()
    {
        // Arrange
        var gateway = CreateGateway(new FakeQuickFixSession());
        var beforeLogon = gateway.IsConnected;

        // Act
        gateway.OnLogon(Session);
        var afterLogon = gateway.IsConnected;
        gateway.OnLogout(Session);
        var afterLogout = gateway.IsConnected;

        // Assert
        Assert.False(beforeLogon);
        Assert.True(afterLogon);
        Assert.False(afterLogout);
    }

    [Fact]
    public async Task SendAsync_BeforeLogon_ThrowsAndSendsNothing()
    {
        // Arrange
        var session = new FakeQuickFixSession();
        var gateway = CreateGateway(session);

        // Act
        var exception = await Record.ExceptionAsync(() => gateway.SendAsync(Request, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.Empty(session.Sent);
    }

    [Fact]
    public async Task SendAsync_WhenTheSessionRefusesToSend_Throws()
    {
        // Arrange
        var gateway = CreateGateway(new FakeQuickFixSession { SendResult = false });
        gateway.OnLogon(Session);

        // Act
        var exception = await Record.ExceptionAsync(() => gateway.SendAsync(Request, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.Contains("Failed to send", exception.Message);
    }
}
