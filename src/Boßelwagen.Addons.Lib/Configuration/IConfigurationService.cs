namespace Boßelwagen.Addons.Lib.Configuration;

public interface IConfigurationService<T> where T : class {
    ValueTask<T> GetConfigurationAsync(CancellationToken cancellationToken = default);
    ValueTask SaveConfigurationAsync(T configuration, CancellationToken cancellationToken = default);
}