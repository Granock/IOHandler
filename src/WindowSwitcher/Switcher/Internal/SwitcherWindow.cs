using WindowSwitcher.Configuration.Model;
using WindowSwitcher.User32;
using WindowSwitcher.User32.Window.Internal;

namespace WindowSwitcher.Switcher.Internal;

public record SwitcherWindow(WindowWrapper Wrapper, WindowConfiguration Configuration, bool Enabled);