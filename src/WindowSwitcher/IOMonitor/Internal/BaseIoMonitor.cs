using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using WindowSwitcher.Switcher;

namespace WindowSwitcher.IOMonitor.Internal;

public abstract class BaseIoMonitor : IIoMonitor, IDisposable {
    
    private readonly ISwitcher _switcher;
    private readonly ILogger _logger;
    private readonly Channel<(int, DateTimeOffset)> _opCodeQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _queueWorkerTask;

    protected BaseIoMonitor(ISwitcher switcher, ILogger logger) {
        _switcher = switcher;
        _logger = logger;
        _opCodeQueue = Channel.CreateBounded<(int, DateTimeOffset)>(new BoundedChannelOptions(capacity: 10) {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
        _cancellationTokenSource = new CancellationTokenSource();
        _queueWorkerTask = Task.Run(
            function: () => WorkQueueAsync(_cancellationTokenSource.Token), 
            cancellationToken: _cancellationTokenSource.Token);
    }
    
    public abstract void StartIoMonitoring();
    public abstract void StopIoMonitoring();
    
    protected void OperationReceived(int opcode, DateTimeOffset timestamp) {
        _logger.LogInformation(message: "Received OpCode: {OpCode}, Timestamp: {Timestamp}", opcode, timestamp);
        if (!_opCodeQueue.Writer.TryWrite((opcode, timestamp))) {
            _logger.LogWarning("Failed to add OpCode: {OpCode} to Channel", opcode);
        }
    }

    private async Task? WorkQueueAsync(CancellationToken cancellationToken) {
        TimeSpan cooldown = TimeSpan.FromSeconds(5);
        Dictionary<int, DateTimeOffset> lastTimeOfOpCode = new();
        
        _logger.LogInformation(message: "Started QueueWorkerThread");
        await foreach ((int opCode, DateTimeOffset receivedAt) in _opCodeQueue.Reader.ReadAllAsync(cancellationToken: cancellationToken)) {
            _logger.LogInformation(message: "Read OpCode: {Opcode} Time: {Timestamp} from Channel", opCode, receivedAt);
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
        return; //TODO remove
        _switcher.SwitchWindow(type);
    }

    protected virtual void Dispose(bool disposing) {
        if (!disposing) return;
        _cancellationTokenSource.Dispose();
        _queueWorkerTask.Dispose();
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}