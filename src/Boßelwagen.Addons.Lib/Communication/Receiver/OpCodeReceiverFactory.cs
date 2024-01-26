using Boßelwagen.Addons.Lib.Communication.Receiver.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Boßelwagen.Addons.Lib.Communication.Receiver;

public class OpCodeReceiverFactory {

    private readonly IServiceProvider _serviceProvider;

    public OpCodeReceiverFactory(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider;
    }

    public IOpCodeReceiver BuildOpCodeReceiver(int receiverTypeId) {
        Type receiverType = receiverTypeId switch {
            1 => typeof(TimerOpCodeReceiver),
            2 => typeof(TcpOpCodeReceiver),
            _ => throw new ArgumentException("Monitor-Type in config not available")
        };
        return (IOpCodeReceiver) ActivatorUtilities.CreateInstance(_serviceProvider, receiverType);
    }
}