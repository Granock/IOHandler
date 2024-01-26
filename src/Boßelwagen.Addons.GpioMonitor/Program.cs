using System.Device.Gpio;
using Boßelwagen.Addons.GpioMonitor.Configuration;
using Boßelwagen.Addons.GpioMonitor.Gpio;
using Boßelwagen.Addons.Lib.Communication;
using Boßelwagen.Addons.Lib.Communication.Sender;
using Boßelwagen.Addons.Lib.Configuration;
using Boßelwagen.Addons.Lib.LogHandling;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine(value: "Starting.....");

IServiceCollection services = new ServiceCollection();
services.AddSingleton(implementationInstance: new GpioController());
services.AddSingleton<IConfigurationService<GpioConfiguration>, GpioMonitorConfigService>();
services.AddSingleton<GpioConfiguration>(x => {
    IConfigurationService<GpioConfiguration> service = x.GetRequiredService<IConfigurationService<GpioConfiguration>>();
    ValueTask<GpioConfiguration> configTask = service.GetConfigurationAsync();
    configTask.AsTask().Wait();
    return configTask.Result;
});
services.ConfigureLoggingWithSerilog();
services.AddSingleton<GpioMonitor>();
services.AddSingleton<OpCodeSender>(x => {
    GpioConfiguration configuration = x.GetRequiredService<GpioConfiguration>();
    return ActivatorUtilities.CreateInstance<OpCodeSender>(x, configuration.ApiKey);
});

IServiceProvider provider = services.BuildServiceProvider();

using (IServiceScope scope = provider.CreateScope()) {
    using GpioMonitor handling = scope.ServiceProvider.GetRequiredService<GpioMonitor>();
    Console.WriteLine(value: ".....Started");

    Console.ReadLine();

    Console.WriteLine(value: "Stopping.....");
}

Console.WriteLine(value: ".....Stopped");