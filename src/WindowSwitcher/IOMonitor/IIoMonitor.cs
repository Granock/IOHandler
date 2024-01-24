namespace WindowSwitcher.IOMonitor;

/// <summary>
/// a service responsible for reading and interpreting input
/// from different io-systems. these can be a serial device
/// or the keyboard.
/// </summary>
public interface IIoMonitor {
    
    /// <summary>
    /// starts monitoring the io and interpreting the inputs.
    /// </summary>
    void StartIoMonitoring();
    
    /// <summary>
    /// stops monitoring the io and interpreting the inputs.
    /// </summary>
    void StopIoMonitoring();
}