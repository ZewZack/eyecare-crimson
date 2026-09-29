using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace eyecarebyzewzack.Services;

public static class Win32Helper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRegistryName = "EyeCareByZewZack";

    /// <summary>
    /// Gets how many seconds the user has been idle (no keyboard or mouse movement).
    /// </summary>
    public static int GetIdleTimeSeconds()
    {
        LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

        if (GetLastInputInfo(ref lastInputInfo))
        {
            uint currentTick = (uint)Environment.TickCount;
            uint idleTicks = currentTick - lastInputInfo.dwTime;
            return (int)(idleTicks / 1000);
        }

        return 0;
    }

    /// <summary>
    /// Trims memory pages to minimal working set footprint (~10-20MB).
    /// </summary>
    public static void TrimWorkingSet()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            EmptyWorkingSet(process.Handle);
            SetProcessWorkingSetSize(process.Handle, (IntPtr)(-1), (IntPtr)(-1));
        }
        catch
        {
            // Best effort
        }
    }

    /// <summary>
    /// Enables or disables automatic startup with Windows.
    /// </summary>
    public static void SetStartupWithWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppRegistryName, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(AppRegistryName, false);
            }
        }
        catch
        {
            // Registry write failed or restricted
        }
    }

    public static bool IsStartupWithWindowsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            if (key != null)
            {
                return key.GetValue(AppRegistryName) != null;
            }
        }
        catch
        {
            // Best effort
        }
        return false;
    }
}
