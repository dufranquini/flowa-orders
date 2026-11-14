using QuickFix;
using QuickFix.Fields;
using NewOrderSingle = QuickFix.FIX44.NewOrderSingle;

namespace OrderAccumulator;

public class AccumulatorApplication(NewOrderSingleHandler handler) : IApplication
{
    public void FromApp(Message message, SessionID sessionID)
    {
        if (message is not NewOrderSingle order)
            return;

        var report = handler.Handle(order);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {report.GetString(Tags.Text)}");

        Session.SendToTarget(report, sessionID);
    }

    public void OnLogon(SessionID sessionID) => Console.WriteLine($"Logon: {sessionID}");
    public void OnLogout(SessionID sessionID) => Console.WriteLine($"Logout: {sessionID}");

    public void OnCreate(SessionID sessionID) { }
    public void ToAdmin(Message message, SessionID sessionID) { }
    public void FromAdmin(Message message, SessionID sessionID) { }
    public void ToApp(Message message, SessionID sessionID) { }
}
