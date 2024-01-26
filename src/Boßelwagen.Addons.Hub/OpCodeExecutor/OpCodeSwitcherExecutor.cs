using System.Threading.Channels;
using Boßelwagen.Addons.Hub.Switcher;
using Boßelwagen.Addons.Lib.Operation;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Hub.OpCodeExecutor;

public sealed class OpCodeSwitcherExecutor : IOpCodeExecutor, IDisposable {
    
    private readonly ISwitcher _switcher;
    private readonly ILogger<OpCodeSwitcherExecutor> _logger;
    private readonly Channel<OpCodeMessage> _opCodeQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _queueWorkerTask;
    
    public OpCodeSwitcherExecutor(ISwitcher switcher, ILogger<OpCodeSwitcherExecutor> logger) {
        _switcher = switcher;
        _logger = logger;
        _opCodeQueue = Channel.CreateBounded<OpCodeMessage>(new BoundedChannelOptions(capacity: 10) {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
        _cancellationTokenSource = new CancellationTokenSource();
        _queueWorkerTask = Task.Run(
            function: () => WorkQueueAsync(_cancellationTokenSource.Token), 
            cancellationToken: _cancellationTokenSource.Token);
    }
    
    public void ExecuteOpCodeMessage(OpCodeMessage message) {
        _logger.LogInformation(message: "Received OpCodeMessage {Message}", message);
        if (!_opCodeQueue.Writer.TryWrite(message)) {
            _logger.LogWarning("Failed to add OpCodeMessage: {Message} to Channel", message);
        }
    }
    
    private async Task? WorkQueueAsync(CancellationToken cancellationToken) {
        TimeSpan cooldown = TimeSpan.FromSeconds(5);
        TimeSpan ageGate = TimeSpan.FromSeconds(10);
        Dictionary<int, DateTimeOffset> lastTimeOfOpCode = new();
        
        _logger.LogInformation(message: "Started QueueWorkerThread");
        await foreach ((int opCode, DateTimeOffset receivedAt) in _opCodeQueue.Reader.ReadAllAsync(cancellationToken: cancellationToken)) {
            _logger.LogInformation(message: "Read OpCode: {Opcode} Time: {Timestamp} from Channel", opCode, receivedAt);

            //Message ist zu alt
            if (receivedAt - DateTimeOffset.UtcNow > ageGate) {
                continue;
            }
            
            //Message ist zu neu
            if (lastTimeOfOpCode.TryGetValue(opCode, out DateTimeOffset value) && receivedAt - value < cooldown) {
                continue;
            }
            
            lastTimeOfOpCode[opCode] = receivedAt;
            _logger.LogInformation(message: "Handling OpCode: {Opcode} at Switcher", opCode);
            HandleOpcodeAtSwitcher(opCode);
        }
        _logger.LogInformation(message: "Stopped QueueWorkerThread");
    }

    private void HandleOpcodeAtSwitcher(int opCode) {
        try {
            HandleOpcodeAtSwitcherCore(opCode);
        } catch (Exception ex) {
            _logger.LogError(
                exception: ex, 
                message: "Error while Handling OpCode: {Opcode} at Switcher", 
                args: opCode);
        }
    }

    private void HandleOpcodeAtSwitcherCore(int opCode) {
        SwitchType type = opCode switch {
            1 => SwitchType.NextWindow,
            2 => SwitchType.PreviousWindow,
            3 => SwitchType.FirstWindow,
            4 => SwitchType.LastWindow,
            _ => throw new ArgumentOutOfRangeException(nameof(opCode))
        };
        _switcher.SwitchWindow(type);
    }

    public void Dispose() {
        _cancellationTokenSource.Dispose();
        _queueWorkerTask.Dispose();
    }
}