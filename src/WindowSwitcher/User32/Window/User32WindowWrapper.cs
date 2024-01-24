using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using WindowSwitcher.User32.Window.Internal;

namespace WindowSwitcher.User32.Window;

[SuppressMessage("Interoperability", "SYSLIB1054:Verwenden Sie \\\"LibraryImportAttribute\\\" anstelle von \\\"DllImportAttribute\\\", um P/Invoke-Marshallingcode zur Kompilierzeit zu generieren.")]
public class User32WindowWrapper : IUser32WindowWrapper {

    #region Nativ Methods
    
    [DllImport(dllName: "user32.dll")]
    private static extern int EnumDesktopWindows(IntPtr hDesktop, EnumWindowsProc lpfn, int lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
    
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    
    [DllImport(dllName: "user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);
    
    [DllImport(dllName: "user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int CommandSwShowmaximized = 3;
    
    [DllImport(dllName: "user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SwitchToThisWindow(IntPtr hWnd, bool fUnknown);
    
    #endregion


    public List<WindowWrapper> GetAllCurrentWindows() {
        List<WindowWrapper> result = [];
        _ = EnumDesktopWindows(
            hDesktop: IntPtr.Zero,
            lpfn: (x, _) => CollectWindow(x, result), 
            lParam: 0);
        return result.DistinctBy(x => x.ProcessName).ToList();
    }

    private static bool CollectWindow(IntPtr hWnd, [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")] List<WindowWrapper> output) {
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
        return windowWrapper.WindowHandle != IntPtr.Zero 
               && SwitchToThisWindow(hWnd: windowWrapper.WindowHandle, fUnknown: false);
    }
    
}