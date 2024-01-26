using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Lib.Configuration;

public abstract class ConfigurationService<T> : IConfigurationService<T> where T : class {
    
    protected abstract JsonSerializerContext Context { get; }
    protected abstract T DefaultConfiguration { get; }

    private readonly ILogger _logger; 
    private const string CONFIG_FILE_NAME = "config.json";
    private static readonly string CONFIG_PATH = Path.Combine(Environment.CurrentDirectory, CONFIG_FILE_NAME);

    protected ConfigurationService(ILogger logger) {
        _logger = logger;
    }

    public async ValueTask<T> GetConfigurationAsync(CancellationToken cancellationToken = default) {
        try {
            return await GetConfigurationCoreAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        } catch (Exception ex) {
            _logger.LogError(ex, "Exception when reading config, falling back to default");
            return DefaultConfiguration;
        }
    }
    
    private async ValueTask<T> GetConfigurationCoreAsync(CancellationToken cancellationToken = default) {
        //File doesnt exist
        T configuration;
        if (!File.Exists(path: CONFIG_PATH)) {
            configuration = DefaultConfiguration;
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
            returnType: typeof(T), 
            context: Context, 
            cancellationToken: cancellationToken);

        //Config invalid
        if (obj is T config) {
            return config;
        }
        
        configuration = DefaultConfiguration;
        await SaveConfigurationAsync(configuration, cancellationToken);
        return configuration;

        //Config valid
    }

    public async ValueTask SaveConfigurationAsync(T configuration, CancellationToken cancellationToken = default) {
        try {
            await SaveConfigurationCoreAsync(configuration: configuration, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        } catch (Exception ex) {
            _logger.LogError(ex, "Exception when saving config");
        }
    }
    
    private async ValueTask SaveConfigurationCoreAsync(T configuration, CancellationToken cancellationToken = default) {
        if (File.Exists(CONFIG_PATH)) {
            File.Delete(CONFIG_PATH);
        }
        
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
            inputType: typeof(T), 
            context: Context,
            cancellationToken: cancellationToken);
    }

}