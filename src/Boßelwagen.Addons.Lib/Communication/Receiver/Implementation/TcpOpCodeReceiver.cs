using System.Net.Sockets;
using Boßelwagen.Addons.Lib.Operation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Lib.Communication.Receiver.Implementation;

public sealed class TcpOpCodeReceiver : BackgroundService, IOpCodeReceiver {

    private readonly ILogger<TcpOpCodeReceiver> _logger;
    private readonly IOpCodeExecutor _opCodeExecutor;
    private readonly OpCodeReceiverConfig _config;

    public TcpOpCodeReceiver(ILogger<TcpOpCodeReceiver> logger, 
                             IOpCodeExecutor opCodeExecutor, 
                             OpCodeReceiverConfig config) {
        _logger = logger;
        _opCodeExecutor = opCodeExecutor;
        _config = config;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation(message: $"Started {nameof(TcpOpCodeReceiver)}");
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await TryConnectAndReceiveAsync(cancellationToken: stoppingToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            } catch (Exception ex) {
                _logger.LogError(exception: ex, message: $"Exception in {nameof(TcpOpCodeReceiver)}:{nameof(ExecuteAsync)}");
            }
        }

        _logger.LogInformation(message: $"Stopped {nameof(TcpOpCodeReceiver)}");
    }

    private async Task TryConnectAndReceiveAsync(CancellationToken cancellationToken) {
        //Open Connection
        using Socket socket = new(socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
        await socket.ConnectAsync(host: _config.Host, port: _config.Port, cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        
        //Setup Reader & Writer
        await using NetworkStream stream = new(socket);
        using StreamReader reader = new(stream);
        await using StreamWriter writer = new(stream);

        //Authenticate Connection
        bool authenticated = await AuthenticateConnectionAsync(
            reader: reader, 
            writer: writer, 
            cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        if (!authenticated) return;

        //Read OpCodes while Connection is Ok
        while (socket.Connected && !cancellationToken.IsCancellationRequested) {
            OpCodeMessage? message = await ReceiveOpCodeMessageAsync(
                reader: reader, 
                writer: writer, 
                cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (message is null) return;

            _opCodeExecutor.ExecuteOpCodeMessage(message: message);
        }
    }

    private async Task<bool> AuthenticateConnectionAsync(StreamReader reader, 
                                                         StreamWriter writer, 
                                                         CancellationToken cancellationToken) {
        _logger.LogWarning(message: "Begin Authentication for Connection");
        
        string authRequest = string.Concat(str0: Constants.API_KEY_AUTH_REQUEST, str1: _config.ApiKey);
        
        await writer.WriteLineAsync(value: authRequest)
            .ConfigureAwait(continueOnCapturedContext: false);
        await writer.FlushAsync()
            .ConfigureAwait(continueOnCapturedContext: false);

        string? authResponse = await reader.ReadLineAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        if (authResponse is not Constants.API_KEY_AUTH_RESPONSE_SUCCESS) {
            _logger.LogWarning(message: "Failed Authentication for Connection");
            return false;
        }

        _logger.LogWarning(message: "Finished Authentication for Connection");
        return true;

    }

    private async Task<OpCodeMessage?> ReceiveOpCodeMessageAsync(StreamReader reader,
                                                                 StreamWriter writer,
                                                                 CancellationToken cancellationToken) {
        string? line = await reader.ReadLineAsync(cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

        _logger.LogDebug(message: "Received client message: {Message}", args: line);
        
        if (line is null || string.IsNullOrWhiteSpace(value: line)) return null;

        string opCodePart = line.Replace(oldValue: Constants.OPCODE_MESSAGE_REQUEST, newValue: "");
        OpCodeMessage opCodeMessage = OpCodeMessage.FromString(opCodePart);

        string opCodeResponse = string.Format(format: Constants.OPCODE_MESSAGE_RESPONSE, arg0: opCodeMessage);

        await writer.WriteLineAsync(value: opCodeResponse)
            .ConfigureAwait(continueOnCapturedContext: false);
        await writer.FlushAsync()
            .ConfigureAwait(continueOnCapturedContext: false);

        return opCodeMessage;
    }

}