using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace NoveNTweaks;

/// <summary>
/// v2.8: Elevation helper — checks if the current process has admin privileges
/// and can restart the app elevated (via UAC) when an operation requires it.
///
/// The app.manifest is set to asInvoker, so the app starts without a UAC prompt.
/// When a tweak or operation needs admin (HKLM writes, system commands like
/// powercfg/bcdedit/sc/schtasks), call EnsureAdmin() to prompt the user.
/// </summary>
public static class ElevationHelper
{
    /// <summary>True if the current process is running with administrator privileges.</summary>
    public static bool IsElevated
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Restarts the current application with administrator privileges via UAC.
    /// Returns true if the elevated process was started (the current process should then exit).
    /// Returns false if the user declined the UAC prompt or an error occurred.
    /// </summary>
    public static bool RestartAsAdmin(string args = "")
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return false;

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true, // Required for Verb = "runas"
                Verb = "runas",         // Triggers UAC elevation
                WorkingDirectory = Environment.CurrentDirectory,
            };
            if (!string.IsNullOrEmpty(args))
                psi.Arguments = args;

            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RestartAsAdmin failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if the process is elevated. If not, prompts the user to restart as admin.
    /// Returns true if already elevated (caller can proceed).
    /// Returns false if NOT elevated — caller should abort the operation.
    /// If the user accepts the UAC prompt, the app restarts elevated and the caller
    /// process will exit.
    /// </summary>
    public static bool EnsureAdminOrPrompt(string reason = "")
    {
        if (IsElevated) return true;

        // The actual WPF dialog is shown by MainWindow.ShowElevationDialog.
        // This method is a non-UI helper; the UI layer calls RestartAsAdmin.
        return false;
    }
}
