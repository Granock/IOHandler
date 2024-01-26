using Boßelwagen.Addons.Hub.Configuration;
using Boßelwagen.Addons.Hub.User32.Internal;

namespace Boßelwagen.Addons.Hub.Switcher;

public record SwitcherWindow(WindowWrapper Wrapper, WindowConfiguration Configuration, bool Enabled);