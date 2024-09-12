using Microsoft.Win32;
using System.Drawing;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SetMaxRefreshRate
{

    public class Program
    {
        // Struct for holding monitor/device information
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        // Importing EnumDisplayDevices from User32.dll
        [DllImport("User32.dll", CharSet = CharSet.Ansi)]
        public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        // Struct for display settings (refresh rate, resolution, etc.)
        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public ushort dmSpecVersion;
            public ushort dmDriverVersion;
            public ushort dmSize;
            public ushort dmDriverExtra;
            public uint dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public ushort dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;
            public uint dmDisplayFlags;
            public uint dmDisplayFrequency; // This is the refresh rate
            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }

        // Import EnumDisplaySettings from User32.dll
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
        static bool IsRefreshRateCloseEnough(float currentRate, float targetRate, float tolerance = 0.01f)
        {
            return Math.Abs(currentRate - targetRate) <= tolerance * Math.Max(Math.Abs(currentRate), Math.Abs(targetRate));
        }

        // Import ChangeDisplaySettings API
        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

        const int DISP_CHANGE_SUCCESSFUL = 0;
        const int CDS_UPDATEREGISTRY = 0x01;
        const int CDS_TEST = 0x02;

        static void Main(string[] args)
        {
            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            d.cb = Marshal.SizeOf(d);
            uint devNum = 0;

            Console.WriteLine("Available Monitors and their Refresh Rates:");

            // Loop through the monitors again
            while (EnumDisplayDevices(null, devNum, ref d, 0))
            {
                Console.WriteLine($"\nMonitor {devNum + 1}: {d.DeviceName} - {d.DeviceString}");

                DEVMODE dm = new DEVMODE();
                dm.dmSize = (ushort)Marshal.SizeOf(dm);

                float maxRefreshRate = GetMaxRefreshRate(d.DeviceName);  // Example maximum refresh rate

                // Retrieve the current display settings for each monitor
                if (EnumDisplaySettings(d.DeviceName, -1, ref dm))
                {
                    float currentRate = (float)dm.dmDisplayFrequency;
                    
                    if (IsRefreshRateCloseEnough(currentRate, maxRefreshRate))
                    {
                        Console.WriteLine($"The monitor is already set to the target refresh rate: {maxRefreshRate}Hz.");
                    }
                    else
                    {
                        Console.WriteLine($"The current refresh rate is {currentRate}Hz, adjusting to {maxRefreshRate}Hz.");

                        // Set the max refresh rate
                        dm.dmDisplayFrequency = (uint)maxRefreshRate;

                        // Apply the change
                        int result = ChangeDisplaySettings(ref dm, CDS_UPDATEREGISTRY);

                        if (result == DISP_CHANGE_SUCCESSFUL)
                        {
                            Console.WriteLine($"Refresh rate successfully changed to {maxRefreshRate}Hz.");
                        }
                        else
                        {
                            Console.WriteLine("Failed to change the refresh rate.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Unable to retrieve display settings.");
                }
                

                devNum++;
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        // Helper function to get the maximum refresh rate
        static float GetMaxRefreshRate(string deviceName)
        {
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (ushort)Marshal.SizeOf(dm);

            int modeNum = 0;
            float maxRefreshRate = 0;

            // Iterate through all available modes for the monitor
            while (EnumDisplaySettings(deviceName, modeNum, ref dm))
            {
                if (dm.dmDisplayFrequency > maxRefreshRate)
                {
                    maxRefreshRate = dm.dmDisplayFrequency;  // Store the highest refresh rate
                }
                modeNum++;
            }

            Console.WriteLine($"Maximum refresh rate for {deviceName} is {maxRefreshRate}Hz.");
            Console.WriteLine();
            return maxRefreshRate;
        }

    }
}

