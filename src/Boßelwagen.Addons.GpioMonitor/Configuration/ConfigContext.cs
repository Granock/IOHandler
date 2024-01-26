using System.Text.Json.Serialization;

namespace Boßelwagen.Addons.GpioMonitor.Configuration;

[JsonSerializable(typeof(GpioConfiguration))]
[JsonSerializable(typeof(IoConfiguration))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class ConfigContext : JsonSerializerContext {
    public static GpioConfiguration DefaultConfiguration => new(
        HandlerId: System.Guid.NewGuid(), 
        ApiKey: "0000", 
        Configurations: new[] { 
            new IoConfiguration(
                Pin: 23, 
                SendOnRising: true, 
                SendOnFalling: true, 
                OpCode: 1) 
        });
}