using System.Device.Gpio;
using Boßelwagen.Addons.GpioMonitor.Configuration;
using Boßelwagen.Addons.Lib.Communication.Sender;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.GpioMonitor.Gpio;

public class GpioMonitor : IHostedService {
    
    private readonly GpioController _gpioController;
    private readonly ILogger<GpioMonitor> _logger;
    private readonly OpCodeSender _opCodeSender;
    private readonly GpioConfiguration _configuration;

    private IDictionary<int, IoOperation>? _operations;
    
    public GpioMonitor(GpioController gpioController,
                       ILogger<GpioMonitor> logger,
                       OpCodeSender opCodeSender,
                       GpioConfiguration configuration) {
        _gpioController = gpioController;
        _logger = logger;
        _opCodeSender = opCodeSender;
        _configuration = configuration;
    }
    
    private IoOperation InitHandlerForOperation(IoOperation operation) {
        _logger.LogDebug(
            message: "Registering Gpio-Monitoring for {Pin} with Rising {Rising}, Falling {Falling} and OpCode {OpCode}",
            operation.Pin,
            operation.Rising,
            operation.Falling,
            operation.OpCode);

        _gpioController.OpenPin(pinNumber: operation.Pin);
        _gpioController.RegisterCallbackForPinValueChangedEvent(
            pinNumber: operation.Pin,
            eventTypes: PinEventTypes.Falling | PinEventTypes.Rising,
            callback: OnPinEvent);

        operation.PinChangeEventHandler = OnPinEvent;
        return operation;
    }

    private void CleanupHandlerForOperation(IoOperation registration) {
        _logger.LogDebug(
            message: "UnRegistering Gpio-Monitoring for {Pin} with Rising {Rising}, Falling {Falling} and OpCode {OpCode}",
            registration.Pin,
            registration.Rising,
            registration.Falling,
            registration.OpCode);

        if (registration.PinChangeEventHandler is null) return;
        _gpioController.UnregisterCallbackForPinValueChangedEvent(
            pinNumber: registration.Pin,
            callback: registration.PinChangeEventHandler);
    }
    
    private void OnPinEvent(object sender, PinValueChangedEventArgs args) {
        if (_operations is null) return;
        if (!_operations.TryGetValue(key: args.PinNumber, value: out IoOperation? item)) return;

        _logger.LogDebug(
            message: "PinEvent {Type} raised for {Pin} with Rising {Rising}, Falling {Falling} and OpCode {OpCode}",
            args.ChangeType.ToString(),
            item.Pin,
            item.Rising,
            item.Falling,
            item.OpCode);

        switch (args.ChangeType) {
            case PinEventTypes.None:
            case PinEventTypes.Rising when item is { Rising: false }:
            case PinEventTypes.Falling when item is { Falling: false }:
                return;
            default:
                SendOpCodeForOperation(operation: item);
                break;
        }
    }

    private void SendOpCodeForOperation(IoOperation operation) {
        _logger.LogDebug(
            message: "Sending OpCode for {Pin} with Rising {Rising}, Falling {Falling} and OpCode {OpCode}",
            operation.Pin,
            operation.Rising,
            operation.Falling,
            operation.OpCode);

        _opCodeSender.SendOpCode(opcode: operation.OpCode);

        _logger.LogDebug(
            message: "Sent OpCode for {Pin} with Rising {Rising}, Falling {Falling} and OpCode {OpCode}",
            operation.Pin,
            operation.Rising,
            operation.Falling,
            operation.OpCode);
    }


    public Task StartAsync(CancellationToken cancellationToken) {
        _operations = _configuration.Configurations
            .Select(selector: x => new IoOperation(configuration: x))
            .Select(selector: InitHandlerForOperation)
            .ToDictionary(keySelector: x => x.Pin);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        if (_operations is { Count: > 0}) {
            foreach (KeyValuePair<int, IoOperation> item in _operations) {
                CleanupHandlerForOperation(registration: item.Value);
            }
        }
        return Task.CompletedTask;
    }
}