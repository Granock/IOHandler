using WindowSwitcher.IOMonitor.Internal;

namespace WindowSwitcher.Configuration.Model;

public record WindowSwitcherConfiguration(
    IReadOnlyCollection<WindowConfiguration> WindowConfigurations, 
    string IpAddress,
    int Port,
    string ApiKey,
    MonitorType Monitor);