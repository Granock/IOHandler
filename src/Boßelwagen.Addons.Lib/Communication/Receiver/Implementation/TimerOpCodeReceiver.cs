using Boßelwagen.Addons.Lib.Operation;

namespace Boßelwagen.Addons.Lib.Communication.Receiver.Implementation;

public sealed class TimerOpCodeReceiver : IDisposable, IOpCodeReceiver {
    
    private readonly IOpCodeExecutor _opCodeExecutor;
    private CancellationTokenSource _cancellationTokenSource = new();
    private Task? _timerTask;

    public TimerOpCodeReceiver(IOpCodeExecutor opCodeExecutor) {
        _opCodeExecutor = opCodeExecutor;
    }
    
    
    public void StartReceiving() {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        _timerTask = Task.Run(DoWork, _cancellationTokenSource.Token);
    }

    public void StopReceiving() => _cancellationTokenSource.Cancel();
    
    private async Task DoWork() {
        using PeriodicTimer pt = new(TimeSpan.FromSeconds(3));
        while (await pt.WaitForNextTickAsync(_cancellationTokenSource.Token)) {
            _opCodeExecutor.ExecuteOpCodeMessage(new OpCodeMessage(1, DateTimeOffset.UtcNow));
        }
    }

    public void Dispose() {
        _cancellationTokenSource.Dispose();
        _timerTask?.Dispose();
    }
}