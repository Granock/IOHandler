using System.Text.Json;
using Boßelwagen.Addons.Configuration.Model;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Configuration.Configuration;

public class ConfigurationService(ILogger<ConfigurationService> logger) : IConfigurationService {

    private readonly ILogger _logger = logger;
    private const string CONFIG_FILE_NAME = "config.json";
    private static readonly string CONFIG_PATH = Path.Combine(Environment.CurrentDirectory, CONFIG_FILE_NAME);

    public async ValueTask<HubConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default) {
        try {
            return await GetConfigurationCoreAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        } catch (Exception ex) {
            _logger.LogError(ex, "Exception when reading config, falling back to default");
            return ConfigContext.DefaultConfiguration;
        }
    }

    private async ValueTask<HubConfiguration> GetConfigurationCoreAsync(CancellationToken cancellationToken = default) {
        //File doesnt exist
        HubConfiguration configuration;
        if (!File.Exists(path: CONFIG_PATH)) {
            configuration = ConfigContext.DefaultConfiguration;
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
            returnType: typeof(HubConfiguration),
            context: ConfigContext.Default,
            cancellationToken: cancellationToken);

        //Config invalid
        if (obj is HubConfiguration config) {
            return config;
        }

        configuration = ConfigContext.DefaultConfiguration;
        await SaveConfigurationAsync(configuration, cancellationToken);
        return configuration;

        //Config valid
    }

    public async ValueTask SaveConfigurationAsync(HubConfiguration configuration, CancellationToken cancellationToken = default) {
        try {
            await SaveConfigurationCoreAsync(configuration: configuration, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        } catch (Exception ex) {
            _logger.LogError(ex, "Exception when saving config");
        }
    }

    private async ValueTask SaveConfigurationCoreAsync(HubConfiguration configuration, CancellationToken cancellationToken = default) {
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
            inputType: typeof(HubConfiguration),
            context: ConfigContext.Default,
            cancellationToken: cancellationToken);
    }

}