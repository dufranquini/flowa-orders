using QuickFix;

namespace OrderGenerator.Api.Fix;

public interface IQuickFixSession
{
    bool Send(Message message, SessionID sessionID);
}
public class QuickFixSession : IQuickFixSession
{
    public bool Send(Message message, SessionID sessionID) => Session.SendToTarget(message, sessionID);
}
