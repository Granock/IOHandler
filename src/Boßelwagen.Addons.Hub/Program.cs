using Boßelwagen.Addons.Hub.Configuration;
using Boßelwagen.Addons.Hub.OpCodeExecutor;
using Boßelwagen.Addons.Hub.Switcher;
using Boßelwagen.Addons.Hub.User32;
using Boßelwagen.Addons.Lib.Communication.Receiver;
using Boßelwagen.Addons.Lib.Configuration;
using Boßelwagen.Addons.Lib.LogHandling;
using Boßelwagen.Addons.Lib.Operation;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine(value: "Starting.....");

IServiceCollection services = new ServiceCollection();
services.AddSingleton<IConfigurationService<HubConfiguration>, HubConfigService>();
services.AddSingleton<HubConfiguration>(x => {
    IConfigurationService<HubConfiguration> service = x.GetRequiredService<IConfigurationService<HubConfiguration>>();
    ValueTask<HubConfiguration> configTask = service.GetConfigurationAsync();
    configTask.AsTask().Wait();
    return configTask.Result;
});
services.AddSingleton<OpCodeReceiverConfig>(x => {
    HubConfiguration config = x.GetRequiredService<HubConfiguration>();
    return new OpCodeReceiverConfig(config.IpAddress, config.Port, config.ApiKey);
});
services.AddSingleton<OpCodeReceiverFactory>();
services.AddSingleton<IOpCodeReceiver>(x => {
    OpCodeReceiverFactory factory = x.GetRequiredService<OpCodeReceiverFactory>();
    HubConfiguration configuration = x.GetRequiredService<HubConfiguration>();
    return factory.BuildOpCodeReceiver(configuration.OpCodeReceiverType);
});
services.AddSingleton<IOpCodeExecutor, OpCodeSwitcherExecutor>();
services.AddSingleton<ISwitcher, Switcher>();
services.AddSingleton<IUser32Wrapper, User32Wrapper>();
services.ConfigureLoggingWithSerilog();
IServiceProvider provider = services.BuildServiceProvider();

await using (AsyncServiceScope scope = provider.CreateAsyncScope()) {
    IOpCodeReceiver receiver = scope.ServiceProvider.GetRequiredService<IOpCodeReceiver>();
    receiver.StartReceiving();
    Console.WriteLine(value: ".....Started");

    Console.ReadLine();

    Console.WriteLine(value: "Stopping.....");
    receiver.StopReceiving();
}

Console.WriteLine(value: ".....Stopped");