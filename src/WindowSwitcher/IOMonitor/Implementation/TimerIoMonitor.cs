using Microsoft.Extensions.Logging;
using WindowSwitcher.IOMonitor.Internal;
using WindowSwitcher.Switcher;

namespace WindowSwitcher.IOMonitor.Implementation;

public sealed class TimerIoMonitor(
    ISwitcher switcher, 
    ILogger<TimerIoMonitor> logger) : BaseIoMonitor(switcher, logger) {

    private Task? _timerTask;
    private CancellationTokenSource? _cancellationTokenSource;
    
    public override void StartIoMonitoring() {
        _cancellationTokenSource ??= new CancellationTokenSource();
        _timerTask = Task.Run(DoWork, _cancellationTokenSource.Token);
    }

    public override void StopIoMonitoring() {
        _cancellationTokenSource?.Cancel();
    }

    private async Task DoWork() {
        _cancellationTokenSource ??= new CancellationTokenSource();
        using PeriodicTimer pt = new(TimeSpan.FromSeconds(30));
        while (await pt.WaitForNextTickAsync(_cancellationTokenSource.Token)) {
            OperationReceived(opcode: 1, timestamp: DateTimeOffset.UtcNow);
        }
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (!disposing) return;
        _timerTask?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}