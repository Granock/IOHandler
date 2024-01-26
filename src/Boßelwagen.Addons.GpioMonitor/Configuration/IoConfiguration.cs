namespace Boßelwagen.Addons.GpioMonitor.Configuration;

public record IoConfiguration(
    int Pin, 
    bool SendOnRising, 
    bool SendOnFalling, 
    int OpCode);