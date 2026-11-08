using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Media;

namespace NoveNTweaks;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Self-elevate if not running as administrator (the manifest also requests
        // requireAdministrator, but this guards against a no-manifest launch).
        if (!IsAdministrator())
        {
            try
            {
                var exe = Process.GetCurrentProcess().MainModule!.FileName;
                var psi = new ProcessStartInfo(exe)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                };
                Process.Start(psi);
            }
            catch { /* user declined UAC */ }
            Shutdown();
            return;
        }

        // ===== HARDWARE ACCELERATION =====
        // Hardware acceleration is controlled by the user via the HwAccel setting
        // in the Settings panel. Here we just apply the saved setting on startup.
        // The MainWindow.LoadedCore() method reads the registry and calls ApplyHwAccel().
        try
        {
            // Default to GPU acceleration on first run (before registry is read).
            System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;
            // Higher-quality text rendering.
            //TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
            //TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            // Crisper images.
            //RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            // Process priority boost for snappier UI.
            using var proc = Process.GetCurrentProcess();
            proc.PriorityClass = ProcessPriorityClass.AboveNormal;
        }
        catch { /* non-fatal */ }

        base.OnStartup(e);
    }

    private static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }
}
