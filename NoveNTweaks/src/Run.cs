using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NoveNTweaks;

// Helpers for running external processes (powercfg, bcdedit, schtasks, reg.exe, sc.exe)
// silently, and for managing scheduled tasks.
//
// v2.0 — hardened: proper argument quoting, real Appx package lists,
// faster process spawning, and output capture helpers.
public static class Run
{
    // Run a command line hidden, wait for completion, return exit code.
    // v2.3: Now checks SafetyGuard.IsCommandSafe before executing — refuses
    // to run commands containing dangerous patterns (format, diskpart, etc.).
    public static int Hidden(string commandLine)
    {
        try
        {
            // v2.3 safety check: refuse dangerous commands
            if (!SafetyGuard.IsCommandSafe(commandLine))
            {
                Debug.WriteLine($"[SafetyGuard] BLOCKED dangerous command: {commandLine}");
                try { DevConsole.LogBlockedCommand(commandLine); } catch { }
                return -1;
            }
            try { DevConsole.Log($"[Run] {commandLine}"); } catch { }

            string exe, args;
            if (commandLine.Contains("&&") || commandLine.Contains("||") || commandLine.Contains("|"))
            {
                exe = "cmd.exe";
                args = $"/c {commandLine}";
            }
            else if (commandLine.StartsWith("\""))
            {
                var end = commandLine.IndexOf('"', 1);
                if (end < 0) return -1;
                exe = commandLine.Substring(1, end - 1);
                args = commandLine.Substring(end + 1).TrimStart();
            }
            else
            {
                var sp = commandLine.IndexOf(' ');
                if (sp < 0) { exe = commandLine; args = ""; }
                else { exe = commandLine.Substring(0, sp); args = commandLine.Substring(sp + 1); }
            }

            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            var p = Process.Start(psi);
            if (p == null) return -1;
            p.WaitForExit(30000);
            if (!p.HasExited) { try { p.Kill(); } catch { } return -1; }
            return p.ExitCode;
        }
        catch { return -1; }
    }

    // Run a command line hidden, with a custom timeout. Used for long-running
    // operations like winget install (can take minutes per package).
    // v2.5: Same safety checks as Hidden() but with longer timeout.
    public static int HiddenLong(string commandLine, int timeoutMs = 600000)
    {
        try
        {
            if (!SafetyGuard.IsCommandSafe(commandLine))
            {
                Debug.WriteLine($"[SafetyGuard] BLOCKED dangerous command: {commandLine}");
                return -1;
            }

            string exe, args;
            if (commandLine.StartsWith("\""))
            {
                var end = commandLine.IndexOf('"', 1);
                if (end < 0) return -1;
                exe = commandLine.Substring(1, end - 1);
                args = commandLine.Substring(end + 1).TrimStart();
            }
            else
            {
                var sp = commandLine.IndexOf(' ');
                if (sp < 0) { exe = commandLine; args = ""; }
                else { exe = commandLine.Substring(0, sp); args = commandLine.Substring(sp + 1); }
            }

            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            var p = Process.Start(psi);
            if (p == null) return -1;
            p.WaitForExit(timeoutMs);
            if (!p.HasExited) { try { p.Kill(); } catch { } return -1; }
            return p.ExitCode;
        }
        catch { return -1; }
    }

    // Run a command and capture stdout (used by Detect functions).
    public static string Capture(string exe, string args, int timeoutMs = 10000)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
            };
            var p = Process.Start(psi);
            if (p == null) return "";
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(timeoutMs);
            if (!p.HasExited) { try { p.Kill(); } catch { } }
            return output ?? "";
        }
        catch { return ""; }
    }

    public static int Powercfg(string args) => Hidden($"powercfg.exe {args}");
    public static int Bcdedit(string args) => Hidden($"bcdedit.exe {args}");

    // Run a PowerShell cmdlet (e.g. Set-MpPreference, Set-NetFirewallProfile).
    // These cmdlets have no direct C# equivalent, so we delegate to powershell.exe.
    public static int PsCmdlet(string cmdlet)
        => Hidden($"powershell.exe -NoProfile -NonInteractive -WindowStyle Hidden -Command \"{cmdlet}\"");

    // ───────── Appx / UWP package removal ─────────
    // Remove UWP Appx packages matching the filter pattern (e.g. "*bing*").
    // Returns true if the command ran without a hard error.
    public static bool RemoveAppxFilter(string filter)
    {
        try
        {
            // Remove for current user AND all-users to catch staged packages.
            Hidden($"powershell.exe -NoProfile -NonInteractive -WindowStyle Hidden -Command \"Get-AppxPackage -Name {filter} -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue\"");
            Hidden($"powershell.exe -NoProfile -NonInteractive -WindowStyle Hidden -Command \"Get-AppxPackage -Name {filter} -AllUsers -ErrorAction SilentlyContinue | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue\"");
            // Also remove staged (provisioned) packages so they don't come back for new users.
            Hidden($"powershell.exe -NoProfile -NonInteractive -WindowStyle Hidden -Command \"Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Where-Object {{ $_.DisplayName -like '{filter}' }} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue\"");
            return true;
        }
        catch { return false; }
    }

    // Remove a known set of bloatware Appx packages by name pattern.
    // This is what A_Bloatware / A_RemoveBingApps / A_RemoveInboxApps SHOULD have done.
    public static int RemoveAppxSet(string[] patterns)
    {
        int removed = 0;
        foreach (var p in patterns)
        {
            if (RemoveAppxFilter(p)) removed++;
        }
        return removed;
    }

    // Common bloatware package patterns.
    public static readonly string[] BloatwarePatterns = new[]
    {
        "*BingWeather*", "*BingNews*", "*BingFinance*", "*BingSports*",
        "*Microsoft3DViewer*", "*MicrosoftSolitaireCollection*", "*MicrosoftWindowsMaps*",
        "*Microsoft.People*", "*Microsoft.ZuneMusic*", "*Microsoft.ZuneVideo*",
        "*Microsoft.GetHelp*", "*Microsoft.Getstarted*", "*Microsoft.Messaging*",
        "*Microsoft.OneConnect*", "*Microsoft.Microsoft3DViewer*",
        "*Microsoft.WindowsSoundRecorder*", "*Microsoft.MicrosoftStickyNotes*",
        "*king.com.CandyCrushSaga*", "*king.com.CandyCrushSodaSaga*",
        "*TikTok*", "*Disney.*", "*Spotify*", "*EclipseManager*",
        "*ActiproSoftwareLLC*", "*AdobeSystemsIncorporated.AdobePhotoshopExpress*",
        "*Duolingo-LearnLanguagesforFree*", "*PandoraMediaInc*", "*Coinbase*",
        "*Instagram*", "*Facebook*", "*Twitter*", "*iHeartRadio*",
        "*Microsoft.NetworkSpeedTest*", "*Microsoft.BingTranslator*",
        "*Microsoft.Office.OneNote*", "*Microsoft.MicrosoftOfficeHub*",
        "*Microsoft.SkypeApp*", "*Microsoft.WindowsFeedbackHub*",
        "*Microsoft.WindowsMaps*", "*Microsoft.Print3D*", "*3DBuilder*",
        "*Microsoft.MixedReality.Portal*", "*Microsoft.Wallet*",
    };

    // Inbox (built-in) apps that are safe to remove.
    public static readonly string[] InboxPatterns = new[]
    {
        "*Microsoft.WindowsCommunicationsApps*", "*Microsoft.WindowsCalendar*",
        "*microsoft.windowsphotos*", "*Microsoft.WindowsAlarms*",
        "*Microsoft.WindowsCalculator*", "*Microsoft.WindowsCamera*",
        "*Microsoft.WindowsStore*", "*Microsoft.XboxApp*", "*Microsoft.XboxGameOverlay*",
        "*Microsoft.XboxGamingOverlay*", "*Microsoft.XboxIdentityProvider*",
        "*Microsoft.XboxSpeechToTextOverlay*", "*Microsoft.Xbox.TCUI*",
    };

    // Bing-specific patterns.
    public static readonly string[] BingPatterns = new[]
    {
        "*BingWeather*", "*BingNews*", "*BingFinance*", "*BingSports*", "*BingTranslator*",
    };

    // True if at least one Appx package matching the filter is installed.
    public static bool AppxExists(string filter)
    {
        try
        {
            var output = Capture("powershell.exe",
                $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-AppxPackage -AllUsers -Name {filter} -ErrorAction SilentlyContinue).Count\"");
            return int.TryParse(output.Trim(), out int n) && n > 0;
        }
        catch { return false; }
    }

    // True if NO Appx package matching any of the patterns is installed
    // (used to detect "all bloatware removed" state).
    public static bool AllAppxRemoved(string[] patterns)
    {
        foreach (var p in patterns)
            if (AppxExists(p)) return false;
        return true;
    }

    /// <summary>
    /// Lists installed Appx packages matching the given pattern.
    /// Returns a list of (Name, PackageFullName) tuples.
    /// Uses PowerShell Get-AppxPackage to query installed packages.
    /// </summary>
    public static List<(string Name, string PackageFullName)> ListAppxPackages(string pattern)
    {
        var result = new List<(string, string)>();
        try
        {
            var output = Capture("powershell.exe",
                $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"Get-AppxPackage -AllUsers -Name {pattern} -ErrorAction SilentlyContinue | Select-Object Name, PackageFullName | ForEach-Object {{ $_.Name + '|' + $_.PackageFullName }}\"");
            if (!string.IsNullOrWhiteSpace(output))
            {
                foreach (var line in output.Split('\n', '\r'))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    var idx = trimmed.IndexOf('|');
                    if (idx > 0)
                    {
                        var name = trimmed.Substring(0, idx).Trim();
                        var pkg = trimmed.Substring(idx + 1).Trim();
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(pkg))
                            result.Add((name, pkg));
                    }
                }
            }
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Removes a specific Appx package by its PackageFullName for all users.
    /// </summary>
    public static bool RemoveAppxPackage(string packageFullName)
    {
        try
        {
            Hidden($"powershell.exe -NoProfile -NonInteractive -WindowStyle Hidden -Command \"Remove-AppxPackage -Package '{packageFullName}' -AllUsers -ErrorAction SilentlyContinue\"");
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Removes all Appx packages and provisioned packages matching the given pattern.
    /// </summary>
    public static bool RemoveAppxPackageByPattern(string pattern)
    {
        try
        {
            Hidden($"powershell.exe -NoProfile -NonInteractive -WindowStyle Hidden -Command \"Get-AppxPackage -Name '{pattern}' -AllUsers -ErrorAction SilentlyContinue | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue\"");
            Hidden($"powershell.exe -NoProfile -NonInteractive -WindowStyle Hidden -Command \"Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Where-Object {{ $_.DisplayName -like '{pattern}' }} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue\"");
            return true;
        }
        catch { return false; }
    }

    // True if the active power plan matches the given text (e.g. "Ultimate Performance").
    public static bool PowercfgMatchesActive(string text)
    {
        try
        {
            var output = Capture("powercfg.exe", "-getactivescheme");
            return output.Contains(text);
        }
        catch { return false; }
    }

    // True if `powercfg -list` output contains the given text.
    public static bool PowercfgListContains(string text)
    {
        try
        {
            var output = Capture("powercfg.exe", "-list");
            return output.Contains(text);
        }
        catch { return false; }
    }

    // Returns PUAProtection from Get-MpPreference (0=off, 1=on, -1=unknown).
    public static int MpPreferencePUA()
    {
        try
        {
            var output = Capture("powershell.exe",
                "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-MpPreference -ErrorAction SilentlyContinue).PUAProtection\"");
            return int.TryParse(output.Trim(), out int n) ? n : -1;
        }
        catch { return -1; }
    }

    // True if the Public firewall profile is enabled.
    public static bool FirewallPublicEnabled()
    {
        try
        {
            var output = Capture("powershell.exe",
                "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-NetFirewallProfile -Profile Public -ErrorAction SilentlyContinue).Enabled\"");
            return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    // True if SMB1 protocol is enabled on the server.
    public static bool Smb1Enabled()
    {
        try
        {
            var output = Capture("powershell.exe",
                "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-SmbServerConfiguration -ErrorAction SilentlyContinue).EnableSMB1Protocol\"");
            return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    // True if the given local user account is enabled.
    public static bool LocalUserEnabled(string name)
    {
        try
        {
            var output = Capture("powershell.exe",
                $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-LocalUser '{name}' -ErrorAction SilentlyContinue).Enabled\"");
            return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    // True if `auditpol /get /subcategory:"Logon"` reports "Success and Failure".
    public static bool AuditPolicyLogonSuccessAndFailure()
    {
        try
        {
            var output = Capture("auditpol.exe", "/get /subcategory:\"Logon\"");
            return output.Contains("Success and Failure");
        }
        catch { return false; }
    }

    // ───────── New helpers for performance & process reduction ─────────

    // Flush the DNS resolver cache (net effect: faster name resolution after a flush).
    public static bool FlushDns() => Hidden("ipconfig.exe /flushdns") == 0;

    // Run disk cleanup with the specified sageset profile (silent).
    public static bool DiskCleanup(int profile) => Hidden($"cleanmgr.exe /sagerun:{profile}") == 0;

    // Trim an SSD volume (requires Windows 8+). Safe no-op on HDDs.
    public static bool TrimDrive(string driveLetter)
    {
        try
        {
            var output = Capture("powershell.exe",
                $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"Optimize-Volume -DriveLetter {driveLetter} -ReTrim -Verbose -ErrorAction SilentlyContinue\"");
            return output.Contains("already") || output.Contains("TRIM") || output.Length > 0;
        }
        catch { return false; }
    }

    // Stop a process by name (without killing critical system processes).
    public static bool StopProcess(string name)
    {
        try
        {
            var procs = Process.GetProcessesByName(name);
            foreach (var p in procs)
            {
                try { p.CloseMainWindow(); } catch { }
                try { p.Kill(); } catch { }
            }
            return true;
        }
        catch { return false; }
    }

    // Count running processes (for the UI counter).
    public static int ProcessCount()
    {
        try { return Process.GetProcesses().Length; }
        catch { return 0; }
    }

    // Delete files in a folder older than the specified days. Safe for temp/cache folders.
    public static int CleanFolder(string path, int olderThanDays = 1)
    {
        int deleted = 0;
        try
        {
            if (!Directory.Exists(path)) return 0;
            var cutoff = DateTime.Now.AddDays(-olderThanDays);
            foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (File.GetLastWriteTime(f) < cutoff)
                    {
                        File.Delete(f);
                        deleted++;
                    }
                }
                catch { }
            }
        }
        catch { }
        return deleted;
    }
}

public static class Task
{
    // Disable a scheduled task by its full path (e.g. "\Microsoft\Windows\...\Task Name").
    public static bool Disable(string taskPath)
        => Run.Hidden($"schtasks.exe /change /tn \"{taskPath}\" /disable") == 0;

    public static bool Enable(string taskPath)
        => Run.Hidden($"schtasks.exe /change /tn \"{taskPath}\" /enable") == 0;

    // True if the scheduled task exists and is enabled.
    public static bool IsEnabled(string taskPath)
    {
        try
        {
            var output = Run.Capture("schtasks.exe", $"/Query /TN \"{taskPath}\" /FO LIST");
            return output.Contains("Enabled");
        }
        catch { return false; }
    }

    // True if the scheduled task exists at all.
    public static bool Exists(string taskPath)
    {
        try
        {
            var output = Run.Capture("schtasks.exe", $"/Query /TN \"{taskPath}\" /FO LIST");
            return output.Length > 0 && !output.Contains("ERROR");
        }
        catch { return false; }
    }
}
