using System.Text.Json.Serialization;

namespace WindowSwitcher.Configuration.Model;

[JsonSerializable(typeof(WindowConfiguration))]
[JsonSerializable(typeof(WindowSwitcherConfiguration))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class ConfigurationJsonContext : JsonSerializerContext {

    public static WindowSwitcherConfiguration GetDefaultConfig() =>
        new (
            WindowConfigurations: [], 
            IpAddress: "127.0.0.1",
            Port: 9999,
            Monitor: IOMonitor.Internal.MonitorType.Timer
            );
}