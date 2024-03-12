using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Boßelwagen.Addons.Core.User32.Internal;

namespace Boßelwagen.Addons.Core.User32;

[SuppressMessage("Interoperability", "SYSLIB1054:Verwenden Sie \\\"LibraryImportAttribute\\\" anstelle von \\\"DllImportAttribute\\\", um P/Invoke-Marshallingcode zur Kompilierzeit zu generieren.")]
public class User32Wrapper : IUser32Wrapper {

    #region Nativ Methods

    [DllImport(dllName: "user32.dll")]
    private static extern int EnumDesktopWindows(nint hDesktop, EnumWindowsProc lpfn, int lParam);
    private delegate bool EnumWindowsProc(nint hWnd, int lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(nint hWnd);

    [DllImport(dllName: "user32.dll")]
    private static extern int GetWindowThreadProcessId(nint hWnd, out int processId);

    [DllImport(dllName: "user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);
    private const int CommandSwShowmaximized = 3;

    [DllImport(dllName: "user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SwitchToThisWindow(nint hWnd, bool fUnknown);

    #endregion


    public List<WindowWrapper> GetAllCurrentWindows() {
        List<WindowWrapper> result = [];
        _ = EnumDesktopWindows(
            hDesktop: nint.Zero,
            lpfn: (x, _) => CollectWindow(x, result),
            lParam: 0);
        return result.DistinctBy(x => x.ProcessName).ToList();
    }

    private static bool CollectWindow(nint hWnd, [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")] List<WindowWrapper> output) {
        if (!IsWindowVisible(hWnd)) return true;

        //Get the process ID for the Window
        _ = GetWindowThreadProcessId(hWnd, out int processId);

        //Use the process ID to get the process
        Process process = Process.GetProcessById(processId);

        //Create the WindowWrapper
        WindowWrapper wrapper = new() {
            WindowHandle = hWnd,
            ProcessName = process.ProcessName
        };

        //Put WindowWrapper into Output
        output.Add(wrapper);

        return true;
    }

    public bool FocusWindow(WindowWrapper windowWrapper) {
        return windowWrapper.WindowHandle != nint.Zero
               && SwitchToThisWindow(hWnd: windowWrapper.WindowHandle, fUnknown: false);
    }

}