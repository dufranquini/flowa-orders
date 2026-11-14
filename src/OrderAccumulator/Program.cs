using Domain;
using OrderAccumulator;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

var settings = new SessionSettings(Path.Combine(AppContext.BaseDirectory, "fixacceptor.cfg"));
var handler = new NewOrderSingleHandler(new FinancialExposure());

var acceptor = new ThreadedSocketAcceptor(
  new AccumulatorApplication(handler),
  new FileStoreFactory(settings),
  settings,
  new FileLogFactory(settings),
  new QuickFix.FIX44.MessageFactory());

using var shutdown = new ManualResetEventSlim();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdown.Set();
};

acceptor.Start();
Console.WriteLine("OrderAccumulater waiting for FIX 4.4 orders. Press Ctrl+C to exit.");

shutdown.Wait();
acceptor.Stop();

foreach (var (symbol, exposure) in handler.Exposure.GetAllExposures())
    Console.WriteLine($"Final exposure for {symbol}: {exposure:N2}");