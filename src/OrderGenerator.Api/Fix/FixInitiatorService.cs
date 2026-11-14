using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace OrderGenerator.Api.Fix;

public class FixInitiatorService(FixOrderGateway orderGateway) : IHostedService
{
    private SocketInitiator? _initiator;
    public Task StartAsync(CancellationToken cancellationToken)
    {
        SessionSettings settings = new(Path.Combine(AppContext.BaseDirectory, "fixinitiator.cfg"));

        _initiator = new SocketInitiator(
            orderGateway,
            new FileStoreFactory(settings),
            settings,
            new FileLogFactory(settings),
            new QuickFix.FIX44.MessageFactory()
        );

        _initiator.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
