namespace Boßelwagen.Addons.GpioMonitor.Configuration;

public record GpioConfiguration(
    Guid HandlerId, 
    string ApiKey, 
    IReadOnlyCollection<IoConfiguration> Configurations);