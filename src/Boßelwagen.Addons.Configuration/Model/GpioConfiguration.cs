using Boßelwagen.Addons.Configuration.Enums;

namespace Boßelwagen.Addons.Configuration.Model;
public record GpioConfiguration(int Pin, bool SendOnRising, bool SendOnFalling, SwitchType SwitchType);