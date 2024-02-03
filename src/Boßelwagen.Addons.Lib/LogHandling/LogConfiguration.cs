using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;

namespace Boßelwagen.Addons.Lib.LogHandling;

public static class LogConfiguration {

        
    public static IHostApplicationBuilder AddSerilogToHost(this IHostApplicationBuilder builder, 
                                                           string directory = "Logs/Files/") {
        builder.Logging.Services.AddSerilogToServices(directory: directory);
        return builder;
    }

    public static IServiceCollection AddSerilogToServices(this IServiceCollection services, 
                                                          string directory = "Logs/Files/") {
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
