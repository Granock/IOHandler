using System.Net.Sockets;
using Boßelwagen.Addons.Lib.Operation;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Lib.Communication.Receiver.Implementation;

public sealed class TcpOpCodeReceiver : IDisposable, IOpCodeReceiver {

    private readonly ILogger<TcpOpCodeReceiver> _logger;
    private readonly IOpCodeExecutor _opCodeExecutor;
    private readonly OpCodeReceiverConfig _config;
    private CancellationTokenSource _cancellationTokenSource = new();
    private Task? _workerTask;

    public TcpOpCodeReceiver(ILogger<TcpOpCodeReceiver> logger, IOpCodeExecutor opCodeExecutor, OpCodeReceiverConfig config) {
        _logger = logger;
        _opCodeExecutor = opCodeExecutor;
        _config = config;
    }

    public void StartReceiving() {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        _workerTask = Task.Run(
            function: () => TcpConnectionWorkerAsync(cancellationToken: _cancellationTokenSource.Token),
            cancellationToken: _cancellationTokenSource.Token);
    }

    public void StopReceiving() => _cancellationTokenSource.Cancel();

    private async Task TcpConnectionWorkerAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Started TcpConnectionWorkerAsync-Thread");
        try {
            await TcpConnectionWorkerCoreAsync(cancellationToken).ConfigureAwait(false);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error in TcpConnectionWorkerCoreAsync");
            throw;
        }
        _logger.LogInformation("Stopped TcpConnectionWorkerAsync-Thread");
    }
    
    private async Task TcpConnectionWorkerCoreAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            try {
                await TryConnectAndReceiveAsync(cancellationToken).ConfigureAwait(false);
            } catch (Exception ex) {
                _logger.LogError(ex, "Exception in TryConnectAndReceiveAsync");
            }
        }
    }

    private async Task TryConnectAndReceiveAsync(CancellationToken cancellationToken) {
        using Socket socket = new(SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(_config.Host, _config.Port, cancellationToken)
            .ConfigureAwait(false);
        
        await using NetworkStream stream = new(socket);
        using StreamReader reader = new(stream);
        await using StreamWriter writer = new(stream);

        //SEND AUTH-REQUEST
        _logger.LogWarning("sending auth-request to client");
        string authRequest = string.Concat(str0: Constants.API_KEY_AUTH_REQUEST, str1: _config.ApiKey);
        await writer.WriteLineAsync(value: authRequest)
            .ConfigureAwait(false);
        await writer.FlushAsync()
            .ConfigureAwait(false);
        
        if (await reader.ReadLineAsync(cancellationToken) is not Constants.API_KEY_AUTH_RESPONSE_SUCCESS) {
            _logger.LogWarning("failed auth-request to client");
        }


        _logger.LogWarning("succeeded auth-request to client");

        while (socket.Connected && !cancellationToken.IsCancellationRequested) {
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Received client message: {Message}", line);
            if (line is null) return;
            if (string.IsNullOrWhiteSpace(line)) continue;

            string opCodePart = line.Replace(Constants.OPCODE_MESSAGE_REQUEST, "");
            OpCodeMessage opCodeMessage = OpCodeMessage.FromString(opCodePart);

            string opCodeResponse = string.Format(Constants.OPCODE_MESSAGE_RESPONSE, opCodeMessage);
            await writer.WriteLineAsync(opCodeResponse).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);

            _opCodeExecutor.ExecuteOpCodeMessage(message: opCodeMessage);
        }
    }

    public void Dispose() {
        _cancellationTokenSource.Dispose();
        _workerTask?.Dispose();
    }
}