using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WindowSwitcher.Configuration.Model;
using WindowSwitcher.Configuration.Service;
using WindowSwitcher.IOMonitor.Implementation;
using WindowSwitcher.IOMonitor.Internal;

namespace WindowSwitcher.IOMonitor.Factory;

public class IoMonitorFactory(
    IServiceProvider serviceProvider, 
    IConfigurationService configurationService, 
    ILogger<IoMonitorFactory> logger) {

    public async ValueTask<IIoMonitor> BuildIoMonitorAsync() {
        logger.LogInformation(message: "Building IoMonitor");
        WindowSwitcherConfiguration config = await configurationService.GetConfigurationAsync();
        Type ioMonitorType = config.Monitor switch {
            MonitorType.Timer => typeof(TimerIoMonitor),
            MonitorType.Tcp => typeof(TcpIoMonitor),
            _ => throw new ArgumentException("Monitor-Type in config not available")
        };
        logger.LogInformation(message: "Building IoMonitor {TypeName}", args: ioMonitorType.Name);
        return (IIoMonitor) ActivatorUtilities.CreateInstance(serviceProvider, ioMonitorType);
    }
    
}