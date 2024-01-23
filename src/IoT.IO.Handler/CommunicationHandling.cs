using IoT.IO.Handler.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace IoT.IO.Handler;
internal sealed class CommunicationHandling : IDisposable {

    private readonly ILogger<CommunicationHandling> _logger;
    private readonly GpioConfiguration _configuration;
    private readonly Channel<(int OpCode, DateTimeOffset Timestamp)> _messageQueue;
    private readonly CancellationTokenSource? _cancellationTokenSource;
    private readonly Task _connectionWorkerTask;

    public CommunicationHandling(ILogger<CommunicationHandling> logger, GpioConfiguration configuration) {
        _logger = logger;
        _messageQueue = Channel.CreateUnbounded<(int OpCode, DateTimeOffset Timestamp)>();
        _configuration = configuration;
        _cancellationTokenSource = new CancellationTokenSource();
        _connectionWorkerTask = Task.Run(
            function: () => ReceiveConnectionWorkerAsync(cancellationToken: _cancellationTokenSource.Token), 
            cancellationToken: _cancellationTokenSource.Token);
    }


    public void SendOpCode(int opcode) {
        (int OpCode, DateTimeOffset Timestamp) item = new(opcode, DateTimeOffset.UtcNow);
        _logger.LogInformation(
            message: "Preparing OpCodeSendMessage for queue for OpCode {opcode}, Timestamp {timestamp}", 
            item.OpCode, 
            item.Timestamp);
        if (!_messageQueue.Writer.TryWrite(item: new(opcode, DateTimeOffset.UtcNow))) {
            _logger.LogError(message: "Failed to Add OpCodeSendMessage to queue for OpCode {opcode}", args: opcode);
        }
    }

    private async Task ReceiveConnectionWorkerAsync(CancellationToken cancellationToken) {
        try {
            await ReceiveConnectionWorkerCoreAsync(cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        } catch (Exception ex) {
            _logger.LogError(exception: ex, message: "Error in ReceiveConnectionWorkerCore");
        }
    }

    private async Task ReceiveConnectionWorkerCoreAsync(CancellationToken cancellationToken) {
        using Socket serverSocket = new(
            socketType: SocketType.Stream, 
            protocolType: ProtocolType.Tcp);

        serverSocket.Bind(localEP: new IPEndPoint(address: IPAddress.Any, port: 9999));
        serverSocket.Listen(backlog: 1);

        while (!cancellationToken.IsCancellationRequested) {
            //Accept connection;
            _logger.LogInformation(message: "Waiting for client-connection");
            
            using Socket clientSocket = await serverSocket.AcceptAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(message: "accepted client-connection");

            //Handle Auth and Message sending
            await HandleClientConnectionAsync(clientSocket, cancellationToken);
            clientSocket.Close();
        }
    }

    private async Task HandleClientConnectionAsync(Socket clientSocket, CancellationToken cancellationToken) {
        try {
            await HandleClientConnectionCoreAsync(clientSocket, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        } catch (Exception ex) {
            _logger.LogError(exception: ex, message: "Error on Handling Client-Communication");
        }
    }

    private async Task HandleClientConnectionCoreAsync(Socket clientSocket, CancellationToken cancellationToken) {
        //open stream-reader and writer
        await using NetworkStream stream = new(socket: clientSocket);
        using StreamReader reader = new(stream);
        using StreamWriter writer = new(stream);
        int failedMessagesCounter = 0;

        //validate client against Api-Key
        string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

        if (string.IsNullOrWhiteSpace(value: line) || !line.StartsWith(value: CommunicationConstants.API_KEY_AUTH_REQUEST)) {
            await writer.WriteLineAsync(value: CommunicationConstants.API_KEY_AUTH_RESPONSE_FAILED);
            await writer.FlushAsync();
            _logger.LogWarning(message: "Failed API-KEY-AUTH with Request ({Request})", args: line);
            return;
        }

        string apiKey = line.Replace(oldValue: CommunicationConstants.API_KEY_AUTH_REQUEST, newValue: string.Empty).Trim();
        
        if (!string.Equals(a: apiKey, b: _configuration.ApiKey, comparisonType: StringComparison.InvariantCulture)) {
            await writer.WriteLineAsync(value: CommunicationConstants.API_KEY_AUTH_RESPONSE_FAILED);
            await writer.FlushAsync();
            _logger.LogWarning(message: "Failed API-KEY-AUTH with Request ({Request})", args: line);
            return;
        }

        await writer.WriteLineAsync(value: CommunicationConstants.API_KEY_AUTH_RESPONSE_SUCCESS);
        await writer.FlushAsync();
        _logger.LogInformation(message: "Success for API-KEY-AUTH with Request ({Request})", args: line);

        await foreach (var item in _messageQueue.Reader.ReadAllAsync(cancellationToken: cancellationToken)) {
            _logger.LogInformation(message: "Sending Opcode {Opcode} ({Timestamp})", item.OpCode, item.Timestamp);
            await writer.WriteLineAsync(value: 
                string.Format(
                    format: CommunicationConstants.OPCODE_MESSAGE_REQUEST, 
                    arg0: item.OpCode, 
                    arg1: item.Timestamp.ToUnixTimeMilliseconds()));
            await writer.FlushAsync();

            line = await reader.ReadLineAsync(cancellationToken: cancellationToken);

            //We expect a response, that contains the same informations
            if (!string.Equals(
                a: line,
                b: string.Format(
                    format: CommunicationConstants.OPCODE_MESSAGE_RESPONSE,
                    arg0: item.OpCode,
                    arg1: item.Timestamp.ToUnixTimeMilliseconds()))) {
                //Response was different!
                failedMessagesCounter++;
                _logger.LogWarning(message: "Failed with Sending Opcode {Opcode} ({Timestamp})", item.OpCode, item.Timestamp);
            } else {
                //Response matched
                failedMessagesCounter--;
                _logger.LogWarning(message: "Succeded with Sending Opcode {Opcode} ({Timestamp})", item.OpCode, item.Timestamp);
            }             //More than 5 failures == instable connection
            if (failedMessagesCounter >= 5) {
                _logger.LogWarning(message: "FailedMessagesCounter reached over five, connection terminated");
                return;
            }
        }
    }

    public void Dispose() {
        _cancellationTokenSource?.Dispose();
        _connectionWorkerTask?.Dispose();
    }

}
