using System.Text.Json.Serialization;
using Boßelwagen.Addons.Lib.Configuration;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Hub.Configuration;

public class HubConfigService(ILogger<HubConfigService> logger) : ConfigurationService<HubConfiguration>(logger) {
    protected override JsonSerializerContext Context => ConfigContext.Default;
    protected override HubConfiguration DefaultConfiguration => ConfigContext.DefaultConfiguration;
}