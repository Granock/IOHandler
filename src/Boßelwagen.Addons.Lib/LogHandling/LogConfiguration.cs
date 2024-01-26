using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Compact;

namespace Boßelwagen.Addons.Lib.LogHandling;

public static class LogConfiguration {


    public static IServiceCollection ConfigureLoggingWithSerilog(this IServiceCollection services, string directory = "Logs/Files/") {
        services.AddLogging();
        services.AddSerilog(configureLogger: x => {
            x.WriteTo.Console();
            x.WriteTo.File(
                path: directory,
                formatter: new CompactJsonFormatter(),
                rollingInterval: RollingInterval.Hour,
                retainedFileCountLimit: 48);
        });
        return services;
    }

}
