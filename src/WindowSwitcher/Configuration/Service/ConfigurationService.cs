using System.Text.Json;
using Microsoft.Extensions.Logging;
using WindowSwitcher.Configuration.Model;

namespace WindowSwitcher.Configuration.Service;

public class ConfigurationService(ILogger<ConfigurationService> logger) : IConfigurationService {

    private const string ConfigFileName = "config.json";
    private static readonly string ConfigDirectory = Environment.CurrentDirectory;
    private static string ConfigPath => Path.Combine(ConfigDirectory, ConfigFileName);
    
    private WindowSwitcherConfiguration? _configuration;
    
    public async ValueTask<WindowSwitcherConfiguration> GetConfigurationAsync(
            CancellationToken cancellationToken = default) {
        //check cache
        logger.LogInformation(message: "Reading Configuration");
        if (_configuration is not null) {
            return _configuration;
        }

        
        logger.LogInformation(message: "Reading Configuration into Cache");
        //config-file exists
        if (File.Exists(path: ConfigPath)) {
            //read config-file
            await using FileStream stream = File.OpenRead(path: ConfigPath);
            object? configObject = await JsonSerializer.DeserializeAsync(
                utf8Json: stream, 
                returnType: typeof(WindowSwitcherConfiguration),
                context: ConfigurationJsonContext.Default, 
                cancellationToken: cancellationToken);
        
            //config-file is valid
            if (configObject is WindowSwitcherConfiguration switcherConfiguration) {
                _configuration = switcherConfiguration;
                return switcherConfiguration;
            }
        }
        
        logger.LogInformation(message: "Writing new Configuration onto Disk");
        //config-file is invalid or doesnt exist
        //so create default, save, and cache it
        _configuration = await SaveConfigurationInternalAsync(
            configuration: ConfigurationJsonContext.GetDefaultConfig(), 
            cancellationToken: cancellationToken);
        
        return _configuration;
    }

    public async ValueTask SaveConfigurationAsync(
            WindowSwitcherConfiguration configuration, 
            CancellationToken cancellationToken = default) {
        //save and update cache
        _configuration = await SaveConfigurationInternalAsync(
            configuration: configuration, 
            cancellationToken: cancellationToken);
    }

    private async ValueTask<WindowSwitcherConfiguration> SaveConfigurationInternalAsync(
        WindowSwitcherConfiguration configuration,
        CancellationToken cancellationToken) {
        logger.LogInformation(message: "Writing new/updated Configuration onto Disk");
        //Open or Create File
        if (File.Exists(path: ConfigPath)) {
            File.Delete(path: ConfigPath);
        }
        await using FileStream stream = File.OpenWrite(path: ConfigPath);
        
        //Write new Config
        await JsonSerializer.SerializeAsync(
            utf8Json: stream, 
            value: configuration, 
            inputType: typeof(WindowSwitcherConfiguration), 
            context: ConfigurationJsonContext.Default, 
            cancellationToken: cancellationToken);

        //Cache new Config
        return configuration;
    }
}