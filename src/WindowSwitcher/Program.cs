using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Compact;
using WindowSwitcher.Configuration.Service;
using WindowSwitcher.IOMonitor;
using WindowSwitcher.IOMonitor.Factory;
using WindowSwitcher.Switcher;
using WindowSwitcher.User32.Window;

Console.WriteLine(value: "Registering Basic Services ...");
IServiceCollection serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<IConfigurationService, ConfigurationService>();
serviceCollection.AddSingleton<IUser32WindowWrapper, User32WindowWrapper>();
serviceCollection.AddSingleton<ISwitcher, Switcher>();
serviceCollection.AddSingleton<IoMonitorFactory>();
serviceCollection.AddSingleton(implementationFactory: x => {
    IoMonitorFactory factory = x.GetRequiredService<IoMonitorFactory>();
    ValueTask<IIoMonitor> build = factory.BuildIoMonitorAsync();
    build.AsTask().Wait();
    return build.Result;
});
serviceCollection.AddLogging();
serviceCollection.AddSerilog(configureLogger: x => {
    x.WriteTo.Console();
    x.WriteTo.File(
        path: "Logs/Files/",
        formatter: new CompactJsonFormatter(),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7);
    x.MinimumLevel.Information();
});
IServiceProvider provider = serviceCollection.BuildServiceProvider();
Console.WriteLine(value: "Basic Services registered");

Console.WriteLine(value: "Creating Service Scope ...");
await using AsyncServiceScope scope = provider.CreateAsyncScope();
Console.WriteLine(value: "Created Service Scope");

Console.WriteLine(value: "Starting Monitoring ...");
IIoMonitor ioMonitor = scope.ServiceProvider.GetRequiredService<IIoMonitor>();
ioMonitor.StartIoMonitoring();
Console.WriteLine(value: "Monitoring started");

Console.WriteLine(value: "Press any key to Stop Monitoring");
Console.ReadLine();
Console.WriteLine(value: "Keypress recognized");

Console.WriteLine(value: "Stopping Monitoring ...");
ioMonitor.StopIoMonitoring();
Console.WriteLine(value: "Monitoring stopped");