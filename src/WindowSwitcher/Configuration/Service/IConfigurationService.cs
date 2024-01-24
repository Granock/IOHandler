using WindowSwitcher.Configuration.Model;

namespace WindowSwitcher.Configuration.Service;

/// <summary>
/// loads and saves the configuration of the application
/// </summary>
public interface IConfigurationService {
     
     /// <summary>
     /// asynchronously loads the configuration
     /// </summary>
     /// <param name="cancellationToken">a <see cref="CancellationToken"/> to cancel the operation if needed</param>
     /// <returns>a <see cref="ValueTask{TResult}"/> representing the loading-operation</returns>
     ValueTask<WindowSwitcherConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
     
     /// <summary>
     /// synchronous version if <see cref="GetConfigurationAsync"/>
     /// </summary>
     /// <returns>the loaded <see cref="WindowSwitcherConfiguration"/></returns>
     WindowSwitcherConfiguration GetConfiguration()
          => Task.Run(async () => await GetConfigurationAsync()).Result;
     
     ///<summary>
     /// asynchronously saves the configuration
     /// </summary>
     /// <param name="configuration">the new configuration to be saved</param>
     /// <param name="cancellationToken">a <see cref="CancellationToken"/> to cancel the operation if needed</param>
     /// <returns>a <see cref="ValueTask"/> representing the saving-operation</returns>
     ValueTask SaveConfigurationAsync(WindowSwitcherConfiguration configuration, 
                                      CancellationToken cancellationToken = default);
     
     /// <summary>
     /// synchronous version if <see cref="SaveConfigurationAsync"/>
     /// </summary>
     /// <param name="configuration">the new configuration to be saved</param>
     void SaveConfiguration(WindowSwitcherConfiguration configuration)
          => Task.Run(async () => await SaveConfigurationAsync(configuration)).Wait();
}