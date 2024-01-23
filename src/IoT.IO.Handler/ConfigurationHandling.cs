using System.Text.Json;
using System.Text.Json.Serialization;

namespace IoT.IO.Handler.Configuration;

internal static class ConfigurationHandling { 

    private static GpioConfiguration DefaultConfiguration => new(
        HandlerId: Guid.NewGuid(), 
        ApiKey: "0000", 
        Configurations: new[] { 
            new IoConfiguration(
                Pin: 23, 
                SendOnRising: true, 
                SendOnFalling: true, 
                OpCode: 1) 
        });

    private const string CONFIG_FILE_NAME = "config.json";
    private static readonly string CONFIG_PATH = Path.Combine(Environment.CurrentDirectory, CONFIG_FILE_NAME);


    public static async ValueTask<GpioConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default) {
        //File doesnt exist
        if (!File.Exists(path: CONFIG_PATH)) {
            GpioConfiguration configuration = DefaultConfiguration;
            await SaveConfigurationAsync(configuration, cancellationToken);
            return configuration;
        }

        //Open existing File
        await using FileStream fs = new(
            path: CONFIG_PATH,
            mode: FileMode.Open,
            access: FileAccess.Read,
            share: FileShare.None);
        
        //Parse Config
        object? obj = await JsonSerializer.DeserializeAsync(
            utf8Json: fs, 
            returnType: typeof(GpioConfiguration), 
            context: ConfigurationContext.Default, 
            cancellationToken: cancellationToken);

        //Config invalid
        if (obj is not GpioConfiguration config) {
            GpioConfiguration configuration = DefaultConfiguration;
            await SaveConfigurationAsync(configuration, cancellationToken);
            return configuration;
        }

        //Config valid
        return config;
    }

    private static async ValueTask SaveConfigurationAsync(GpioConfiguration configuration,
                                                          CancellationToken cancellationToken = default) {
        //Open or Create File
        await using FileStream fs = new(
            path: CONFIG_PATH, 
            mode: FileMode.OpenOrCreate, 
            access: FileAccess.Write, 
            share: FileShare.None);

        //Save new Config
        await JsonSerializer.SerializeAsync(
            utf8Json: fs, 
            value: configuration, 
            inputType: typeof(GpioConfiguration), 
            context: ConfigurationContext.Default,
            cancellationToken: cancellationToken);
    }

}

[JsonSerializable(typeof(GpioConfiguration))]
[JsonSerializable(typeof(IoConfiguration))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class ConfigurationContext : JsonSerializerContext { }

internal record GpioConfiguration(
    Guid HandlerId, 
    string ApiKey, 
    IReadOnlyCollection<IoConfiguration> Configurations);

internal record IoConfiguration(
    int Pin, 
    bool SendOnRising, 
    bool SendOnFalling, 
    int OpCode);