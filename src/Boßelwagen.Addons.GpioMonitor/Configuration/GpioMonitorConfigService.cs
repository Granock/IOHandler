using System.Text.Json.Serialization;
using Boßelwagen.Addons.Lib.Configuration;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.GpioMonitor.Configuration;

public class GpioMonitorConfigService : ConfigurationService<GpioConfiguration> {
    public GpioMonitorConfigService(ILogger<GpioMonitorConfigService> logger) : base(logger) { }
    protected override JsonSerializerContext Context => ConfigContext.Default;
    protected override GpioConfiguration DefaultConfiguration => ConfigContext.DefaultConfiguration;
}