using IoT.IO.Handler.Configuration;
using Microsoft.Extensions.Logging;
using System.Device.Gpio;

namespace IoT.IO.Handler;
internal class GpioHandling : IDisposable {

	private readonly GpioController _gpioController;
	private readonly ILogger<GpioHandling> _logger;
	private readonly CommunicationHandling _communicationHandling;
	private IDictionary<int, IoOperation>? _operations;
	private bool disposedValue;

	public GpioHandling(GpioController gpioController,
						ILogger<GpioHandling> logger,
						CommunicationHandling communicationHandling,
						GpioConfiguration configuration) {
		_gpioController = gpioController;
		_logger = logger;
		_communicationHandling = communicationHandling;
		_operations = configuration.Configurations
								   .Select(selector: x => new IoOperation(configuration: x))
								   .Select(selector: InitHandlerForOperation)
								   .ToDictionary(keySelector: x => x.Pin);
	}

	private IoOperation InitHandlerForOperation(IoOperation operation) {
		_logger.LogInformation(
			message: "Registering Gpio-Monitoring for {pin} with Rising {rising}, Falling {falling} and OpCode {opcode}",
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
		_logger.LogInformation(
			message: "UnRegistering Gpio-Monitoring for {pin} with Rising {rising}, Falling {falling} and OpCode {opcode}",
			  registration.Pin,
			  registration.Rising,
			  registration.Falling,
			  registration.OpCode);

		if (registration?.PinChangeEventHandler is null) return;
		_gpioController.UnregisterCallbackForPinValueChangedEvent(
				pinNumber: registration.Pin,
				callback: registration.PinChangeEventHandler);
	}


	private void OnPinEvent(object sender, PinValueChangedEventArgs args) {
		if (_operations is null) return;
		if (!_operations.TryGetValue(key: args.PinNumber, value: out IoOperation? item)) return;
		if (item is null) return;

		_logger.LogInformation(
			message: "PinEvent {type} raised for {pin} with Rising {rising}, Falling {falling} and OpCode {opcode}",
			args.ChangeType.ToString(),
			item.Pin,
			item.Rising,
			item.Falling,
			item.OpCode);

		if (args.ChangeType is PinEventTypes.Rising && item is { Rising: false }) return;
		if (args.ChangeType is PinEventTypes.Falling && item is { Falling: false }) return;

		SendOpCodeForOperation(operation: item);
	}

	private void SendOpCodeForOperation(IoOperation operation) {
		_logger.LogInformation(
			message: "Sending OpCode for {pin} with Rising {rising}, Falling {falling} and OpCode {opcode}",
			operation.Pin,
			operation.Rising,
			operation.Falling,
			operation.OpCode);

		_communicationHandling.SendOpCode(opcode: operation.OpCode);

		_logger.LogInformation(
			message: "Sent OpCode for {pin} with Rising {rising}, Falling {falling} and OpCode {opcode}",
			operation.Pin,
			operation.Rising,
			operation.Falling,
			operation.OpCode);
	}

	protected virtual void Dispose(bool disposing) {
		if (!disposedValue) {
			if (_operations is not null) {
				foreach (var item in _operations) {
					CleanupHandlerForOperation(item.Value);
				}
				_operations = null;
			}
			disposedValue = true;
		}
	}

	public void Dispose() {
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private class IoOperation {
		public IoOperation(IoConfiguration configuration) {
			Pin = configuration.Pin;
			OpCode = configuration.OpCode;
			Rising = configuration.SendOnRising;
			Falling = configuration.SendOnFalling;
		}

		public int Pin { get; }
		public int OpCode { get; }
		public bool Rising { get; }
		public bool Falling { get; }
		public PinChangeEventHandler? PinChangeEventHandler { get; set; }
	}

}