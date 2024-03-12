using Boßelwagen.Addons.Configuration.Configuration;
using Boßelwagen.Addons.Configuration.Model;
using Boßelwagen.Addons.Core.Switcher;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Lib.Gpio.Monitor;

public abstract class GpioMonitorBase : IHostedService {
    
    private readonly ILogger<GpioMonitorBase> _logger;
    private readonly IConfigurationService _configurationService;
    private readonly ISwitcher _switcher;

    private Dictionary<int, GpioConfiguration>? _operations;
    
    protected GpioMonitorBase(ILogger<GpioMonitorBase> logger,
                              IConfigurationService configurationService,
                              ISwitcher switcher) {
        _logger = logger;
        _configurationService = configurationService;
        _switcher = switcher;
    }
    
    private GpioConfiguration InitHandlerForOperation(GpioConfiguration operation) {
        _logger.LogDebug(
            message: "Registering Gpio-Monitoring for {Pin} with Rising {Rising}, Falling {Falling} and SwitchType {SwitchType}",
            operation.Pin,
            operation.SendOnRising,
            operation.SendOnFalling,
            operation.SwitchType);

        InitHandlerForOperationCore(operation);

        return operation;
    }

    protected abstract void InitHandlerForOperationCore(GpioConfiguration operation);
    
    private void CleanupHandlerForOperation(GpioConfiguration operation) {
        _logger.LogDebug(
            message: "UnRegistering Gpio-Monitoring for {Pin} with Rising {Rising}, Falling {Falling} and SwitchType {SwitchType}",
            operation.Pin,
            operation.SendOnRising,
            operation.SendOnFalling,
            operation.SwitchType);

        CleanupHandlerForOperationCore(operation);
    }
    
    protected abstract void CleanupHandlerForOperationCore(GpioConfiguration operation);
    
    protected void OnPinEvent(int pinNumber, bool rising) {
        if (_operations is null) return;
        if (!_operations.TryGetValue(key: pinNumber, value: out GpioConfiguration? item)) return;

        _logger.LogDebug(
            message: "PinEvent rising: {rising} raised for {Pin} with Rising {Rising}, Falling {Falling} and SwitchType {SwitchType}",
            rising,
            item.Pin,
            item.SendOnRising,
            item.SendOnFalling,
            item.SwitchType);

        switch (rising) {
            case true when item is { SendOnRising: false }:
            case false when item is { SendOnFalling: false }:
                return;
            default:
                _switcher.SwitchWindow(item.SwitchType);
                break;
        }
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken) {
        HubConfiguration configuration = await _configurationService.GetConfigurationAsync(cancellationToken)
                                                                    .ConfigureAwait(false);
        _operations = configuration.Gpios
            .Select(selector: InitHandlerForOperation)
            .ToDictionary(keySelector: x => x.Pin);
    }

    public virtual Task StopAsync(CancellationToken cancellationToken) {
        if (_operations is not { Count: > 0 }) return Task.CompletedTask;
        foreach (KeyValuePair<int, GpioConfiguration> item in _operations) {
            CleanupHandlerForOperation(operation: item.Value);
        }
        return Task.CompletedTask;
    }

}