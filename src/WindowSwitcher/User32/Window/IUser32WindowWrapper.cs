using WindowSwitcher.User32.Window.Internal;

namespace WindowSwitcher.User32.Window;

/// <summary>
/// wrapper for user32-methods that work with the windows
/// </summary>
public interface IUser32WindowWrapper {
    
    /// <summary>
    /// loads a snapshot of all windows that are currently open
    /// </summary>
    /// <returns>a <see cref="List{T}"/> of <see cref="WindowWrapper"/>
    /// that represent all currently open windows</returns>
    List<WindowWrapper> GetAllCurrentWindows();
    
    /// <summary>
    /// instructs the os to focus the window represented by <paramref name="windowWrapper"/>
    /// </summary>
    /// <param name="windowWrapper">a <see cref="WindowWrapper"/> representing a specific window</param>
    /// <returns>if the window was focused correctly</returns>
    bool FocusWindow(WindowWrapper windowWrapper);
}