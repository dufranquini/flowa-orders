using Domain;
using QuickFix.Fields;
using QuickFix.FIX44;

namespace OrderAccumulator.Tests;

public class NewOrderSingleHandlerTests
{
    private static NewOrderSingle CreateOrder(string symbol, char side, decimal quantity, decimal price, string clOrdId = "CL-1")
    {
        var order = new NewOrderSingle();
        order.SetField(new ClOrdID(clOrdId));
        order.SetField(new Symbol(symbol));
        order.SetField(new Side(side));
        order.SetField(new TransactTime(DateTime.UtcNow));
        order.SetField(new OrdType(OrdType.LIMIT));
        order.SetField(new OrderQty(quantity));
        order.SetField(new Price(price));
        return order;
    }

    [Fact]
    public void Handle_BuyWithinLimit_ReturnsNewAndIncreasesExposure()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure());
        var order = CreateOrder("PETR4", Side.BUY, 100, 35.42m, clOrdId: "ABC");

        // Act
        var report = handler.Handle(order);

        // Assert
        Assert.Equal(ExecType.NEW, report.GetChar(Tags.ExecType));
        Assert.Equal(OrdStatus.NEW, report.GetChar(Tags.OrdStatus));
        Assert.Equal("ABC", report.GetString(Tags.ClOrdID));
        Assert.Equal("PETR4", report.GetString(Tags.Symbol));
        Assert.Equal(100m, report.GetDecimal(Tags.LeavesQty));
        Assert.Equal(3_542m, handler.Exposure.GetExposure("PETR4"));
    }

    [Fact]
    public void Handle_Sell_DecreasesExposure()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure());
        handler.Handle(CreateOrder("PETR4", Side.BUY, 1_000, 10m));

        // Act
        handler.Handle(CreateOrder("PETR4", Side.SELL, 400, 10m));

        // Assert
        Assert.Equal(6_000m, handler.Exposure.GetExposure("PETR4"));
    }

    [Fact]
    public void Handle_OrdersOfDifferentSymbols_TrackExposureSeparately()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure());

        // Act
        handler.Handle(CreateOrder("PETR4", Side.BUY, 100, 10m));
        handler.Handle(CreateOrder("VALE3", Side.SELL, 100, 20m));

        // Assert
        Assert.Equal(1_000m, handler.Exposure.GetExposure("PETR4"));
        Assert.Equal(-2_000m, handler.Exposure.GetExposure("VALE3"));
        Assert.Equal(0m, handler.Exposure.GetExposure("VIIA4"));
    }

    [Fact]
    public void Handle_OrderReachingExactlyTheLimit_IsAccepted()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure(limitSymbol: 1_000m));
        var order = CreateOrder("PETR4", Side.BUY, 100, 10m);

        // Act
        var report = handler.Handle(order);

        // Assert
        Assert.Equal(ExecType.NEW, report.GetChar(Tags.ExecType));
        Assert.Equal(1_000m, handler.Exposure.GetExposure("PETR4"));
    }

    [Fact]
    public void Handle_OrderThatReducesExposure_IsAcceptedEvenAtTheLimit()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure(limitSymbol: 1_000m));
        handler.Handle(CreateOrder("PETR4", Side.BUY, 100, 10m));

        // Act
        var report = handler.Handle(CreateOrder("PETR4", Side.SELL, 100, 10m));

        // Assert
        Assert.Equal(ExecType.NEW, report.GetChar(Tags.ExecType));
        Assert.Equal(0m, handler.Exposure.GetExposure("PETR4"));
    }

    [Fact]
    public void Handle_DefaultLimit_Is100MillionPerSymbol()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure());
        var order = CreateOrder("VALE3", Side.BUY, 99_999, 999.99m);

        // Act
        var first = handler.Handle(order);
        var second = handler.Handle(order);

        // Assert
        Assert.Equal(ExecType.NEW, first.GetChar(Tags.ExecType));
        Assert.Equal(ExecType.REJECTED, second.GetChar(Tags.ExecType));
    }

    [Fact]
    public void Handle_BuyThatExceedsTheLimit_IsRejectedAndNotCounted()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure(limitSymbol: 1_000m));
        handler.Handle(CreateOrder("PETR4", Side.BUY, 90, 10m)); // 900

        // Act
        var report = handler.Handle(CreateOrder("PETR4", Side.BUY, 11, 10m)); // 900 + 110

        // Assert
        Assert.Equal(ExecType.REJECTED, report.GetChar(Tags.ExecType));
        Assert.Equal(OrdStatus.REJECTED, report.GetChar(Tags.OrdStatus));
        Assert.Equal(OrdRejReason.ORDER_EXCEEDS_LIMIT, report.GetInt(Tags.OrdRejReason));
        Assert.Equal(0m, report.GetDecimal(Tags.LeavesQty));
        Assert.Equal(900m, handler.Exposure.GetExposure("PETR4"));
    }


    [Fact]
    public void Handle_InvalidSide_IsRejectedAndNotCounted()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure());
        var order = CreateOrder("PETR4", '5', 10, 10m); 

        // Act
        var report = handler.Handle(order);

        // Assert
        Assert.Equal(ExecType.REJECTED, report.GetChar(Tags.ExecType));
        Assert.Equal(OrdRejReason.OTHER, report.GetInt(Tags.OrdRejReason));
        Assert.Empty(handler.Exposure.GetAllExposures());
    }

    [Fact]
    public void Handle_AnyOrder_ReturnsTheRequiredFix44Fields()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure());
        int[] requiredTags = [Tags.OrderID, Tags.ExecID, Tags.ExecType, Tags.OrdStatus, Tags.Side,
               Tags.LeavesQty, Tags.CumQty, Tags.AvgPx, Tags.TransactTime];

        // Act
        var report = handler.Handle(CreateOrder("PETR4", Side.BUY, 1, 1m));

        // Assert
        foreach (var tag in requiredTags)
            Assert.True(report.IsSetField(tag), $"Tag {tag} ausente");
    }

    [Fact]
    public void Handle_AcceptedAndRejectedOrders_CopyTheOrderData()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure(limitSymbol: 1_000m));

        // Act
        var accepted = handler.Handle(CreateOrder("PETR4", Side.SELL, 10, 12.34m, clOrdId: "A"));
        var rejected = handler.Handle(CreateOrder("PETR4", Side.BUY, 999, 999.99m, clOrdId: "B"));

        // Assert
        Assert.Equal(Side.SELL, accepted.GetChar(Tags.Side));
        Assert.Equal(10m, accepted.GetDecimal(Tags.OrderQty));
        Assert.Equal(12.34m, accepted.GetDecimal(Tags.Price));
        Assert.Equal("B", rejected.GetString(Tags.ClOrdID));
        Assert.Equal(999m, rejected.GetDecimal(Tags.OrderQty));
    }

    [Fact]
    public void Handle_AcceptedOrder_TextInformsTheCurrentExposure()
    {
        // Arrange
        var handler = new NewOrderSingleHandler(new FinancialExposure());

        // Act
        var report = handler.Handle(CreateOrder("PETR4", Side.BUY, 100, 10m));

        // Assert
        Assert.StartsWith("Order accepted.", report.GetString(Tags.Text));
    }
}