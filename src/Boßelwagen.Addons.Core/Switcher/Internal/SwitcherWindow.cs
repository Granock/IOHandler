using Boßelwagen.Addons.Configuration.Model;
using Boßelwagen.Addons.Core.User32.Internal;

namespace Boßelwagen.Addons.Core.Switcher.Internal;

public record SwitcherWindow(WindowWrapper Wrapper, WindowConfiguration Configuration, bool Enabled);