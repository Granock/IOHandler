using Boßelwagen.Addons.Configuration.Enums;

namespace Boßelwagen.Addons.Core.Switcher;

/// <summary>
///  a simple service responsible for switching between windows
/// </summary>
public interface ISwitcher {
    /// <summary>
    /// instructs the <see cref="ISwitcher"/> to focus a window
    /// </summary>
    /// <param name="switchType">what window to focus,
    /// relational to the last focused window</param>
    void SwitchWindow(SwitchType switchType = SwitchType.NextWindow);

    /// <summary>
    /// resets the <see cref="ISwitcher"/>, so the
    /// next <see cref="SwitchWindow"/> focuses
    /// the first window in the order
    /// </summary>
    void Reset();
}
