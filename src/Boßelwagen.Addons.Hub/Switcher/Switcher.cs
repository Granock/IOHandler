using Boßelwagen.Addons.Hub.Configuration;
using Boßelwagen.Addons.Hub.User32;
using Boßelwagen.Addons.Hub.User32.Internal;
using Boßelwagen.Addons.Lib.Configuration;
using Microsoft.Extensions.Logging;

namespace Boßelwagen.Addons.Hub.Switcher;

public class Switcher(IConfigurationService<HubConfiguration> configurationService, 
                      IUser32Wrapper user32Wrapper,
                      ILogger<Switcher> logger) : ISwitcher {
    
    private bool _initialized = false;
    private int _lastWindowIndex = -1;
    private SwitcherWindow[] _windows = [];

    public void SwitchWindow(SwitchType switchType = SwitchType.NextWindow) {
        try {
            SwitchWindowCore(switchType: switchType);
        } catch (Exception ex) {
            logger.LogError(message: "Error when switching Window: {Exception}", ex);
        }
    }

    private void SwitchWindowCore(SwitchType switchType) {
        logger.LogInformation(message: "Begin Switch Window, Type: {SwitchType}", switchType);
        if (!_initialized) {
            Initialize();
        }

        //Keine fenster wirkt falsch, init neu
        if (_windows.Length is 0) {
            logger.LogWarning("reset, as no Windows was present in switcher");
            Reset();
            return;
        }
        
        int? newIndex = switchType switch {
            SwitchType.NextWindow when _lastWindowIndex <= _windows.Length - 1 => _lastWindowIndex + 1,
            SwitchType.NextWindow when _lastWindowIndex >= _windows.Length - 1 => 0,
            SwitchType.PreviousWindow when _lastWindowIndex != 0 => _lastWindowIndex - 1,
            SwitchType.PreviousWindow when _lastWindowIndex is 0 => _windows.Length - 1,
            SwitchType.FirstWindow => 0,
            SwitchType.LastWindow => _windows.Length - 1,
            _ => null
        };

        if (newIndex is null) {
            throw new ArgumentException("windowToFocus is null");
        }

        if (newIndex > _windows.Length - 1 || newIndex < 0) {
            newIndex = 0;
        }
        
        SwitcherWindow window = _windows[newIndex.Value];
        
        logger.LogInformation(
            "Switching to window: {WindowName} (Index: {Index})", 
            window.Configuration.ProcessName, 
            newIndex.Value);
        
        _lastWindowIndex = newIndex.Value;
        
        if (!user32Wrapper.FocusWindow(window.Wrapper)) {
            throw new InvalidOperationException("failed to focus window");
        }
    }

    public void Reset() {
        _initialized = false;
        logger.LogInformation(message: "switcher was reset");
    }

    private void Initialize() {
        try {
            InitializeCore();
        } catch (Exception ex) {
            logger.LogError(message: "Error when Initializing: {Exception}", ex);
        }
    }

    private void InitializeCore() {
        logger.LogInformation(message: "Initializing Switcher");
        _lastWindowIndex = -1;
        List<WindowWrapper> allWindows = user32Wrapper.GetAllCurrentWindows();
        HubConfiguration config = configurationService.GetConfigurationAsync().AsTask().Result;
        List<SwitcherWindow> windows = [];

        logger.LogInformation(message: "Switcher found {WindowCount} windows", allWindows.Count);
        logger.LogInformation(message: "Switcher found {WindowConfig} window-configs", config.WindowConfigurations.Count);
        
        foreach (WindowWrapper windowWrapper in allWindows) {
            WindowConfiguration? windowConfiguration =
                config.WindowConfigurations.FirstOrDefault(x => x.ProcessName == windowWrapper.ProcessName);
            windowConfiguration ??= new WindowConfiguration(
                windowWrapper.ProcessName ?? string.Empty,
                -1
            );
            windows.Add(new SwitcherWindow(windowWrapper, windowConfiguration, windowConfiguration.ShowOrder is not -1));
        }

        int maxOrder = windows.Max(x => x.Configuration.ShowOrder);
        if (maxOrder is -1) {
            maxOrder = 0;
        }
        foreach (SwitcherWindow window in windows
                     .Where(x => x.Configuration.ShowOrder is -1)
                     .OrderBy(x => x.Configuration.ProcessName)
                     .ToList()) {
            maxOrder++;
            SwitcherWindow newWindow = window with {
                Configuration = window.Configuration with { ShowOrder = maxOrder }
            };
            windows.Remove(window);
            windows.Add(newWindow);
            
            logger.LogInformation(
                message: "Switcher found window {ProcessName} with order {ShowOrder} and Enabled: {EnabledForShowing}", 
                newWindow.Configuration.ProcessName, 
                newWindow.Configuration.ShowOrder,
                newWindow.Enabled);
        }

        config = config with {
            WindowConfigurations = windows
                .Where(x => x.Enabled)
                .Select(x => x.Configuration)
                .ToList()
        };
        
        _ = configurationService.SaveConfigurationAsync(config).AsTask();
        
        _windows = windows.Where(x => x.Enabled)
                          .OrderBy(x => x.Configuration.ShowOrder)
                          .ToArray();
        
        logger.LogInformation("Initialized Switcher with {WindowsCount} windows", _windows.Length);
        foreach (WindowConfiguration item in _windows.Select(x => x.Configuration)) {
            logger.LogInformation(
                "Window: {WindowName}, Order: {Order}", 
                item.ProcessName, 
                item.ShowOrder);
        }

        _initialized = true;
    }
    
}