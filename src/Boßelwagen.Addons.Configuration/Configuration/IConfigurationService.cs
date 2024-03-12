using Boßelwagen.Addons.Configuration.Model;

namespace Boßelwagen.Addons.Configuration.Configuration;

public interface IConfigurationService {
    ValueTask<HubConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
    ValueTask SaveConfigurationAsync(HubConfiguration configuration, CancellationToken cancellationToken = default);
}