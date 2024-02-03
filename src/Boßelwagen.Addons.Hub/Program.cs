using Boßelwagen.Addons.Hub.Configuration;
using Boßelwagen.Addons.Hub.OpCodeExecutor;
using Boßelwagen.Addons.Hub.Switcher;
using Boßelwagen.Addons.Hub.User32;
using Boßelwagen.Addons.Lib.Communication.Receiver;
using Boßelwagen.Addons.Lib.Configuration;
using Boßelwagen.Addons.Lib.LogHandling;
using Boßelwagen.Addons.Lib.Operation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder hostbuilder = Host.CreateApplicationBuilder();
hostbuilder.AddSerilogToHost();

hostbuilder.Services.AddSingleton<IConfigurationService<HubConfiguration>, HubConfigService>();
hostbuilder.Services.AddSingleton(implementationFactory: x => {
    IConfigurationService<HubConfiguration> service = x.GetRequiredService<IConfigurationService<HubConfiguration>>();
    ValueTask<HubConfiguration> configTask = service.GetConfigurationAsync();
    configTask.AsTask().Wait();
    return configTask.Result;
});
hostbuilder.Services.AddSingleton(implementationFactory: x => {
    HubConfiguration config = x.GetRequiredService<HubConfiguration>();
    return new OpCodeReceiverConfig(
        Host: config.IpAddress, 
        Port: config.Port, 
        ApiKey: config.ApiKey);
});
hostbuilder.Services.AddSingleton<OpCodeReceiverFactory>();
hostbuilder.Services.AddHostedService(implementationFactory: x => {
    OpCodeReceiverFactory factory = x.GetRequiredService<OpCodeReceiverFactory>();
    HubConfiguration configuration = x.GetRequiredService<HubConfiguration>();
    return factory.BuildOpCodeReceiver(receiverTypeId: configuration.OpCodeReceiverType);
});
hostbuilder.Services.AddSingleton<IOpCodeExecutor, OpCodeSwitcherExecutor>();
hostbuilder.Services.AddHostedService(implementationFactory: x => x.GetRequiredService<IOpCodeExecutor>());
hostbuilder.Services.AddSingleton<ISwitcher, Switcher>();
hostbuilder.Services.AddSingleton<IUser32Wrapper, User32Wrapper>();

IHost host = hostbuilder.Build();

await host.RunAsync();