using System.Device.Gpio;
using Boßelwagen.Addons.GpioMonitor.Configuration;

namespace Boßelwagen.Addons.GpioMonitor.Gpio;

internal sealed class IoOperation {
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