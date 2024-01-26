using System.Text.Json.Serialization;

namespace Boßelwagen.Addons.Hub.Configuration;

[JsonSerializable(typeof(WindowConfiguration))]
[JsonSerializable(typeof(HubConfiguration))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class ConfigContext : JsonSerializerContext {
    public static HubConfiguration DefaultConfiguration => 
        new (
        WindowConfigurations: [], 
        IpAddress: "127.0.0.1",
        Port: 9999,
        ApiKey: "0000",
        OpCodeReceiverType: 1
    );
}