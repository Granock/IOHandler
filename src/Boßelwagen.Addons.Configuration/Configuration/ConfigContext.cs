using System.Text.Json.Serialization;
using Boßelwagen.Addons.Configuration.Model;

namespace Boßelwagen.Addons.Configuration.Configuration;

[JsonSerializable(typeof(HubConfiguration))]
[JsonSerializable(typeof(GpioConfiguration))]
[JsonSerializable(typeof(WindowConfiguration))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class ConfigContext : JsonSerializerContext {
    public static HubConfiguration DefaultConfiguration => 
        new (
        Windows: [
            new ("App1", 1),
            new ("App2", 2),
        ],
        Gpios: [ 
            new(1, false, false, Enums.SwitchType.NextWindow),
            new(2, false, false, Enums.SwitchType.PreviousWindow),
            new(3, false, false, Enums.SwitchType.FirstWindow),
            new(4, false, false, Enums.SwitchType.LastWindow)
        ]
    );
}