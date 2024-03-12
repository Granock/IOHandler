namespace Boßelwagen.Addons.Boardunit.Lib;

using System;
using System.Runtime.InteropServices;
using System.Threading;

public unsafe class FleetPcGpio {

    #region InteropServices

    [DllImport("kernel32.dll")]
    private static extern nint LoadLibrary(string DllName);

    [DllImport("kernel32.dll")]
    private static extern nint GetProcAddress(nint hModule, string ProcName);

    [DllImport("kernel32")]
    private static extern bool FreeLibrary(nint hModule);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool InitializeWinIoType();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool GetPortValType(ushort PortAddr, uint* pPortVal, ushort Size);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool SetPortValType(ushort PortAddr, uint PortVal, ushort Size);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool ShutdownWinIoType();

    private nint _hMod = nint.Zero;

    #endregion InteropServices

    private bool _initialized;
    public bool Initialized => _initialized;

    private readonly object _syncObj = new object();

    /// <summary>
    /// Gibt zurück, ob die Firmware Version >=42 ist. Die kleinen CarPCs (FleetPC 4D) haben diese Version und eine andere GPIO Hardware)
    /// </summary>
    public bool IsFleetPC4D { get; set; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="winIoPath">Beispiel: Programmstartverzeichnis/Libs/</param>
    /// <param name="error"></param>
    /// <returns></returns>
    public bool Initialize() {
        //DOKUMENTATION----------------------------------------------------
        //WINIO init Problem
        //Lösung: Als Administrator ausführen +
        //Für x64 system
        //bcdedit.exe /set TESTSIGNING ON

        //für x32 system
        //bcdedit.exe /set TESTSIGNING OFF

        //Wiederherstellen
        //bcdedit.exe /set loadoptions ENABLE_INTEGRITY_CHECKS
        //bcdedit.exe /set TESTSIGNING OFF
        //-----------------------------------------------------------------
        //Weitere Infos zu dem Thema:
        //http://www.codeproject.com/script/Articles/ArticleVersion.aspx?aid=394289&av=571979
        //https://msdn.microsoft.com/en-us/library/windows/hardware/dn653569(v=vs.85).aspx

        _initialized = false;

        //32 oder 64 Bit System?
        if (nint.Size == 4) {
            _hMod = LoadLibrary("WinIo32.dll");
        } else if (nint.Size == 8) {
            _hMod = LoadLibrary("WinIo64.dll");
        }

        if (_hMod == nint.Zero) {
            // Die WinIo dll konnte nicht gefunden werden. Stellen Sie sicher das sich die WinIo Bibliothek in dem Ordner der Anwendung befindet
            return false;
        }

        nint pFunc = GetProcAddress(_hMod, "InitializeWinIo");

        if (pFunc != nint.Zero) {
            InitializeWinIoType InitializeWinIo = (InitializeWinIoType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(InitializeWinIoType));
            bool Result = InitializeWinIo();
            if (!Result) {
                FreeLibrary(_hMod);
                //Fehler in der Funktion InitializeWinIo. Stellen Sie sicher das das Programm mit Administratoren Rechte ausgeführt wird.
            } else {
                pFunc = GetProcAddress(_hMod, "SetPortVal");
                if (pFunc != nint.Zero) {
                    SetPortValType SetPortVal = (SetPortValType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(SetPortValType));

                    //Anhand der Firmware prüfen, was für eine Art von CarPc wir sind.
                    if (GetFirmware() >= 0x42) {
                        IsFleetPC4D = true; //Fleet PC 4-D (Kleinere Rechner) (Wir gehen davon aus, dass die größeren Rechner diese Firmeware nicht haben. Anders können wir die unterschiedliche Hardware derzeit nicht identifizieren.)
                    }

                    if (IsFleetPC4D) {
                        SetPortVal(0x4E, 0x87, 1); // Enter Super IO
                        SetPortVal(0x4E, 0x87, 1); // Enter Super IO
                        SetPortVal(0x4E, 0x07, 1); // Select Logic 7 for GPIO
                        SetPortVal(0x4F, 0x06, 1); // Jump in GPIO
                        SetPortVal(0x4F, 0x30, 1); // Set Base Address Reg 0x30
                        SetPortVal(0x4F, 0x01, 1); // Set Base Address to Enabled
                        SetPortVal(0x4E, 0xAA, 1); // exit PNP mode
                    }

                    _initialized = true;
                } else {
                    //Fehler in der Funktion SetPortVal
                }
            }
        }
        return Initialized;
    }

    /// <summary>
    /// Set Power Off Delay Time. Wenn die Zündung ausgemacht wird, macht der FleetPC4D nach 3 Sekunden hart den Strom aus. Mit dieser Funktion wird dies auf X-Minuten angepasst.
    /// </summary>
    /// <param name="delayTimeInMinutes"></param>
    public bool SetDelayTime(ushort delayTimeInMinutes) {
        try {
            if (Initialized && IsFleetPC4D) { //Wird nicht von jedem CarPC unterstützt
                if (delayTimeInMinutes > 255) {
                    delayTimeInMinutes = 255;
                }

                ushort Delay_Value = delayTimeInMinutes; // Power Off Delay Time Value
                SMB_Write(0xF040, 0xAE, 0x31, Delay_Value); // SMB address:0xF04; Device address: 0xAE; Delay Time setting Reg: 0x31
                return true;
            }
        } catch {
            //Ignore exception
        }
        return false;
    }

    private void SMB_Write(ushort SMB_Addr, ushort DEV_Address, ushort DEV_Reg, ushort Value) {
        lock (_syncObj) {
            nint pFunc = GetProcAddress(_hMod, "SetPortVal");

            if (pFunc != nint.Zero) {
                SetPortValType SetPortVal = (SetPortValType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(SetPortValType));

                pFunc = GetProcAddress(_hMod, "GetPortVal");
                if (pFunc != nint.Zero) {
                    SetPortVal(SMB_Addr, 0xFF, 1);                                              // Clear SMBUS Status
                    Thread.Sleep(10);                                                           // Delay 10 ms (1000 is 1 Sec)
                    SetPortVal((ushort)(SMB_Addr + 0x04), DEV_Address, 1);                      // EEPROM Address AE -Write / AF -Read
                    Thread.Sleep(10);
                    SetPortVal((ushort)(SMB_Addr + 0x03), DEV_Reg, 1);                          // EEPROM Reg Address
                    Thread.Sleep(10);
                    SetPortVal((ushort)(SMB_Addr + 0x05), Value, 1);                            // Write Data in EEPROM
                    Thread.Sleep(10);
                    SetPortVal((ushort)(SMB_Addr + 0x02), 0x48, 1);                             // Start to Send
                    Thread.Sleep(10);
                }
            }
        }
    }

    private uint GetFirmware() {
        uint Firmware_Ver;
        Firmware_Ver = SMB_Read(0xF040, 0xAE, 0x13);
        return Firmware_Ver;
    }

    private uint SMB_Read(ushort SMB_Addr, ushort DEV_Address, ushort DEV_Reg) {
        lock (_syncObj) {
            nint pFunc = GetProcAddress(_hMod, "SetPortVal");

            if (pFunc != nint.Zero) {
                SetPortValType SetPortVal = (SetPortValType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(SetPortValType));

                pFunc = GetProcAddress(_hMod, "GetPortVal");
                if (pFunc != nint.Zero) {
                    GetPortValType GetPortVal = (GetPortValType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(GetPortValType));

                    uint Value;
                    SetPortVal(SMB_Addr, 0xFF, 1);                                              // Clear SMBUS Status
                    Thread.Sleep(10);                                                           // Delay 10 ms (1000 is 1 Sec)
                    SetPortVal((ushort)(SMB_Addr + 0x04), (ushort)(DEV_Address + 1), 1);        // EEPROM Address AE -Write / AF -Read
                    Thread.Sleep(10);
                    SetPortVal((ushort)(SMB_Addr + 0x03), DEV_Reg, 1);                          // EEPROM Reg Address
                    Thread.Sleep(10);
                    SetPortVal((ushort)(SMB_Addr + 0x02), 0x48, 1);                             // Start to Send
                    Thread.Sleep(10);
                    GetPortVal((ushort)(SMB_Addr + 0x05), &Value, 1);                           // Read Data in EEPROM
                    Thread.Sleep(10);
                    return Value;
                }
            }
        }
        return 0;
    }

    private bool GetPortValue(out uint out_PortVal, uint in_DIOPort) {
        uint PortVal = 0;
        if (Initialized) {
            lock (_syncObj) {
                nint pFunc = GetProcAddress(_hMod, "SetPortVal");

                if (pFunc != nint.Zero) {
                    SetPortValType SetPortVal = (SetPortValType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(SetPortValType));

                    pFunc = GetProcAddress(_hMod, "GetPortVal");
                    if (pFunc != nint.Zero) {
                        GetPortValType GetPortVal = (GetPortValType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(GetPortValType));

                        SetPortVal(0x4E, 0x87, 1); // Enter Super IO
                        SetPortVal(0x4E, 0x87, 1); // Enter Super IO
                        SetPortVal(0x4E, 0x07, 1); // Select Logic 7 for GPIO
                        SetPortVal(0x4F, 0x06, 1);
                        SetPortVal(0x4E, in_DIOPort, 1); // GPIO Status Register

                        // Call WinIo to get value
                        bool result = GetPortVal(0x4F, &PortVal, 1); // Read GPIO Status

                        SetPortVal(0x4E, 0xAA, 1); // exit PNP mode

                        out_PortVal = PortVal;

                        return result;

                        //FleetPC5D: 7    6    5    4    3    2    1    0
                        //           DO2  DO1  DT1  DT0  DI4  DI3  DI2  DI1
                        //           1    1    1    1    0    0    0    0
                    }
                }
            }
        }
        out_PortVal = PortVal;
        return false;
    }

    private bool SetPortValue(uint in_DIOPort, uint in_PortVal) {
        if (Initialized) {
            lock (_syncObj) {
                nint pFunc = GetProcAddress(_hMod, "SetPortVal");

                if (pFunc != nint.Zero) {
                    SetPortValType SetPortVal = (SetPortValType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(SetPortValType));

                    SetPortVal(0x4E, 0x87, 1); // Enter Super IO
                    SetPortVal(0x4E, 0x87, 1); // Enter Super IO
                    SetPortVal(0x4E, 0x07, 1); // Select Logic 7 for GPIO
                    SetPortVal(0x4F, 0x06, 1);
                    SetPortVal(0x4E, in_DIOPort, 1); // GPIO Status Register

                    bool result = SetPortVal(0x4F, in_PortVal, 1); // Write GPIO Status

                    SetPortVal(0x4E, 0xAA, 1); // exit PNP mode
                    return result;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Gibt die Temperatur des FleetPc zurück (Momentan nur FleetPC-4D)
    /// </summary>
    /// <returns></returns>
    public int? GetTempVal() {
        if (Initialized && IsFleetPC4D) {
            lock (_syncObj) {
                nint pFunc = GetProcAddress(_hMod, "SetPortVal");

                if (pFunc != nint.Zero) {
                    SetPortValType SetPortVal = (SetPortValType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(SetPortValType));

                    pFunc = GetProcAddress(_hMod, "GetPortVal");
                    if (pFunc != nint.Zero) {
                        GetPortValType GetPortVal = (GetPortValType)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(GetPortValType));

                        SetPortVal(0xA05, 0x74, 1); //Set Temp2 (74h) to Hardware Monitor Address Port (A05h)

                        // Call WinIo to get value
                        uint PortVal = 0;
                        GetPortVal(0xA06, &PortVal, 1);
                        return Convert.ToInt32(PortVal);
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Gibt zurück, ob auf dem übergebenen GPI-Port eine Spannung vorhanden ist.
    /// </summary>
    /// <param name="port"></param>
    /// <returns></returns>
    public bool HasVoltage(int port) {
        uint PortVal;
        if (GetPortValue(out PortVal, 0xA2)) {
            switch (port) {
                case 1:
                    return (PortVal & 0x01) == 0x01;

                case 2:
                    return (PortVal & 0x02) == 0x02;

                case 3:
                    return !IsFleetPC4D && (PortVal & 0x04) == 0x04;

                case 4:
                    return !IsFleetPC4D && (PortVal & 0x08) == 0x08;

                default:
                    return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Gibt zurück, ob die Zündung an ist.
    /// </summary>
    /// <returns></returns>
    public bool HasIgnitionVoltage() {
        if (Initialized) {
            uint DIOPort = IsFleetPC4D ? (uint)0xF2 : 0xD2;
            if (GetPortValue(out uint PortVal, DIOPort)) {
                if (IsFleetPC4D) {
                    return (PortVal & 0x08) == 0x08;
                } else {
                    return (PortVal & 0x10) == 0x10;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Setzt die Spannung der DO-Ports.
    /// </summary>
    /// <param name="in_PortNumber">Nummer des DO-Ports</param>
    /// <param name="in_HighVoltage">Gibt an, ob die Spannung auf dem Port auf Hoch(true) gesetzt werden soll oder nicht(false).</param>
    /// <returns></returns>
    public bool SetDOVoltage(int in_PortNumber, bool in_HighVoltage) {
        if (GetPortValue(out uint PortVal, 0xA2)) {
            if (IsFleetPC4D) {
                switch (in_PortNumber) {
                    case 1:
                        return SetPortValue(0xA1, in_HighVoltage ? PortVal | 0x10 : PortVal & 0xEF);

                    case 2:
                        return SetPortValue(0xA1, in_HighVoltage ? PortVal | 0x20 : PortVal & 0xDF);

                    default:
                        return false;
                }
            } else {
                switch (in_PortNumber) {
                    case 1:
                        return SetPortValue(0xA1, in_HighVoltage ? PortVal | 0x40 : PortVal & 0xB0);

                    case 2:
                        return SetPortValue(0xA1, in_HighVoltage ? PortVal | 0x80 : PortVal & 0x70);

                    default:
                        return false;
                }
            }
        }
        return false;
    }
}