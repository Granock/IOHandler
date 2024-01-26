namespace Boßelwagen.Addons.Hub.Configuration;

public record HubConfiguration(
    IReadOnlyCollection<WindowConfiguration> WindowConfigurations, 
    string IpAddress,
    int Port,
    string ApiKey,
    int OpCodeReceiverType);