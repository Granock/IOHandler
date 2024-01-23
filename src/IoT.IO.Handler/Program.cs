
using IoT.IO.Handler;
using IoT.IO.Handler.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Compact;
using System.Device.Gpio;

Console.WriteLine(value: "Starting.....");

using CancellationTokenSource tokenSource = new(delay: new TimeSpan(hours: 0, minutes: 3, seconds: 0));
GpioConfiguration gpioConfiguration = await ConfigurationHandling.GetConfigurationAsync(cancellationToken: tokenSource.Token);

Console.WriteLine(value: "Config has been read");


IServiceCollection services = new ServiceCollection();
services.AddSingleton(implementationInstance: gpioConfiguration);
services.AddSingleton(implementationInstance: new GpioController());
services.AddSingleton<GpioHandling>();
services.AddLogging();
services.AddSerilog(configureLogger: x => {
    x.WriteTo.Console();
    x.WriteTo.File(
        path: "Logs/Files/",
        formatter: new CompactJsonFormatter(),
        rollingInterval: RollingInterval.Hour,
        retainedFileCountLimit: 48);
});

Console.WriteLine(value: "Service Setup finished");


services.AddSingleton<CommunicationHandling>();
IServiceProvider provider = services.BuildServiceProvider();

using (IServiceScope scope = provider.CreateScope()) {
    using GpioHandling handling = scope.ServiceProvider.GetRequiredService<GpioHandling>();
    Console.WriteLine(value: ".....Started");

    Console.ReadLine();

    Console.WriteLine(value: "Stopping.....");
}
Console.WriteLine(value: ".....Stopped");