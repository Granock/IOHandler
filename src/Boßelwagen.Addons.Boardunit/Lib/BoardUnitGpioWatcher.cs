namespace Boßelwagen.Addons.Boardunit.Lib;

using Microsoft.Extensions.Logging;
using System;
using System.Threading;

public partial class FleetPcGpioWatcher : IDisposable {

    private readonly FleetPcGpio _fleetPcGpio;
    private readonly ILogger _logger;

    private CancellationTokenSource? _cts;
    private Task? _watcherTask;

    private bool _diPort1Voltage;
    private bool _diPort2Voltage;
    private bool _diPort3Voltage;
    private bool _diPort4Voltage;
    private bool _disposedValue;

    public event EventHandler<DIPortEventArgs>? DIPortVoltageChanged;

    public FleetPcGpioWatcher(FleetPcGpio fleetPcGpio, ILogger logger) {
        _fleetPcGpio = fleetPcGpio;
        _logger = logger;
    }


    /// <summary>
    /// Starts a timer to check the DI-Ports Status
    /// </summary>
    public void StartPortWatcher() {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _watcherTask = Task.Run(() => TimerTask(_cts.Token));
    }

    private async Task TimerTask(CancellationToken cancellationToken) {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(3000));
        while (await timer.WaitForNextTickAsync(cancellationToken)) {
            try {
                UpdatePortStatus();
            } catch (Exception ex) {
                _logger.LogError(ex, "Error in TimerTask: {message}", ex.Message);
            }
        }
    }

    private void UpdatePortStatus() {
        if (_fleetPcGpio.HasVoltage(1) != _diPort1Voltage) {
            _diPort1Voltage = !_diPort1Voltage;
            RaiseVoltageChangedAsync(1, _diPort1Voltage);
        }
        if (_fleetPcGpio.HasVoltage(2) != _diPort2Voltage) {
            _diPort2Voltage = !_diPort2Voltage;
            RaiseVoltageChangedAsync(2, _diPort2Voltage);
        }
        if (!_fleetPcGpio.IsFleetPC4D) {
            if (_fleetPcGpio.HasVoltage(3) != _diPort3Voltage) {
                _diPort3Voltage = !_diPort3Voltage;
                RaiseVoltageChangedAsync(3, _diPort3Voltage);
            }
            if (_fleetPcGpio.HasVoltage(4) != _diPort4Voltage) {
                _diPort4Voltage = !_diPort4Voltage;
                RaiseVoltageChangedAsync(4, _diPort4Voltage);
            }
        }
    }

    private void RaiseVoltageChangedAsync(int port, bool hasVoltage) {
        Thread t = new(() => {
            try {
                DIPortVoltageChanged?.Invoke(this, new DIPortEventArgs(port, hasVoltage));
            } catch (Exception ex) {
                _logger.LogError(ex, "Error in RaiseVoltageChangedAsync: {message}", ex.Message);
            }
        }) {
            IsBackground = true
        };
        t.Start();
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposedValue) {
            if (disposing) {
                _cts?.Cancel();
                _watcherTask?.Dispose();
            }
            _disposedValue = true;
        }
    }

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}