using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using WindowSwitcher.Configuration.Model;
using WindowSwitcher.Configuration.Service;
using WindowSwitcher.IOMonitor.Internal;
using WindowSwitcher.Switcher;

namespace WindowSwitcher.IOMonitor.Implementation;

public class TcpIoMonitor(
    ISwitcher switcher,
    ILogger<TcpIoMonitor> logger,
    IConfigurationService configurationService) : BaseIoMonitor(switcher, logger) {
    
    private const string API_KEY_AUTH_REQUEST = "API-KEY-AUTH:REQUEST:";
    private const string API_KEY_AUTH_RESPONSE_SUCCESS = "API-KEY-AUTH:RESPONSE:SUCCESS";
    private const string OPCODE_MESSAGE_RESPONSE = "OPCODE:RESPONSE:{0}:{1}";
    
    private CancellationTokenSource _cancellationTokenSource = new();
    private Task? _workerTask;

    public override void StartIoMonitoring() {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        _workerTask = Task.Run(
            function: () => TcpConnectionWorkerAsync(cancellationToken: _cancellationTokenSource.Token),
            cancellationToken: _cancellationTokenSource.Token);
    }

    public override void StopIoMonitoring() {
        _cancellationTokenSource.Cancel();
    }

    private async Task TcpConnectionWorkerAsync(CancellationToken cancellationToken) {
        logger.LogInformation("Started TcpConnectionWorkerAsync-Thread");
        try {
            await TcpConnectionWorkerCoreAsync(cancellationToken).ConfigureAwait(false);
        } catch (Exception ex) {
            logger.LogError(ex, "Error in TcpConnectionWorkerCoreAsync");
            throw;
        }
        logger.LogInformation("Stopped TcpConnectionWorkerAsync-Thread");
    }
    
    private async Task TcpConnectionWorkerCoreAsync(CancellationToken cancellationToken) {
        WindowSwitcherConfiguration config = await configurationService.GetConfigurationAsync(cancellationToken);
        while (!cancellationToken.IsCancellationRequested) {
            try {
                await TryConnectAndReceiveAsync(config, cancellationToken).ConfigureAwait(false);
            } catch (Exception ex) {
                logger.LogError(ex, "Exception in TryConnectAndReceiveAsync");
            }
        }
    }

    private async Task TryConnectAndReceiveAsync(WindowSwitcherConfiguration config, CancellationToken cancellationToken) {
        using Socket socket = new(SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(config.IpAddress, config.Port, cancellationToken)
            .ConfigureAwait(false);
        
        await using NetworkStream stream = new(socket);
        using StreamReader reader = new(stream);
        using StreamWriter writer = new(stream);

        //SEND AUTH-REQUEST
        logger.LogWarning("sending auth-request to client");
        string authRequest = string.Concat(str0: API_KEY_AUTH_REQUEST, str1: config.ApiKey);
        await writer.WriteLineAsync(value: authRequest)
            .ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken)
            .ConfigureAwait(false);
        
        if (await reader.ReadLineAsync(cancellationToken) is not API_KEY_AUTH_RESPONSE_SUCCESS) {
            logger.LogWarning("failed auth-request to client");
        }


        logger.LogWarning("succeded auth-request to client");

        while (socket.Connected && !cancellationToken.IsCancellationRequested) {
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Received client message: {message}", line);
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] splits = line.Split(":");
            int opcode = int.Parse(splits[2]);
            long milis = long.Parse(splits[3]);
            DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(milis);

            logger.LogInformation("Received Opcode: {opcode}, Timestamp: {timestamp}", opcode, timestamp);

            string opCodeResponse = string.Format(OPCODE_MESSAGE_RESPONSE, opcode, timestamp.ToUnixTimeMilliseconds());
            await writer.WriteLineAsync(opCodeResponse).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            OperationReceived(opcode, timestamp);
        }
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (!disposing) return;
        _cancellationTokenSource.Dispose();
        _workerTask?.Dispose();
    }
    
}