using Boßelwagen.Addons.Configuration.Configuration;
using Boßelwagen.Addons.Core.Switcher;
using Boßelwagen.Addons.Core.User32;
using Boßelwagen.Addons.Lib.Gpio.Monitor.Implementation;
using Boßelwagen.Addons.Lib.LogHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder hostbuilder = Host.CreateApplicationBuilder();
hostbuilder.AddSerilogToHost();

hostbuilder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
hostbuilder.Services.AddHostedService<BoardUnitMonitor>();
hostbuilder.Services.AddSingleton<ISwitcher, Switcher>();
hostbuilder.Services.AddSingleton<IUser32Wrapper, User32Wrapper>();

IHost host = hostbuilder.Build();

await host.RunAsync();