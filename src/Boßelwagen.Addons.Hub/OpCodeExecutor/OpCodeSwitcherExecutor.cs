using System.Threading.Channels;
using Boßelwagen.Addons.Hub.Switcher;
using Boßelwagen.Addons.Lib.Operation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Hub.OpCodeExecutor;

public sealed class OpCodeSwitcherExecutor : BackgroundService, IOpCodeExecutor {
    
    private readonly ISwitcher _switcher;
    private readonly ILogger<OpCodeSwitcherExecutor> _logger;
    private readonly Channel<OpCodeMessage> _opCodeQueue;
    private readonly TimeSpan Cooldown = TimeSpan.FromSeconds(value: 5);
    private readonly TimeSpan Expiration = TimeSpan.FromSeconds(value: 10);

    public OpCodeSwitcherExecutor(ISwitcher switcher, ILogger<OpCodeSwitcherExecutor> logger) {
        _switcher = switcher;
        _logger = logger;
        _opCodeQueue = Channel.CreateBounded<OpCodeMessage>(
            options: new BoundedChannelOptions(capacity: 10) {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
    }
    
    public void ExecuteOpCodeMessage(OpCodeMessage message) {
        _logger.LogInformation(message: "Received OpCodeMessage {Message}", args: message);
        if (!_opCodeQueue.Writer.TryWrite(item: message)) {
            _logger.LogWarning(message: "Failed to add OpCodeMessage: {Message} to Channel", args: message);
        }
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation(message: $"Started {nameof(OpCodeSwitcherExecutor)}");
        try {
            await WorkQueueAsync(cancellationToken: stoppingToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        } catch (Exception ex) {
            _logger.LogError(exception: ex, message: $"Exception in {nameof(OpCodeSwitcherExecutor)}");
        }
        _logger.LogInformation(message: $"Stopped {nameof(OpCodeSwitcherExecutor)}");
    }

    private async Task WorkQueueAsync(CancellationToken cancellationToken) {
        Dictionary<int, DateTimeOffset> lastTimeOfOpCode = [];
        await foreach (OpCodeMessage message in _opCodeQueue.Reader.ReadAllAsync(cancellationToken: cancellationToken)) {
            _logger.LogInformation(message: "Read Message {Message} from Channel", args: message);

            //Message expired
            if (message.Timestamp - DateTimeOffset.UtcNow > Expiration) {
                continue;
            }
            
            //Message ist zu neu
            if (lastTimeOfOpCode.TryGetValue(key: message.OpCode, value: out DateTimeOffset value) && message.Timestamp - value < Cooldown) {
                continue;
            }
            
            lastTimeOfOpCode[key: message.OpCode] = message.Timestamp;

            SwitchType? type = message.OpCode switch {
                1 => SwitchType.NextWindow,
                2 => SwitchType.PreviousWindow,
                3 => SwitchType.FirstWindow,
                4 => SwitchType.LastWindow,
                _ => null
            };


            _logger.LogDebug(message: "Got SwitchType {SwitchType} for Message {Message}", type, message);

            if (type is null) return;

            _switcher.SwitchWindow(switchType: type.Value);

        }
    }

}