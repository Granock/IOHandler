using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using Boßelwagen.Addons.Lib.Operation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Lib.Communication.Sender;

public sealed class OpCodeSender : BackgroundService {

    private readonly string _apiKey;
    private readonly ILogger<OpCodeSender> _logger;
    private readonly Channel<OpCodeMessage> _messageQueue;
    
    public OpCodeSender(ILogger<OpCodeSender> logger, string apiKey) {
        _logger = logger;
        _apiKey = apiKey;
        _messageQueue = Channel.CreateUnbounded<OpCodeMessage>();
    }
    
    public void SendOpCode(int opcode) {
        OpCodeMessage message = OpCodeMessage.FromOpCode(opcode);
        _logger.LogInformation(message: "Adding message {Message} to Queue of OpCodeSender", args: message);
        if (!_messageQueue.Writer.TryWrite(item: message)) {
            _logger.LogError(message: "Failed to Add message {Message}", args: message);
        }
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation(message: $"Started in {nameof(OpCodeSender)}");
        try {
            await ReceiveConnectionWorkerCoreAsync(stoppingToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        } catch (Exception ex) {
            _logger.LogError(exception: ex, message: $"Exception in {nameof(OpCodeSender)}");
        }
        _logger.LogInformation(message: $"Stopped in {nameof(OpCodeSender)}");
    }

    private async Task ReceiveConnectionWorkerCoreAsync(CancellationToken cancellationToken) {
        //Setup Socket
        using Socket serverSocket = new(socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
        serverSocket.Bind(localEP: new IPEndPoint(address: IPAddress.Any, port: 9999));
        serverSocket.Listen(backlog: 1);

        while (!cancellationToken.IsCancellationRequested) {
            //Accept connection
            _logger.LogDebug(message: "Waiting for client-connection");
            
            using Socket clientSocket = await serverSocket.AcceptAsync(cancellationToken)
                                                          .ConfigureAwait(continueOnCapturedContext: false);
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(message: "accepted client-connection");

            //Handle Auth and Message sending
            try {
                await HandleClientConnectionCoreAsync(clientSocket, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            } catch (Exception ex) {
                _logger.LogError(exception: ex, message: "Error on Handling Client-Communication");
            }
            clientSocket.Close();
        }
    }

    private async Task HandleClientConnectionCoreAsync(Socket clientSocket, CancellationToken cancellationToken) {
        //open stream-reader and writer
        await using NetworkStream stream = new(socket: clientSocket);
        using StreamReader reader = new(stream);
        await using StreamWriter writer = new(stream);
        int failedMessagesCounter = 0;

        //validate client against Api-Key
        string? line = await reader.ReadLineAsync(cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        if (string.IsNullOrWhiteSpace(value: line) 
            || !line.StartsWith(value: Constants.API_KEY_AUTH_REQUEST)) {
            await writer.WriteLineAsync(value: Constants.API_KEY_AUTH_RESPONSE_FAILED)
                .ConfigureAwait(continueOnCapturedContext: false);
            await writer.FlushAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
            _logger.LogWarning(message: "Failed API-KEY-AUTH with Request ({Request})", args: line);
            return;
        }

        string apiKey = line.Replace(oldValue: Constants.API_KEY_AUTH_REQUEST, newValue: string.Empty).Trim();
        
        if (!string.Equals(a: apiKey, b: _apiKey, comparisonType: StringComparison.InvariantCulture)) {
            await writer.WriteLineAsync(value: Constants.API_KEY_AUTH_RESPONSE_FAILED)
                .ConfigureAwait(continueOnCapturedContext: false);
            await writer.FlushAsync()
                .ConfigureAwait(continueOnCapturedContext: false);
            _logger.LogWarning(message: "Failed API-KEY-AUTH with Request ({Request})", args: line);
            return;
        }

        await writer.WriteLineAsync(value: Constants.API_KEY_AUTH_RESPONSE_SUCCESS)
            .ConfigureAwait(continueOnCapturedContext: false);
        await writer.FlushAsync()
            .ConfigureAwait(continueOnCapturedContext: false);
        _logger.LogInformation(message: "Success for API-KEY-AUTH with Request ({Request})", args: line);

        await foreach (OpCodeMessage item in _messageQueue.Reader.ReadAllAsync(cancellationToken: cancellationToken)) {
            _logger.LogInformation(message: "Sending OpcodeMessage {Message}", args: item);
            await writer.WriteLineAsync(value: string.Format(format: Constants.OPCODE_MESSAGE_REQUEST, arg0: item))
                .ConfigureAwait(continueOnCapturedContext: false);
            await writer.FlushAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            line = await reader.ReadLineAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            //We expect a response, that contains the same information
            if (!string.Equals(
                a: line,
                b: string.Format(
                    format: Constants.OPCODE_MESSAGE_RESPONSE,
                    arg0: item))) {
                
                //Response was different!
                failedMessagesCounter++;
                _logger.LogWarning(message: "Failed with Sending OpcodeMessage {Message}", item.ToString());
            } else {
                //Response matched
                failedMessagesCounter--;
                _logger.LogWarning(message: "Succeeded with Sending OpcodeMessage {Message}", item.ToString());
            }             
            //More than 5 failures == unstable connection
            if (failedMessagesCounter < 5) {
                continue;
            }
            _logger.LogWarning(message: "FailedMessagesCounter reached over five, connection terminated");
            return;
        }
    }
}