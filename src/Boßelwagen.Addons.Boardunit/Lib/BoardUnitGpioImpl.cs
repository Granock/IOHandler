using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Boardunit.Lib;

public class BoardunitGpio(ILogger<BoardunitGpio> logger) : FleetPcGpio(), IDisposable {

    private readonly ILogger _logger = logger;
    private FleetPcGpioWatcher? _fleetPcGpioWatcher;
    private bool _disposedValue;
    public event Action<(int port, bool rising)>? PortChanged;

    public new bool Initialize() {
        if (!Initialized && base.Initialize()) {
            _fleetPcGpioWatcher = new FleetPcGpioWatcher(this, _logger);
            _fleetPcGpioWatcher.DIPortVoltageChanged += OnDIPortVoltageChanged;
            _fleetPcGpioWatcher.StartPortWatcher();
        }
        return Initialized;
    }

    private void OnDIPortVoltageChanged(object? sender, DIPortEventArgs e) {
        Thread t = new(() => {
            try {
                PortChanged?.Invoke((e.Port, e.HasVoltage));
            } catch (Exception ex) {
                _logger.LogError(
                    exception: ex, 
                    message: "Error in OnDIPortVoltageChanged: {message}", 
                    args: ex.Message);
            }
        }) {
            IsBackground = true
        };
        t.Start();
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposedValue) return;
        if (disposing) {
            _fleetPcGpioWatcher?.Dispose();
        }
        _disposedValue = true;
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
