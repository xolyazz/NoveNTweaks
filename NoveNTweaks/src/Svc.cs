using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace NoveNTweaks;

// Port of the PowerShell Stop-Service / Set-Service helpers using the
// System.ServiceProcess.ServiceController (which wraps the SCM API).
public static class Svc
{
    public enum StartType { Automatic, Manual, Disabled }

    public static bool Stop(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            }
            return true;
        }
        catch { return false; }
    }

    public static bool Start(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            if (sc.Status != ServiceControllerStatus.Running)
            {
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            }
            return true;
        }
        catch { return false; }
    }

    public static bool SetStartup(string name, StartType type)
    {
        try
        {
            using var sc = new ServiceController(name);
            // ServiceController doesn't expose ChangeStartMode directly; use WMI or P/Invoke.
            // The simplest reliable way: use the sc.exe command-line.
            var mode = type switch
            {
                StartType.Automatic => "auto",
                StartType.Manual => "demand",
                StartType.Disabled => "disabled",
                _ => "demand",
            };
            return Run.Hidden($"sc.exe config \"{name}\" start= {mode}") == 0;
        }
        catch { return false; }
    }

    public static bool SetStart(string name, ServiceStartMode mode)
    {
        var startType = mode switch
        {
            ServiceStartMode.Disabled => StartType.Disabled,
            ServiceStartMode.Automatic => StartType.Automatic,
            _ => StartType.Manual,
        };
        if (mode == ServiceStartMode.Disabled) Stop(name);
        return SetStartup(name, startType);
    }

    public static bool StopAndDisable(string name)
    {
        Stop(name);
        return SetStartup(name, StartType.Disabled);
    }

    public static bool EnableAndStart(string name)
    {
        SetStartup(name, StartType.Automatic);
        return Start(name);
    }

    public static bool IsDisabled(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            if (sc.StartType == ServiceStartMode.Disabled) return true;
        }
        catch { }

        var regStart = Reg.GetDWord($@"HKLM:\SYSTEM\CurrentControlSet\Services\{name}", "Start");
        return regStart == 4;
    }

    // True if a service with the given name exists.
    public static bool Exists(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            var _ = sc.Status;
            return true;
        }
        catch { return false; }
    }
}
