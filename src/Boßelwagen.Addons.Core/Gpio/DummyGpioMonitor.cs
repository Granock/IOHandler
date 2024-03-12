using Boßelwagen.Addons.Configuration.Configuration;
using Boßelwagen.Addons.Configuration.Model;
using Boßelwagen.Addons.Core.Switcher;
using Boßelwagen.Addons.Lib.Gpio.Monitor;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Core.Gpio;
public class DummyGpioMonitor(ILogger<GpioMonitorBase> logger, 
                              IConfigurationService configurationService, 
                              ISwitcher switcher) 
    : GpioMonitorBase(logger, configurationService, switcher) {

    private CancellationTokenSource? _cts;    
    private Task? _timerTask;

    protected override void CleanupHandlerForOperationCore(GpioConfiguration operation) { }
    protected override void InitHandlerForOperationCore(GpioConfiguration operation) { }

    public async override Task StartAsync(CancellationToken cancellationToken) {
        await base.StartAsync(cancellationToken);
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _timerTask = Task.Run(() => TimerTask(_cts.Token), cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        _cts?.Cancel();
        _timerTask?.Dispose();
        return base.StopAsync(cancellationToken);
    }

    private async Task TimerTask(CancellationToken cancellationToken) {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(3));
        while(await timer.WaitForNextTickAsync(cancellationToken)) {
            OnPinEvent(1, true);
        }
    }
}
