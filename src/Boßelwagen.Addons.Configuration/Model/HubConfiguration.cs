namespace Boßelwagen.Addons.Configuration.Model;

public record HubConfiguration(
    IReadOnlyCollection<WindowConfiguration> Windows, 
    IReadOnlyCollection<GpioConfiguration> Gpios);