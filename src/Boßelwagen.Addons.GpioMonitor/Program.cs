using System.Device.Gpio;
using Boßelwagen.Addons.GpioMonitor.Configuration;
using Boßelwagen.Addons.GpioMonitor.Gpio;
using Boßelwagen.Addons.Lib.Communication.Sender;
using Boßelwagen.Addons.Lib.Configuration;
using Boßelwagen.Addons.Lib.LogHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder hostbuilder = Host.CreateApplicationBuilder();
hostbuilder.AddSerilogToHost();

hostbuilder.Services.AddSingleton(implementationInstance: new GpioController());
hostbuilder.Services.AddSingleton<IConfigurationService<GpioConfiguration>, GpioMonitorConfigService>();
hostbuilder.Services.AddSingleton(implementationFactory: x => {
    IConfigurationService<GpioConfiguration> service = x.GetRequiredService<IConfigurationService<GpioConfiguration>>();
    ValueTask<GpioConfiguration> configTask = service.GetConfigurationAsync();
    configTask.AsTask().Wait();
    return configTask.Result;
});
hostbuilder.Services.AddHostedService<GpioMonitor>();
hostbuilder.Services.AddSingleton(implementationFactory: x => {
    GpioConfiguration configuration = x.GetRequiredService<GpioConfiguration>();
    return ActivatorUtilities.CreateInstance<OpCodeSender>(
        provider: x, 
        parameters: configuration.ApiKey);
});
hostbuilder.Services.AddHostedService(implementationFactory: x => x.GetRequiredService<OpCodeSender>());

IHost host = hostbuilder.Build();

await host.RunAsync();