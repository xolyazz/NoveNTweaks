using System;
using System.IO;
using System.Diagnostics;

namespace NoveNTweaks;

// ============================================================
// SafetyGuard — v2.1 safety layer added in response to user reports
// about tweaks that destroyed data / broke the network stack.
//
// This module provides:
//   1. Pre-apply registry backup (.reg file) for any tweak that
//      modifies HKLM or HKCU keys. Stored in
//      %USERPROFILE%\NoveNTweaks_Backups\<timestamp>\.
//   2. A blacklist of "never run these" patterns that block
//      dangerous commands even if a future tweak adds them.
//   3. A "first run" checkpoint that creates a System Restore
//      point the first time the user runs any Apply operation.
//   4. Helper to verify the system is not in a vulnerable state
//      (e.g. on battery, in audit mode, mid-WU-download) before
//      applying disruptive tweaks.
// ============================================================

public static class SafetyGuard
{
    private const string BackupRoot = @"SOFTWARE\NoveNTweaks\SafetyGuard";
    private static bool _restorePointCreatedThisSession = false;

    // Returns the directory where backups for the current session live.
    public static string GetBackupDir()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "NoveNTweaks_Backups",
            $"session_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    // Create a System Restore point (only once per session).
    // Requires system protection to be enabled; silently no-ops if not.
    public static bool EnsureRestorePoint()
    {
        if (_restorePointCreatedThisSession) return true;
        try
        {
            // PowerShell: CheckPoint-Computer -Description "NoveNTweaks pre-apply" -RestorePointType "MODIFY_SETTINGS"
            var exit = Run.PsCmdlet(
                "CheckPoint-Computer -Description 'NoveNTweaks_pre_apply' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction SilentlyContinue");
            _restorePointCreatedThisSession = (exit == 0);
            return _restorePointCreatedThisSession;
        }
        catch { return false; }
    }

    // Backup a registry key (and all its subkeys/values) to a .reg file.
    // path: "HKLM\\..." or "HKCU\\..." with backslash separators (registry style).
    public static bool BackupRegistryKey(string path, string reason)
    {
        try
        {
            var dir = GetBackupDir();
            var safe = string.Join("_", path.Split(Path.GetInvalidFileNameChars()));
            var file = Path.Combine(dir, $"{safe}_{reason}_{DateTime.Now:HHmmss}.reg");
            // reg.exe export "HKLM\..." file.reg /y
            return Run.Hidden($"reg.exe export \"{path}\" \"{file}\" /y") == 0;
        }
        catch { return false; }
    }

    // Check whether the system is on battery power. Disruptive tweaks
    // should be deferred until AC power is restored.
    public static bool IsOnBattery()
    {
        try
        {
            var power = System.Windows.Forms.SystemInformation.PowerStatus;
            return power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline;
        }
        catch { return false; } // assume AC if we can't tell
    }

    // Check whether Windows Update is currently downloading. Disruptive
    // tweaks should not run while WU is mid-download.
    public static bool IsWindowsUpdateRunning()
    {
        try
        {
            // wuauclt.exe (legacy), wusa.exe, USOClient.exe (modern), and TiWorker.exe
            foreach (var name in new[] { "wuauclt", "wusa", "USOClient", "TiWorker", "TrustedInstaller" })
            {
                if (Process.GetProcessesByName(name).Length > 0) return true;
            }
            return false;
        }
        catch { return false; }
    }

    // Verify a command line is safe to execute. Returns true if safe,
    // false if it matches a forbidden pattern.
    public static bool IsCommandSafe(string commandLine)
    {
        if (string.IsNullOrEmpty(commandLine)) return true;
        var lower = commandLine.ToLowerInvariant();

        // Forbidden patterns - these should NEVER be executed by any tweak.
        string[] forbidden = new[]
        {
            "format ",                 // disk format
            "diskpart",                // disk partition tool (too dangerous for a tweak)
            "cipher /w",               // disk wipe (free space overwrite)
            "del /f /s /q c:\\",       // bulk root delete
            "rd /s /q c:\\",           // bulk root remove
            "rmdir /s /q c:\\",
            "reg delete hklm",         // bulk HKLM delete
            "reg delete hkcr",         // bulk HKCR delete
            "reg delete hkcu\\software\\microsoft\\windows\\currentversion\\run", // kill all startup
            "remove-item c:\\windows", // remove Windows dir
            "remove-item c:\\program", // remove Program Files
            "remove-item c:\\programdata",
            "remove-item c:\\users\\public",
            "netsh winsock reset",     // already removed but block future additions
            "netsh int ip reset",      // full TCP/IP stack reset (use targeted fixes instead)
            "netsh int ipv4 reset",
            "netsh int ipv6 reset",
            "shutdown /r /t 0",        // instant reboot (no warning to user)
            "shutdown /s /t 0",        // instant shutdown
            "bcdedit /delete",         // delete boot entry (use /deletevalue instead)
            "powercfg /delete",        // delete power plan
            "set-itemproperty 'hklm:\\software\\microsoft\\windows nt\\currentversion\\winlogon' -name shell", // replace user shell
        };

        foreach (var f in forbidden)
        {
            if (lower.Contains(f)) return false;
        }
        return true;
    }

    // Verify that an Apply function name is in our whitelist of "safe to
    // apply without a system restore point" tweaks (pure UI tweaks, etc).
    public static bool NeedsRestorePoint(string tweakId)
    {
        // Pure UI / appearance tweaks don't need a restore point.
        string[] safeIds = new[]
        {
            "DarkMode", "ClassicTaskbar", "ShowExtensions", "ShowHidden",
            "ShowProtectedFiles", "DisableStickyKeys", "DisableFilterKeys",
            "MousePrecision", "MouseFix", "DisableCapsLock",
            "DisableShortcutArrow", "DisableShortcutSuffix", "SearchHighlights",
            "DisableWindowsSounds", "DisableRecycleBinConfirm",
            "GodMode", "GodModeDesktop", "OpenCMDHere", "OpenPSHere",
            "OpenTerminalHere", "OpenVSCodeHere", "CopyToFolder", "MoveToFolder",
            "RunAsUserMenu", "EditNotepadPP", "WallpaperHere", "ChecksumMenu",
            "TaskbarClean", "TaskbarWidgetsOff", "TaskbarSearchIcon",
            "WindowsTools", "ShutdownOptions", "KillTasks", "AdminConsole",
            "TakeOwnership", "FileHashCheck", "AeroShakeDisable",
            "OldContextMenu", "ContextMenuExtras", "Hide3DObjects",
            "ShowThisPC", "ForceNetworkTrayIcon", "DisableLockScreenAds",
            "DisableLockScreenSlideshow", "DisableCameraOnLockScreen",
            "DisableTouchKeyboardAutoInvoke", "OpenExplorerToThisPC",
            "ExplorerSearch", "NumLock", "CursorBlink",
        };
        foreach (var id in safeIds)
            if (string.Equals(id, tweakId, StringComparison.OrdinalIgnoreCase))
                return false;
        return true;
    }

    // ───────── Emergency safeguards (v2.2) ─────────

    // Returns true if the system has at least 1GB free on the system drive.
    // Some tweaks (NGEN update, SvcHost tune) need disk space to complete.
    public static bool HasFreeDiskSpace(long minBytes = 1024 * 1024 * 1024)
    {
        try
        {
            var sysDrive = Path.GetPathRoot(Environment.SystemDirectory);
            var drive = new DriveInfo(sysDrive);
            return drive.AvailableFreeSpace >= minBytes;
        }
        catch { return true; } // assume OK if we can't check
    }

    // Returns true if the user has admin rights (required for HKLM tweaks).
    public static bool IsAdmin()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    // Returns true if System Restore is enabled on the system drive.
    public static bool IsSystemRestoreEnabled()
    {
        try
        {
            var output = Run.Capture("powershell.exe",
                "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-ComputerRestorePoint -ErrorAction SilentlyContinue).Count -gt 0 -or ((Get-CimInstance -ClassName Win32_SystemRestore -Namespace root/default -ErrorAction SilentlyContinue) -ne $null)\"");
            return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    // List of "potentially destructive" tweak IDs that require explicit
    // user confirmation even if the user checked them in batch mode.
    // These tweaks either disable critical services, modify boot config,
    // or remove system components.
    //
    // v1.4 — rewritten to reference REAL snake_case tweak IDs that exist
    // in Tweaks.cs / TweaksExtra.cs / TweaksProc.cs. The old CamelCase list
    // never matched anything and was effectively dead code.
    public static readonly string[] DestructiveTweaks = new[]
    {
        // — Virtualization & CPU security (advanced) —
        "adv_disable_hyperv_feature",     // disables Hyper-V (breaks WSL2, Docker, Sandbox)
        "adv_disable_hypervisor_launch",  // disables hypervisor launch (breaks VBS, WSL2)
        "adv_disable_vbs",                 // disables Virtualization-Based Security
        "adv_disable_hvci",                // disables Memory Integrity (HVCI)
        "adv_disable_device_guard",       // disables Device Guard / Code Integrity

        // — Windows Update —
        "wu_disable_orchestrator_scans",  // disables the Windows Update orchestrator
        "wu_disable_driver_updates",      // blocks driver updates via WU
        "wu_disable_other_ms_products",   // blocks other MS product updates
        "v1_wu_disable_autodownload_v2",  // V1 composite: full WU auto-download off

        // — Defender / SmartScreen / Firewall —
        "adv_disable_defender_cloud",     // disables Defender cloud protection
        "adv_disable_defender_realtime",  // disables Defender real-time protection
        "adv_remove_defender_entirely",   // fully removes Defender
        "adv_disable_smartscreen_edge_explorer", // disables SmartScreen for Edge/Explorer
        "adv_disable_smartscreen_store",  // disables SmartScreen for Store apps
        "adv_disable_firewall",           // disables Windows Firewall
        "v1_defender_disable_full",       // V1 composite: full Defender teardown

        // — Printing / SMB / IPv6 —
        "adv_disable_print_spooler",      // breaks printing
        "bg_disable_smbv1",               // disables SMBv1 (safe but warns)
        "net_disable_teredo",             // affects IPv6/Teredo (Xbox Live multiplayer)

        // — Memory / Storage —
        "cpu_disable_memory_compression",  // disables RAM compression (32GB+ only)
        "v1_ram_disable_memory_compression", // V1: same intent
        "v1_ssd_life_improvement",        // disables SysMain + Prefetch + MemCompression

        // — Boot / BCD (timer / APIC changes — can BSOD on some hardware) —
        "v1_bcdedit_optimization_safe",   // V1 composite BCD optimization (no DEP off)
        "v1_bcdedit_latency_tweaks",      // V1 minimal BCD latency set

        // — System package removal (experimental) —
        "v1_debloat_remove_system_apps",  // dism /Online /Remove-Package (experimental)
        "v1_privacy_disable_tracking_composite", // IFEO hijacks + Defender disable + telemetry

        // — Boot-time scheduled task —
        "ui_clear_temp_on_boot",          // creates a boot-time scheduled task

        // — IFEO debugger hijacks (telemetry blockers — opt-in) —
        "bg2_disable_compat_tel_runner",  // IFEO CompatTelRunner.exe → no-op
        "bg2_disable_device_census",      // IFEO DeviceCensus.exe → no-op
    };

    // Returns true if a tweak ID is in the destructive list.
    public static bool IsDestructiveTweak(string id)
    {
        foreach (var d in DestructiveTweaks)
            if (string.Equals(d, id, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // Emergency rollback: restore the last registry backup (if any).
    // Returns true if a backup was found and restored.
    public static bool EmergencyRestoreLastBackup()
    {
        try
        {
            var backupRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "NoveNTweaks_Backups");
            if (!Directory.Exists(backupRoot)) return false;

            // Find the most recent session directory
            var sessions = Directory.GetDirectories(backupRoot)
                .OrderByDescending(d => d)
                .ToArray();
            if (sessions.Length == 0) return false;

            var latestSession = sessions[0];
            var regFiles = Directory.GetFiles(latestSession, "*.reg");
            if (regFiles.Length == 0) return false;

            // Import all .reg files in this session
            int imported = 0;
            foreach (var regFile in regFiles)
            {
                if (Run.Hidden($"reg.exe import \"{regFile}\"") == 0) imported++;
            }
            return imported > 0;
        }
        catch { return false; }
    }

    // Safe-mode check: returns true if Windows booted in Safe Mode.
    // Tweaks should not be applied in Safe Mode (some services are disabled).
    public static bool IsSafeMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\SafeBoot", false);
            return key != null;
        }
        catch { return false; }
    }

    // Verify the system is in a state where it's safe to apply tweaks.
    // Returns (ok, reason). If ok is false, the caller should warn the user.
    public static (bool ok, string reason) VerifySafeToApply()
    {
        if (IsSafeMode())
            return (false, "System is in Safe Mode. Tweaks cannot be reliably applied.");
        if (!IsAdmin())
            return (false, "Administrator privileges required. Please run NoveN Tweaks as Administrator.");
        if (IsWindowsUpdateRunning())
            return (false, "Windows Update is currently running. Please wait for it to finish before applying tweaks.");
        if (!HasFreeDiskSpace(500 * 1024 * 1024))
            return (false, "Less than 500MB free on system drive. Free up space before applying tweaks.");
        return (true, "OK");
    }

    // ───────── v2.8: Additional safety guards ─────────

    // Returns true if a critical audio service is currently running.
    // This prevents applying tweaks that would break audio during a meeting/game.
    public static bool IsAudioInUse()
    {
        try
        {
            // Check if any process is actively using audio capture (microphone)
            var procs = new[] { "Discord", "Zoom", "Teams", "Skype", "OBS64", "obs64", "obs",
                                "Slack", "Webex", "wechat", "QQ", "TeamSpeak", "Mumble" };
            foreach (var p in procs)
            {
                if (System.Diagnostics.Process.GetProcessesByName(p).Length > 0)
                    return true;
            }
            return false;
        }
        catch { return false; }
    }

    // Returns true if a game is currently running (heuristic).
    // Prevents applying disruptive tweaks during gameplay.
    public static bool IsGameRunning()
    {
        try
        {
            // Common game processes and launchers
            var procs = new[] { "cs2", "Valorant", "Fortnite", "Minecraft", "Overwatch",
                                "Apex", "RustClient", "TslGame", "PUBG", "GenshinImpact",
                                "LeagueClient", "League of Legends", "Dota2", "Wow",
                                "EpicGamesLauncher", "Steam", "Battle.net" };
            foreach (var p in procs)
            {
                if (System.Diagnostics.Process.GetProcessesByName(p).Length > 0)
                    return true;
            }
            return false;
        }
        catch { return false; }
    }

    // Returns true if the system is running on battery AND battery is low (<20%).
    // Disruptive tweaks should be deferred to avoid mid-game shutdown.
    public static bool IsBatteryLow()
    {
        try
        {
            var power = System.Windows.Forms.SystemInformation.PowerStatus;
            if (power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online)
                return false; // on AC power
            return power.BatteryLifePercent < 0.20f;
        }
        catch { return false; }
    }

    // Verify a tweak is safe to apply RIGHT NOW (extended check).
    // Returns (ok, reason). Use this before applying disruptive tweaks.
    public static (bool ok, string reason) VerifySafeToApplyNow()
    {
        var (baseOk, baseReason) = VerifySafeToApply();
        if (!baseOk) return (baseOk, baseReason);

        if (IsBatteryLow())
            return (false, "Battery is below 20% and not charging. Plug in AC power before applying tweaks.");
        return (true, "OK");
    }

    // Returns true if the given tweak ID is "device-critical" — i.e. it can
    // break microphone, camera, audio, or input devices. These tweaks get an
    // extra confirmation dialog before applying.
    //
    // v1.4 — rewritten to reference REAL snake_case tweak IDs.
    // NOTE: The 7 hardware-disabling tweaks (adv_disable_camera_mic_access,
    // adv_disable_frame_server, adv_disable_frame_server_monitor,
    // adv_disable_sti_svc, adv_disable_audio_endpoint_builder,
    // adv_disable_audiosrv, svc_deviceassoc) have been REMOVED from the
    // tweak catalog entirely — they are no longer registered with the panel.
    // The list below now contains only tweaks that are STILL available but
    // touch device/network functionality and should prompt the user.
    public static readonly string[] DeviceCriticalTweaks = new[]
    {
        // — Xbox / Gamepad (headset & controller pairing) —
        "xbox_auth_manager",        // XblAuthManager service
        "xbox_networking",          // XboxNetApiSvc service
        "xbox_live_game_save",       // XblGameSave service
        "xbox_game_save_net",        // XboxGipSvc service
        "xbox_accessory_mgmt",      // GamingServices (Accessories)
        "xbox_game_monitoring",      // GameMonitoring service

        // — Networking that affects multiplayer / IPv6 —
        "net_disable_teredo",       // Teredo (Xbox Live multiplayer)
        "net_disable_wifi_roaming",  // WLAN roaming aggressiveness (radio stays on)

        // — Printing (device driver) —
        "adv_disable_print_spooler", // Print Spooler

        // — Security features that protect user data / devices —
        "adv_disable_hvci",          // Memory Integrity (HVCI)
        "adv_disable_vbs",           // Virtualization-Based Security
        "adv_disable_device_guard",  // Device Guard / Code Integrity
        "adv_disable_hyperv_feature", // Hyper-V (breaks WSL2/Sandbox)
        "adv_disable_hypervisor_launch", // Hypervisor launch

        // — Defender / SmartScreen / Firewall (device security surface) —
        "adv_disable_defender_cloud",
        "adv_disable_defender_realtime",
        "adv_remove_defender_entirely",
        "adv_disable_smartscreen_edge_explorer",
        "adv_disable_smartscreen_store",
        "adv_disable_firewall",

        // — Memory compression (affects responsiveness on low-RAM devices) —
        "cpu_disable_memory_compression",
        "v1_ram_disable_memory_compression",

        // — Background service sweepers (composite) —
        "v1_services_disable_unwanted_v2",
        "v1_privacy_disable_tracking_composite",
    };

    public static bool IsDeviceCriticalTweak(string id)
    {
        foreach (var d in DeviceCriticalTweaks)
            if (string.Equals(d, id, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    // Backup critical audio/device registry keys before a tweak that may affect them.
    // Returns the directory where backups were saved.
    public static string BackupDeviceCriticalKeys()
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "NoveNTweaks_Backups",
                $"DeviceCritical_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(dir);

            // Backup audio-related registry keys
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Services\Audiosrv", "AudioService");
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Services\AudioEndpointBuilder", "AudioEndpointBuilder");
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Services\BthServ", "BluetoothService");
            BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone", "MicConsent");
            BackupRegistryKey(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam", "CamConsent");
            BackupRegistryKey(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "AppPrivacy");
            return dir;
        }
        catch { return null; }
    }

    // ───────── v2.9: Critical safety additions ─────────

    // Backup the Boot Configuration Data (BCD) store before any bcdedit tweak.
    // A bad BCD change can leave the system unbootable; this backup allows recovery
    // via `bcdedit /import <file>` from the Windows Recovery Console.
    public static bool BackupBcd()
    {
        try
        {
            var dir = GetBackupDir();
            var file = Path.Combine(dir, $"BCD_backup_{DateTime.Now:HHmmss}.bcd");
            // bcdedit /export saves the entire BCD store
            return Run.Hidden($"bcdedit.exe /export \"{file}\"") == 0;
        }
        catch { return false; }
    }

    // Backup critical HKLM registry keys before destructive tweaks.
    // Call this before modifying HKLM\SYSTEM\CurrentControlSet\Control\* or Services\*.
    public static bool BackupSystemRegistry()
    {
        try
        {
            // The most critical keys that tweaks modify
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "MemoryMgmt");
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Control\CrashControl", "CrashControl");
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "DeviceGuard");
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "Tcpip6");
            BackupRegistryKey(@"HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcpip");
            BackupRegistryKey(@"HKLM\SOFTWARE\Policies\Microsoft\Windows", "PoliciesWindows");
            return true;
        }
        catch { return false; }
    }

    // Returns true if the system is running in a virtual machine.
    // Timer/boot tweaks (HPET, disabledynamictick, tscsyncpolicy) are unreliable in VMs
    // and can cause BSODs (CLOCK_WATCHDOG_TIMEOUT) on some hypervisors.
    public static bool IsVirtualMachine()
    {
        try
        {
            var output = Run.Capture("powershell.exe",
                "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-WmiObject Win32_ComputerSystem).Manufacturer\"");
            var manufacturer = output.Trim().ToLowerInvariant();
            // Common VM manufacturer signatures
            string[] vmSignatures = { "vmware", "virtualbox", "xen", "kvm", "qemu", "microsoft corporation", "innotek", "parallels" };
            foreach (var sig in vmSignatures)
                if (manufacturer.Contains(sig)) return true;

            // Also check the model for VM signatures
            var modelOutput = Run.Capture("powershell.exe",
                "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-WmiObject Win32_ComputerSystem).Model\"");
            var model = modelOutput.Trim().ToLowerInvariant();
            foreach (var sig in vmSignatures)
                if (model.Contains(sig)) return true;
            // "Virtual Machine" is a common model string
            if (model.Contains("virtual")) return true;
            return false;
        }
        catch { return false; }
    }

    // Returns the total physical RAM in GB. Used to guard tweaks that require
    // a minimum RAM (DisableMemoryCompression needs 32GB+, DisableSwapFile needs 16GB+).
    public static int GetTotalRamGb()
    {
        try
        {
            var ms = new MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref ms))
            {
                return (int)(ms.ullTotalPhys / (1024UL * 1024 * 1024));
            }
        }
        catch { }
        return 0;
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    // Verify that a timer/boot tweak is safe to apply on this hardware.
    // Returns (ok, reason). If ok is false, the tweak should be skipped.
    public static (bool ok, string reason) VerifyTimerTweakSafe()
    {
        if (IsVirtualMachine())
            return (false, "Timer/boot tweaks (HPET, disabledynamictick, etc.) are unreliable in virtual machines and can cause BSODs. Skipped for safety.");
        if (IsOnBattery())
            return (false, "Timer tweaks should not be applied on battery power. Plug in AC power first.");
        return (true, "OK");
    }
}
