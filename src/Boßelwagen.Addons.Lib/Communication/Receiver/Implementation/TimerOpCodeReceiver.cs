using Boßelwagen.Addons.Lib.Operation;
using Microsoft.Extensions.Hosting;

namespace Boßelwagen.Addons.Lib.Communication.Receiver.Implementation;

public sealed class TimerOpCodeReceiver : BackgroundService, IOpCodeReceiver {
    
    private readonly IOpCodeExecutor _opCodeExecutor;

    public TimerOpCodeReceiver(IOpCodeExecutor opCodeExecutor) => _opCodeExecutor = opCodeExecutor;


    protected async override Task ExecuteAsync(CancellationToken stoppingToken) {
        using PeriodicTimer pt = new(period: TimeSpan.FromSeconds(value: 3));
        while (await pt.WaitForNextTickAsync(cancellationToken: stoppingToken)
                       .ConfigureAwait(continueOnCapturedContext: false)) {
            _opCodeExecutor.ExecuteOpCodeMessage(message: new OpCodeMessage(OpCode: 1, Timestamp: DateTimeOffset.UtcNow));
        }
    }
}