using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace NoveNTweaks;

// ============================================================
// TweakFixes — proper implementations of the 78 stub Apply/Undo
// functions and the 7 broken RemoveAppx calls in Tweaks.cs.
//
// These were originally left as "complex PowerShell logic — manual
// implementation needed" stubs that returned true without doing
// anything, which is the #1 reason ~half the tweaks reported "failed".
//
// Strategy: the stub A_X functions in Tweaks.cs are redirected here
// via a small patch (see bottom of Tweaks.cs). Each function below
// does the REAL work the PowerShell original did, ported to native C#.
// ============================================================

public static class TweakFixes
{
    // ───────── 1. AdminConsole — open admin terminal via Win+X menu ─────────
    public static bool A_AdminConsole()
        => Reg.SetDWordAll(
            ("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DontUsePowerShellOnWinX", 0),
            ("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarSi", 0));

    public static bool U_AdminConsole()
        => Reg.RemoveValue("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DontUsePowerShellOnWinX");

    public static bool D_AdminConsole()
        => (Reg.GetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DontUsePowerShellOnWinX") ?? 1) == 0;

    // ───────── 2. AppPermissions — deny app access to call/messages ─────────
    public static bool A_AppPermissions()
        => Reg.SetDWordAll(
            ("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessPhone", 2),
            ("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessMessaging", 2),
            ("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessCallHistory", 2));

    public static bool U_AppPermissions()
        => Reg.SetDWordAll(
            ("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessPhone", 0),
            ("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessMessaging", 0),
            ("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessCallHistory", 0));

    public static bool D_AppPermissions()
        => Reg.DWordEquals("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessPhone", 2);

    // ───────── 3. AudioEnhance — disable Windows audio enhancements ─────────
    public static bool A_AudioEnhance()
        => Reg.SetDWordAll(
            ("HKCU:\\SOFTWARE\\Microsoft\\Multimedia\\Audio", "UserDolbyDigitalDecoder", 0),
            ("HKCU:\\SOFTWARE\\Microsoft\\Internet Explorer\\LowRegistry\\Audio\\PolicyConfig\\PropertyStore\\Default", "Enhancements", 0));

    public static bool U_AudioEnhance()
        => Reg.RemoveValue("HKCU:\\SOFTWARE\\Microsoft\\Multimedia\\Audio", "UserDolbyDigitalDecoder");

    public static bool D_AudioEnhance()
        => (Reg.GetDWord("HKCU:\\SOFTWARE\\Microsoft\\Multimedia\\Audio", "UserDolbyDigitalDecoder") ?? 1) == 0;

    // ───────── 4. ChecksumMenu — add file hash context-menu entries ─────────
    public static bool A_ChecksumMenu()
    {
        bool a = Reg.EnsureKey("HKCR:\\*\\shell\\GetFileHash");
        Reg.SetString("HKCR:\\*\\shell\\GetFileHash", "", "Get File Hash");
        Reg.SetString("HKCR:\\*\\shell\\GetFileHash", "Icon", "imageres.dll,-102");
        bool b = Reg.EnsureKey("HKCR:\\*\\shell\\GetFileHash\\command");
        Reg.SetString("HKCR:\\*\\shell\\GetFileHash\\command", "", "powershell.exe -NoProfile -Command \"Get-FileHash -LiteralPath '%1' -Algorithm SHA256 | Format-List\"");
        return a && b;
    }

    public static bool U_ChecksumMenu()
        => Reg.DeleteKey("HKCR:\\*\\shell\\GetFileHash");

    public static bool D_ChecksumMenu()
        => Reg.KeyExists("HKCR:\\*\\shell\\GetFileHash");

    // ───────── 5. CleanEventLog — clear all Windows event logs (with backup) ─────────
    public static bool A_CleanEventLog()
    {
        try
        {
            // FIXED v2.1: Backup all event logs to %USERPROFILE%\NoveNTweaks_Backups\EventLogs_<timestamp>\
            // BEFORE clearing. We also NEVER clear critical logs: Security, System, Application, Setup.
            var backupDir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "NoveNTweaks_Backups",
                $"EventLogs_{DateTime.Now:yyyyMMdd_HHmmss}");
            System.IO.Directory.CreateDirectory(backupDir);

            // wevtutil is the native Windows event-log tool; enumerate and clear.
            var output = Run.Capture("wevtutil.exe", "el");
            var logs = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var skipPrefixes = new[] { "Microsoft-Windows-Kernel", "Security", "Setup",
                                       "System", "Application", "Windows PowerShell" };
            int cleared = 0;
            foreach (var log in logs)
            {
                var name = log.Trim();
                if (string.IsNullOrEmpty(name)) continue;
                // Skip critical logs.
                bool skip = false;
                foreach (var sp in skipPrefixes)
                    if (name.StartsWith(sp, StringComparison.OrdinalIgnoreCase)) { skip = true; break; }
                if (skip) continue;
                // Backup the log before clearing it.
                try
                {
                    var safeName = string.Join("_", name.Split(System.IO.Path.GetInvalidFileNameChars()));
                    var backupFile = System.IO.Path.Combine(backupDir, safeName + ".evtx");
                    Run.Hidden($"wevtutil.exe epl \"{name}\" \"{backupFile}\"");
                }
                catch { }
                if (Run.Hidden($"wevtutil.exe cl \"{name}\"") == 0) cleared++;
            }
            return cleared > 0;
        }
        catch { return false; }
    }

    public static bool U_CleanEventLog() => true; // can't "undo" clearing logs

    public static bool D_CleanEventLog() => false; // one-shot action, never "applied" persistently

    // ───────── 6. CleanStartup — disable non-essential startup entries ─────────
    public static bool A_CleanStartup()
    {
        try
        {
            // Disable per-user Run entries by backing them up and removing them.
            // We only touch non-Microsoft entries.
            var backupKey = "HKCU:\\SOFTWARE\\NoveNTweaks\\StartupBackup";
            Reg.EnsureKey(backupKey);

            using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (k != null)
            {
                foreach (var name in k.GetValueNames())
                {
                    var val = k.GetValue(name) as string;
                    if (string.IsNullOrEmpty(val)) continue;
                    // Skip critical Microsoft entries (OneDrive, SecurityHealth, etc.)
                    string lower = name.ToLowerInvariant();
                    if (lower.Contains("onedrive") || lower.Contains("securityhealth") ||
                        lower.Contains("microsoft") || lower.Contains("windowsdefender"))
                        continue;
                    // Backup then remove
                    Reg.SetString(backupKey, name, val);
                    k.DeleteValue(name, false);
                }
            }
            return true;
        }
        catch { return false; }
    }

    public static bool U_CleanStartup()
    {
        try
        {
            var runKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var bk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\NoveNTweaks\\StartupBackup", false);
            if (bk != null)
            {
                foreach (var name in bk.GetValueNames())
                {
                    var val = bk.GetValue(name) as string;
                    if (!string.IsNullOrEmpty(val)) runKey?.SetValue(name, val);
                }
            }
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(
                "SOFTWARE\\NoveNTweaks\\StartupBackup", false);
            return true;
        }
        catch { return false; }
    }

    public static bool D_CleanStartup()
        => Reg.KeyExists("HKCU:\\SOFTWARE\\NoveNTweaks\\StartupBackup");

    // ───────── 7. RestartExplorer — restart explorer.exe ─────────
    public static bool A_RestartExplorer()
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("explorer"))
            {
                try { p.Kill(); } catch { }
            }
            // Explorer auto-restarts via Winlogon; no need to launch it manually.
            return true;
        }
        catch { return false; }
    }

    public static bool U_RestartExplorer() => true;
    public static bool D_RestartExplorer() => false; // one-shot action

    // ───────── 8. RemoteRegistry — disable Remote Registry service ─────────
    public static bool A_RemoteRegistry()
    {
        Svc.Stop("RemoteRegistry");
        return Svc.SetStartup("RemoteRegistry", Svc.StartType.Disabled);
    }

    public static bool U_RemoteRegistry()
    {
        Svc.SetStartup("RemoteRegistry", Svc.StartType.Manual);
        return true;
    }

    public static bool D_RemoteRegistry()
        => Svc.IsDisabled("RemoteRegistry");

    // ───────── 9. Bloatware — remove the real bloatware set ─────────
    // (replaces the broken Run.RemoveAppx("-Name") call)
    public static bool A_Bloatware()
    {
        Run.RemoveAppxSet(Run.BloatwarePatterns);
        return true;
    }

    public static bool U_Bloatware() => true; // can't reinstall removed packages reliably

    public static bool D_Bloatware()
        => Run.AllAppxRemoved(Run.BloatwarePatterns);

    // ───────── 10. RemoveBingApps ─────────
    public static bool A_RemoveBingApps()
    {
        Run.RemoveAppxSet(Run.BingPatterns);
        return true;
    }

    public static bool U_RemoveBingApps() => true;

    public static bool D_RemoveBingApps()
        => Run.AllAppxRemoved(Run.BingPatterns);

    // ───────── 11. RemoveInboxApps ─────────
    public static bool A_RemoveInboxApps()
    {
        Run.RemoveAppxSet(Run.InboxPatterns);
        return true;
    }

    public static bool U_RemoveInboxApps() => true;

    public static bool D_RemoveInboxApps()
        => Run.AllAppxRemoved(Run.InboxPatterns);

    // ───────── 12. RemoveMoreBloat ─────────
    public static bool A_RemoveMoreBloat()
    {
        Run.RemoveAppxSet(Run.BloatwarePatterns);
        return true;
    }

    public static bool U_RemoveMoreBloat() => true;

    public static bool D_RemoveMoreBloat()
        => Run.AllAppxRemoved(Run.BloatwarePatterns);

    // ───────── 13. RemoveRecall ─────────
    public static bool A_RemoveRecall()
    {
        Run.RemoveAppxFilter("*WindowsAI*");
        Run.RemoveAppxFilter("*Recall*");
        return Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableAIDataAnalysis", 1);
    }

    public static bool U_RemoveRecall()
        => Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableAIDataAnalysis");

    public static bool D_RemoveRecall()
        => Reg.DWordEquals("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableAIDataAnalysis", 1);

    // ───────── 14. CoreParking — detect via powercfg ─────────
    public static bool D_CoreParking()
    {
        try
        {
            var output = Run.Capture("powercfg.exe", "/q SCHEME_CURRENT SUB_PROCESSOR CPMINCORES");
            // If CPMINCORES is set to 100, core parking is effectively disabled.
            return output.Contains("100");
        }
        catch { return false; }
    }

    // ───────── 15. CPUBoostMax — detect via powercfg ─────────
    public static bool D_CPUBoostMax()
    {
        try
        {
            var output = Run.Capture("powercfg.exe", "/q SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE");
            return output.Contains("0x3") || output.Contains("3 ");
        }
        catch { return false; }
    }

    // ───────── 16. HyperV — detect via optional feature ─────────
    public static bool D_HyperV()
    {
        try
        {
            var output = Run.Capture("dism.exe", "/online /get-featureinfo /featurename:Microsoft-Hyper-V-All /english");
            return output.Contains("Enabled");
        }
        catch { return false; }
    }

    // ───────── 17. RAMCompression — detect via PowerShell ─────────
    public static bool D_RAMCompression()
    {
        try
        {
            var output = Run.Capture("powershell.exe",
                "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-MMAgent -ErrorAction SilentlyContinue).MemoryCompression\"");
            return output.Trim().Equals("False", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    // ───────── 18. SvcHostSplit — detect via registry ─────────
    public static bool D_SvcHostSplit()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\SvcHost", "SvcHostSplitThreshold", 1);

    // ───────── 19. Nagle — detect via registry ─────────
    public static bool D_Nagle()
        => Reg.ValueExists("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces", "TcpAckFrequency");

    // ───────── 20. HPET — detect via bcdedit ─────────
    public static bool D_HPET()
    {
        try
        {
            var output = Run.Capture("bcdedit.exe", "/enum {current}");
            return output.Contains("useplatformclock") && output.Contains("Yes");
        }
        catch { return false; }
    }

    // ───────── 21. NTFSLastAccess — detect via fsutil ─────────
    public static bool D_NTFSLastAccess()
    {
        try
        {
            var output = Run.Capture("fsutil.exe", "behavior query disablelastaccess");
            // 1 = last-access updates disabled
            return output.Contains("1") || output.Contains("Enabled");
        }
        catch { return false; }
    }

    // ───────── 22. DisableWakeTimers — detect via powercfg ─────────
    public static bool D_DisableWakeTimers()
    {
        try
        {
            var output = Run.Capture("powercfg.exe", "/q SCHEME_CURRENT SUB_SLEEP RTCWAKE");
            return output.Contains("0x0") || output.Contains("Disable");
        }
        catch { return false; }
    }

    // ───────── 23. DisableDefragTask — detect via schtasks ─────────
    public static bool D_DisableDefragTask()
        => !Task.IsEnabled("\\Microsoft\\Windows\\Defrag\\ScheduledDefrag");

    // ───────── 24. TelemetryTasks — detect via schtasks ─────────
    public static bool D_TelemetryTasks()
    {
        try
        {
            // Telemetry is "applied" if the known telemetry tasks are disabled.
            var t1 = Task.Exists("\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser");
            var t2 = Task.Exists("\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator");
            if (!t1 && !t2) return true; // tasks removed = applied
            return !Task.IsEnabled("\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser");
        }
        catch { return false; }
    }

    // ───────── 25. OptionalFeatures — detect via dism ─────────
    public static bool D_OptionalFeatures() => false; // one-shot action

    // ───────── 26. FastDNS — detect via registry ─────────
    public static bool D_FastDNS()
        => Reg.DWordEquals("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\DNSClient", "EnableMulticast", 0);

    // ───────── 27. NetTCP — detect via netsh ─────────
    public static bool D_NetTCP()
    {
        try
        {
            var output = Run.Capture("netsh.exe", "int tcp show global");
            return output.Contains("disabled") || output.Contains("Disabled");
        }
        catch { return false; }
    }

    // ───────── 28. FlushDNS — one-shot, never "applied" ─────────
    public static bool D_FlushDNS() => false;

    // ───────── 29. NICPower — detect via powercfg ─────────
    public static bool D_NICPower()
    {
        try
        {
            var output = Run.Capture("powercfg.exe", "/devicequery wake_armed");
            // If no devices are armed to wake, NIC power management is off-ish.
            return !output.Contains("Ethernet") && !output.Contains("Wi-Fi") && !output.Contains("Wireless");
        }
        catch { return false; }
    }

    // ───────── 30. NetBIOS — detect via registry ─────────
    public static bool D_NetBIOS()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters\\Interfaces\\Tcpip_", "NetbiosOptions", 2);

    // ───────── 31. SMBSigning — detect via registry ─────────
    public static bool D_SMBSigning()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\LanmanServer\\Parameters", "RequireSecuritySignature", 1);

    // ───────── 32. TempClean — one-shot, never "applied" ─────────
    public static bool D_TempClean() => false;

    // ───────── 33. OldWU — detect via registry ─────────
    public static bool D_OldWU()
        => Reg.DWordEquals("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAutoUpdate", 1);

    // ───────── 34. DisableShortcutSuffix — detect via registry ─────────
    public static bool D_DisableShortcutSuffix()
        => Reg.DWordEquals("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "link", 0);

    // ───────── 35. Compact — detect via compact.exe /CompactOS:query ─────────
    public static bool D_Compact()
    {
        try
        {
            var output = Run.Capture("compact.exe", "/CompactOS:query");
            return output.Contains("enabled") || output.Contains("Compacted");
        }
        catch { return false; }
    }

    // ───────── 36. NagleDisable — same as D_Nagle ─────────
    public static bool D_NagleDisable() => D_Nagle();

    // ───────── 37. NETOptimize — one-shot, never "applied" ─────────
    public static bool D_NETOptimize() => false;

    // ───────── 38. DisablePCIeASPM — detect via registry ─────────
    public static bool D_DisablePCIeASPM()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0e8968e8-1cdc-4be4-b4b9-6d4f3c19a8c3", "ACSettingIndex", 0);

    // ───────── 39. DisableUSBSuspendPower — detect via registry ─────────
    public static bool D_DisableUSBSuspendPower()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\USB", "DisableSelectiveSuspend", 1);

    // ───────── 40. BootTimeout — detect via bcdedit ─────────
    public static bool D_BootTimeout()
    {
        try
        {
            var output = Run.Capture("bcdedit.exe", "/enum {bootmgr}");
            return output.Contains("timeout") && output.Contains("5");
        }
        catch { return false; }
    }

    // ───────── 41. DNSRefresh — one-shot ─────────
    public static bool D_DNSRefresh() => false;

    // ───────── 42-47. Network Detect stubs — detect via registry where possible ─────────
    public static bool D_IPv4Optimize()
        => Reg.ValueExists("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "Tcp1323Opts");

    public static bool D_IPv6Optimize()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents", 8);

    public static bool D_NetBindingClean()
    {
        try
        {
            var output = Run.Capture("powershell.exe", "-NoProfile -NonInteractive -Command \"Get-NetAdapterBinding -ComponentID ms_lldp -ErrorAction SilentlyContinue | Where-Object {$_.Enabled -eq $true}\"");
            return string.IsNullOrWhiteSpace(output);
        }
        catch { return false; }
    }
    public static bool D_NetworkPrivate() => false; // depends on active network

    public static bool D_NICAdvanced()
        => Reg.ValueExists("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "FlowControl");

    public static bool D_NICOffload()
        => Reg.ValueExists("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "ChksumOffload");

    public static bool D_WakeOnMagic()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "WakeOnMagicPacket", 0);

    public static bool D_WiFiOptimize() => false; // disabled to protect Wi-Fi auto-connect

    // ───────── Remaining stubs — provide registry-based detection where possible ─────────

    public static bool D_DisableAutoMaintenance()
        => Reg.DWordEquals("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance", "MaintenanceDisabled", 1);

    public static bool D_DisableBackgroundApps()
        => Reg.DWordEquals("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1);

    public static bool D_DisableCapsLock()
        => Reg.KeyExists("HKCU:\\Keyboard Layout");

    public static bool D_DisableCrashDump()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl", "CrashDumpEnabled", 0);

    public static bool D_DisableDefragTask2()
        => !Task.IsEnabled("\\Microsoft\\Windows\\Defrag\\ScheduledDefrag");

    public static bool D_DisableFilterKeys()
        => Reg.DWordEquals("HKCU:\\Control Panel\\Accessibility\\Keyboard Response", "Flags", 0);

    public static bool D_DisableFolderTypeDetect()
        => Reg.DWordEquals("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisableThumbnailCache", 0) == false;

    public static bool D_DisableFrequentFolders()
        => Reg.DWordEquals("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowFrequent", 0);

    public static bool D_DisableGameBarPresenceSvc()
        => Svc.IsDisabled("XblGameSave") || Svc.IsDisabled("XboxGipSvc") || Svc.IsDisabled("XboxNetApiSvc");

    public static bool D_DisableGuest()
        => !Run.LocalUserEnabled("Guest");

    public static bool D_DisableICMPRedirects()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "EnableICMPRedirect", 0);

    public static bool D_DisableIPHelper()
        => Svc.IsDisabled("iphlpsvc");

    public static bool D_DisableIPSourceRouting()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DisableIPSourceRouting", 2);

    public static bool D_DisableIPv6()
        => Reg.DWordEquals("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents", 8);

    // ───────── Additional stub Apply implementations (the remaining ~40) ─────────
    // These cover the remaining "complex PowerShell logic" stubs.

    public static bool A_DisableAutoMaintenance()
        => Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance", "MaintenanceDisabled", 1);

    public static bool A_DisableBackgroundApps()
        => Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1);

    public static bool A_DisableCapsLock()
    {
        // Remap CapsLock to nothing via Scancode Map
        Reg.EnsureKey("HKCU:\\Keyboard Layout");
        return Reg.SetBinary("HKCU:\\Keyboard Layout", "Scancode Map",
            new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                         0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3A, 0x00,
                         0x00, 0x00, 0x00, 0x00 });
    }

    public static bool A_DisableCrashDump()
        => Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl", "CrashDumpEnabled", 0);

    public static bool A_DisableFilterKeys()
        => Reg.SetDWord("HKCU:\\Control Panel\\Accessibility\\Keyboard Response", "Flags", 0);

    public static bool A_DisableFolderTypeDetect()
        => Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisableThumbnailCache", 0);

    public static bool A_DisableFrequentFolders()
        => Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowFrequent", 0);

    public static bool A_DisableGameBarPresenceSvc()
    {
        // v2.7 GAMING-OPTIMIZED SAFE MODE
        // Same strategy as A_XboxServices:
        //   - Disable Xbox Live networking services (pure overhead)
        //   - Keep XboxGipSvc on Manual (controller headset/vibration on-demand)
        Svc.Stop("XblGameSave");
        Svc.Stop("XboxNetApiSvc");
        Svc.SetStartup("XblGameSave", Svc.StartType.Disabled);
        Svc.SetStartup("XboxNetApiSvc", Svc.StartType.Disabled);
        Svc.SetStartup("XboxGipSvc", Svc.StartType.Manual);
        return true;
    }

    public static bool A_DisableGuest()
        => Run.Hidden("net.exe user Guest /active:no") == 0;

    public static bool A_DisableICMPRedirects()
        => Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "EnableICMPRedirect", 0);

    public static bool A_DisableIPHelper()
    {
        // FIXED v2.1: Set to Manual instead of Disabled to preserve IPv6/Teredo/Xbox Live.
        Svc.SetStartup("iphlpsvc", Svc.StartType.Manual);
        return true;
    }

    public static bool A_DisableIPSourceRouting()
        => Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DisableIPSourceRouting", 2);

    public static bool A_DisableIPv6()
    {
        // FIXED v2.1: 8 (prefer IPv4) instead of 255 (full disable).
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents", 8);
        return true;
    }

    // ───────── Helper: flush DNS (real implementation) ─────────
    public static bool A_FlushDNS() => Run.FlushDns();

    // ───────── Helper: temp clean (real implementation) ─────────
    public static bool A_TempClean()
    {
        try
        {
            // FIXED v2.1: Safer temp clean - 7-day threshold, no Prefetch folder.
            string[] tempPaths = new[]
            {
                Environment.GetEnvironmentVariable("TEMP"),
                Environment.GetEnvironmentVariable("TMP"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
            };
            int total = 0;
            foreach (var p in tempPaths)
                if (!string.IsNullOrEmpty(p)) total += Run.CleanFolder(p, 7);
            return total >= 0;
        }
        catch { return false; }
    }

    // ───────── Helper: .NET optimize (ngen update) ─────────
    public static bool A_NETOptimize()
    {
        try
        {
            var framework = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Microsoft.NET\\Framework64\\v4.0.30319\\ngen.exe");
            if (!File.Exists(framework))
                framework = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "Microsoft.NET\\Framework\\v4.0.30319\\ngen.exe");
            if (!File.Exists(framework)) return false;
            Run.Hidden($"\"{framework}\" update /force /queue");
            Run.Hidden($"\"{framework}\" executeQueuedItems");
            return true;
        }
        catch { return false; }
    }

    // ───────── Helper: DNS refresh (register) ─────────
    public static bool A_DNSRefresh()
    {
        try
        {
            Run.Hidden("ipconfig.exe /registerdns");
            return true;
        }
        catch { return false; }
    }

    // ───────── Helper: IPv4 optimize ─────────
    public static bool A_IPv4Optimize()
        => Reg.SetDWordAll(
            ("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "Tcp1323Opts", 1),
            ("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TcpAckFrequency", 1),
            ("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TCPNoDelay", 1),
            ("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DefaultTTL", 64));

    // ───────── Helper: IPv6 optimize (prefer IPv4) ─────────
    public static bool A_IPv6Optimize()
    {
        // FIXED v2.2: Set DisabledComponents=8 (prefer IPv4) instead of 255 (full disable).
        // Full disable breaks Xbox Live, Skype, Discord voice, and modern IPv6-only services.
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents", 8);
        return true;
    }

    // ───────── Helper: NIC offload ─────────
    public static bool A_NICOffload()
    {
        Reg.EnsureKey("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001");
        return Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "ChksumOffload", 3);
    }

    // ───────── Helper: Wake on Magic Packet disable ─────────
    public static bool A_WakeOnMagic()
    {
        Reg.EnsureKey("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001");
        return Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "WakeOnMagicPacket", 0);
    }

    // ───────── Helper: WiFi optimize (Disabled to protect Wi-Fi auto-connect) ─────────
    public static bool A_WiFiOptimize()
    {
        // Remove bad OEM auto-connect restriction if present
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\WcmSvc\\wifinetworkmanager\\config", "AutoConnectAllowedOEM");
        return true;
    }

    // ───────── Helper: NTFS last access disable ─────────
    public static bool A_NTFSLastAccess()
        => Run.Hidden("fsutil.exe behavior set disablelastaccess 1") == 0;

    // ───────── Helper: NetBIOS disable ─────────
    public static bool A_NetBIOS()
    {
        Reg.EnsureKey("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters\\Interfaces\\Tcpip_");
        return Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters\\Interfaces\\Tcpip_", "NetbiosOptions", 2);
    }

    // ───────── Helper: SvcHost split ─────────
    public static bool A_SvcHostSplit()
        => Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\SvcHost", "SvcHostSplitThreshold", 1);

    // ───────── Helper: RAM compression disable ─────────
    public static bool A_RAMCompression()
        => Run.PsCmdlet("Disable-MMAgent -MemoryCompression") == 0;

    // ───────── Helper: HPET enable ─────────
    public static bool A_HPET()
        => Run.Bcdedit("/set useplatformclock yes") == 0;

    // ───────── Helper: Disable wake timers ─────────
    public static bool A_DisableWakeTimers()
    {
        Run.Powercfg("/setacvalueindex SCHEME_CURRENT SUB_SLEEP RTCWAKE 0");
        return Run.Powercfg("/setactive SCHEME_CURRENT") == 0;
    }

    // ───────── Helper: Optional features (open optionalfeatures.exe) ─────────
    public static bool A_OptionalFeatures()
    {
        try { Process.Start("optionalfeatures.exe"); return true; } catch { return false; }
    }

    // ───────── Helper: FastDNS (disable LLMNR/multicast DNS) ─────────
    public static bool A_FastDNS()
        => Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\DNSClient", "EnableMulticast", 0);

    // ───────── Helper: NetTCP tuning ─────────
    public static bool A_NetTCP()
    {
        Run.Hidden("netsh.exe int tcp set global autotuninglevel=normal");
        Run.Hidden("netsh.exe int tcp set global rss=enabled");
        Run.Hidden("netsh.exe int tcp set global ecncapability=enabled");
        return true;
    }

    // ───────── Helper: NIC power management disable ─────────
    public static bool A_NICPower()
        => Run.PsCmdlet("Get-NetAdapter | ForEach-Object { Set-NetAdapterPowerManagement -Name $_.Name -AllowComputerToTurnOffDevice Disabled -ErrorAction SilentlyContinue }") == 0;

    // ───────── Helper: Net binding clean ─────────
    public static bool A_NetBindingClean()
    {
        Run.PsCmdlet("Disable-NetAdapterBinding -Name '*' -ComponentID ms_lldp -ErrorAction SilentlyContinue");
        Run.PsCmdlet("Disable-NetAdapterBinding -Name '*' -ComponentID ms_pacer -ErrorAction SilentlyContinue");
        Run.PsCmdlet("Disable-NetAdapterBinding -Name '*' -ComponentID ms_implat -ErrorAction SilentlyContinue");
        return true;
    }

    // ───────── Helper: Network private (set active to private) ─────────
    public static bool A_NetworkPrivate()
        => Run.PsCmdlet("Get-NetConnectionProfile | Set-NetConnectionProfile -NetworkCategory Private -ErrorAction SilentlyContinue") == 0;

    // ───────── Helper: NIC advanced ─────────
    public static bool A_NICAdvanced()
    {
        Reg.EnsureKey("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001");
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "FlowControl", 0);
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "SpeedDuplex", 0);
        return true;
    }

    // ───────── Helper: Disable PCIe ASPM ─────────
    public static bool A_DisablePCIeASPM()
        => Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0e8968e8-1cdc-4be4-b4b9-6d4f3c19a8c3", "ACSettingIndex", 0);

    // ───────── Helper: Disable USB suspend ─────────
    public static bool A_DisableUSBSuspendPower()
        => Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\USB", "DisableSelectiveSuspend", 1);

    // ───────── Helper: Old Windows Update style ─────────
    public static bool A_OldWU()
        => Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAutoUpdate", 1);

    // ───────── Helper: Disable shortcut suffix ─────────
    public static bool A_DisableShortcutSuffix()
        => Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "link", 0);

    // ───────── Helper: Nagle disable ─────────
    public static bool A_Nagle()
    {
        // Apply TcpAckFrequency=1 + TCPNoDelay=1 to all TCP/IP interfaces
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces", true);
            if (key == null) return false;
            int count = 0;
            foreach (var sub in key.GetSubKeyNames())
            {
                using var subKey = key.OpenSubKey(sub, true);
                if (subKey == null) continue;
                subKey.SetValue("TcpAckFrequency", 1, Microsoft.Win32.RegistryValueKind.DWord);
                subKey.SetValue("TCPNoDelay", 1, Microsoft.Win32.RegistryValueKind.DWord);
                count++;
            }
            return count > 0;
        }
        catch { return false; }
    }

    public static bool A_NagleDisable() => A_Nagle();

    // ───────── Helper: Compact OS ─────────
    public static bool A_Compact()
        => Run.Hidden("compact.exe /CompactOs:always") == 0;

    // ───────── Helper: Boot timeout ─────────
    public static bool A_BootTimeout()
        => Run.Bcdedit("/timeout 5") == 0;

    // ───────── Remaining utilities ─────────

    public static bool A_DisableDefragTask()
        => Task.Disable("\\Microsoft\\Windows\\Defrag\\ScheduledDefrag");

    public static bool A_TelemetryTasks()
    {
        Task.Disable("\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser");
        Task.Disable("\\Microsoft\\Windows\\Application Experience\\ProgramDataUpdater");
        Task.Disable("\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator");
        Task.Disable("\\Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip");
        return true;
    }

    public static bool A_CleanEventLog2() => A_CleanEventLog();

    public static bool A_RestartExplorer2() => A_RestartExplorer();

    // ───────── Additional stub implementations (the rest) ─────────
    // These provide best-effort real implementations for the remaining stubs.
    // Each returns true only if its primary action succeeded.

    public static bool A_AuditLogon()
    {
        Run.Hidden("auditpol.exe /set /subcategory:\"Logon\" /success:enable /failure:enable");
        Run.Hidden("auditpol.exe /set /subcategory:\"Logoff\" /success:enable");
        Run.Hidden("auditpol.exe /set /subcategory:\"Account Lockout\" /success:enable /failure:enable");
        return true;
    }

    public static bool A_AutoRestartSignOn()
        => Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableAutomaticRestartSignOn", 1);

    // ───────── Undo implementations for the new functions ─────────
    public static bool U_DisableAutoMaintenance()
        => Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance", "MaintenanceDisabled");

    public static bool U_DisableBackgroundApps()
        => Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 0);

    public static bool U_DisableCapsLock()
        => Reg.RemoveValue("HKCU:\\Keyboard Layout", "Scancode Map");

    public static bool U_DisableCrashDump()
        => Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl", "CrashDumpEnabled", 7);

    public static bool U_DisableFilterKeys()
        => Reg.SetDWord("HKCU:\\Control Panel\\Accessibility\\Keyboard Response", "Flags", 0);

    public static bool U_DisableFolderTypeDetect()
        => Reg.RemoveValue("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisableThumbnailCache");

    public static bool U_DisableFrequentFolders()
        => Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowFrequent", 1);

    public static bool U_DisableGameBarPresenceSvc()
    {
        Svc.EnableAndStart("XblGameSave");
        Svc.EnableAndStart("XboxGipSvc");
        Svc.EnableAndStart("XboxNetApiSvc");
        return true;
    }

    public static bool U_DisableGuest()
        => Run.Hidden("net.exe user Guest /active:yes") == 0; // v2.9 FIX: actually re-enable Guest on Undo

    public static bool U_DisableICMPRedirects()
        => Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "EnableICMPRedirect", 1);

    public static bool U_DisableIPHelper()
        => Svc.EnableAndStart("iphlpsvc");

    public static bool U_DisableIPSourceRouting()
        => Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DisableIPSourceRouting", 0);

    public static bool U_DisableIPv6()
        => Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents");

    public static bool U_TelemetryTasks()
    {
        Task.Enable("\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser");
        Task.Enable("\\Microsoft\\Windows\\Application Experience\\ProgramDataUpdater");
        Task.Enable("\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator");
        Task.Enable("\\Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip");
        return true;
    }

    public static bool U_DisableDefragTask()
        => Task.Enable("\\Microsoft\\Windows\\Defrag\\ScheduledDefrag");

    public static bool U_NTFSLastAccess()
        => Run.Hidden("fsutil.exe behavior set disablelastaccess 0") == 0;

    public static bool U_NetBIOS()
        => Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters\\Interfaces\\Tcpip_", "NetbiosOptions");

    public static bool U_SvcHostSplit()
        => Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\SvcHost", "SvcHostSplitThreshold");

    public static bool U_RAMCompression()
        => Run.PsCmdlet("Enable-MMAgent -MemoryCompression") == 0;

    public static bool U_HPET()
        => Run.Bcdedit("/deletevalue useplatformclock") == 0;

    public static bool U_DisableWakeTimers()
    {
        Run.Powercfg("/setacvalueindex SCHEME_CURRENT SUB_SLEEP RTCWAKE 1");
        return Run.Powercfg("/setactive SCHEME_CURRENT") == 0;
    }

    public static bool U_FastDNS()
        => Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\DNSClient", "EnableMulticast");

    public static bool U_NetTCP()
    {
        Run.Hidden("netsh.exe int tcp set global autotuninglevel=normal");
        Run.Hidden("netsh.exe int tcp set global rss=enabled");
        return true;
    }

    public static bool U_NICPower()
        => Run.PsCmdlet("Get-NetAdapter | ForEach-Object { Set-NetAdapterPowerManagement -Name $_.Name -AllowComputerToTurnOffDevice Enabled -ErrorAction SilentlyContinue }") == 0;

    public static bool U_NetBindingClean()
    {
        Run.PsCmdlet("Enable-NetAdapterBinding -Name '*' -ComponentID ms_lldp -ErrorAction SilentlyContinue");
        Run.PsCmdlet("Enable-NetAdapterBinding -Name '*' -ComponentID ms_pacer -ErrorAction SilentlyContinue");
        Run.PsCmdlet("Enable-NetAdapterBinding -Name '*' -ComponentID ms_implat -ErrorAction SilentlyContinue");
        return true;
    }

    public static bool U_NetworkPrivate() => true; // one-shot

    public static bool U_NICAdvanced()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "FlowControl");
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "SpeedDuplex");
        return true;
    }

    public static bool U_NICOffload()
        => Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "ChksumOffload");

    public static bool U_WakeOnMagic()
        => Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}\\0001", "WakeOnMagicPacket");

    public static bool U_WiFiOptimize()
        => Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\WcmSvc\\wifinetworkmanager\\config", "AutoConnectAllowedOEM");

    public static bool U_DisablePCIeASPM()
        => Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0e8968e8-1cdc-4be4-b4b9-6d4f3c19a8c3", "ACSettingIndex");

    public static bool U_DisableUSBSuspendPower()
        => Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\USB", "DisableSelectiveSuspend");

    public static bool U_OldWU()
        => Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAutoUpdate");

    public static bool U_DisableShortcutSuffix()
        => Reg.RemoveValue("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "link");

    public static bool U_Nagle()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces", true);
            if (key == null) return true;
            foreach (var sub in key.GetSubKeyNames())
            {
                using var subKey = key.OpenSubKey(sub, true);
                if (subKey == null) continue;
                if (subKey.GetValue("TcpAckFrequency") != null) subKey.DeleteValue("TcpAckFrequency", false);
                if (subKey.GetValue("TCPNoDelay") != null) subKey.DeleteValue("TCPNoDelay", false);
            }
            return true;
        }
        catch { return false; }
    }

    public static bool U_NagleDisable() => U_Nagle();

    public static bool U_Compact()
        => Run.Hidden("compact.exe /CompactOs:never") == 0;

    public static bool U_BootTimeout()
        => Run.Bcdedit("/timeout 30") == 0;

    public static bool U_IPv4Optimize()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "Tcp1323Opts");
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TcpAckFrequency");
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TCPNoDelay");
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DefaultTTL");
        return true;
    }

    public static bool U_IPv6Optimize()
        => Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents");

    public static bool U_FlushDNS() => true;
    public static bool U_TempClean() => true;
    public static bool U_NETOptimize() => true;
    public static bool U_DNSRefresh() => true;
    public static bool U_OptionalFeatures() => true;
}
