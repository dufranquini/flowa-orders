using Domain;
using QuickFix.Fields;
using QuickFix.FIX44;
using System.Globalization;

namespace OrderAccumulator;

public sealed class NewOrderSingleHandler(FinancialExposure exposure)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private long _sequence;

    public FinancialExposure Exposure => exposure;

    public ExecutionReport Handle(NewOrderSingle order)
    {
        var symbol = order.GetString(Tags.Symbol);
        var quantity = order.GetDecimal(Tags.OrderQty);
        var price = order.GetDecimal(Tags.Price);

        OrderSide? side = order.GetChar(Tags.Side) switch
        {
            Side.BUY => OrderSide.Buy,
            Side.SELL => OrderSide.Sell,
            _ => null
        };

        if (side is null)
            return Rejected(order, OrdRejReason.OTHER, "");

        //TODO: Validate order
        Exposure result = exposure.Apply(symbol, side.Value, price, (int)quantity);

        if (!result.Accepted)
            return Rejected(order, OrdRejReason.ORDER_EXCEEDS_LIMIT,
                $"Order exceeds limit. Current exposure: {result.ExposureBefore.ToString("C", PtBr)}");

        var report = CreateReport(order, ExecType.NEW, OrdStatus.NEW);
        report.SetField(new LeavesQty(quantity));
        report.SetField(new Text($"Order accepted. Current exposure: {result.ExposureAfter.ToString("C2", PtBr)}"));
        return report;
    }

    private ExecutionReport Rejected(NewOrderSingle order, int reason, string text)
    {
        var report = CreateReport(order, ExecType.REJECTED, OrdStatus.REJECTED);
        report.SetField(new LeavesQty(0));
        report.SetField(new OrdRejReason(reason));
        report.SetField(new Text(text));
        return report;
    }

    private ExecutionReport CreateReport(NewOrderSingle order, char execType, char ordStatus)
    {
        var id = Interlocked.Increment(ref _sequence);

        var report = new ExecutionReport();
        report.SetField(new OrderID($"ORD-{id:D8}"));
        report.SetField(new ExecID($"EXE-{id:D8}"));
        report.SetField(new ExecType(execType));
        report.SetField(new OrdStatus(ordStatus));
        report.SetField(new ClOrdID(order.GetString(Tags.ClOrdID)));
        report.SetField(new Symbol(order.GetString(Tags.Symbol)));
        report.SetField(new Side(order.GetChar(Tags.Side)));
        report.SetField(new OrderQty(order.GetDecimal(Tags.OrderQty)));
        report.SetField(new Price(order.GetDecimal(Tags.Price)));
        report.SetField(new CumQty(0));
        report.SetField(new AvgPx(0));
        report.SetField(new TransactTime(DateTime.UtcNow));
        return report;
    }
}