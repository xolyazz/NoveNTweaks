using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NoveNTweaks;

// ============================================================
// DevConsole — v2.9 developer console (separate file, easy to delete)
//
// Logs all tweak applications, errors, blocked commands to a log file
// in TEMP. Opens a CMD window that shows the log in real-time.
//
// To disable this feature: delete DevConsole.cs and remove the
// BtnOpenDevConsole/BtnClearDevLog XAML + Click handlers in MainWindow.
// ============================================================

public static class DevConsole
{
    private static string _logPath;
    private static readonly object _lock = new();

    // Returns the log file path. Creates the file if it doesn't exist.
    public static string GetLogPath()
    {
        if (_logPath != null) return _logPath;
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "NoveNTweaks");
            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir, "dev_console.log");

            if (!File.Exists(_logPath))
            {
                File.WriteAllText(_logPath,
                    $"=== NoveN Tweaks Developer Console ===\r\n" +
                    $"=== Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\r\n" +
                    $"=== This file is for developers only. You can delete it at any time. ===\r\n\r\n");
            }
        }
        catch { }
        return _logPath ?? "";
    }

    // Append a log line with timestamp.
    public static void Log(string message)
    {
        try
        {
            lock (_lock)
            {
                var path = GetLogPath();
                if (string.IsNullOrEmpty(path)) return;
                var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n";
                File.AppendAllText(path, line);
            }
        }
        catch { }
    }

    // Log a tweak application.
    public static void LogTweak(string tweakId, string action, bool success, string details = null)
    {
        var status = success ? "OK" : "FAIL";
        var msg = $"Tweak {action}: {tweakId} [{status}]";
        if (!string.IsNullOrEmpty(details)) msg += $" — {details}";
        Log(msg);
    }

    // Log a blocked command (SafetyGuard blocked it).
    public static void LogBlockedCommand(string command)
    {
        Log($"[BLOCKED] Dangerous command refused: {command}");
    }

    // Log an error.
    public static void LogError(string source, Exception ex)
    {
        Log($"[ERROR] {source}: {ex?.Message ?? "null"}");
    }

    // Clear the log file.
    public static void Clear()
    {
        try
        {
            var path = GetLogPath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            GetLogPath(); // recreate with header
        }
        catch { }
    }

    // Open a CMD window that tails the log file in real-time.
    // Uses `cmd /k powershell -NoExit -Command "Get-Content -Path <log> -Wait -Tail 50"`
    public static bool OpenConsole()
    {
        try
        {
            var path = GetLogPath();
            if (string.IsNullOrEmpty(path)) return false;

            // Use PowerShell Get-Content -Wait for real-time tailing
            var psCmd = $"-NoExit -Command \"Write-Host '=== NoveN Tweaks Developer Console ===' -ForegroundColor Cyan; Write-Host 'Log file: {path}' -ForegroundColor Gray; Write-Host 'Watching for changes... (Ctrl+C to stop)' -ForegroundColor Yellow; Write-Host ''; Get-Content -Path '{path}' -Wait -Tail 50\"";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = psCmd,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
            };
            Process.Start(psi);
            Log("[DevConsole] Developer console opened.");
            return true;
        }
        catch (Exception ex)
        {
            LogError("DevConsole.OpenConsole", ex);
            return false;
        }
    }

    // Open the log file in Notepad (alternative to console window).
    public static bool OpenInNotepad()
    {
        try
        {
            var path = GetLogPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            Process.Start("notepad.exe", $"\"{path}\"");
            return true;
        }
        catch { return false; }
    }

    // Open the folder containing the log file in Explorer.
    public static bool OpenFolder()
    {
        try
        {
            var path = GetLogPath();
            if (string.IsNullOrEmpty(path)) return false;
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true,
            });
            return true;
        }
        catch { return false; }
    }
}
