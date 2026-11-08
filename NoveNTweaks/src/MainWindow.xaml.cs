using System.Windows.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace NoveNTweaks;

public partial class MainWindow : Window
{
    I18n.Language _lang = I18n.Language.EN;
    I18n.Strings s => I18n.Get(_lang);

    public static readonly DependencyProperty AreAnimationsEnabledProperty =
        DependencyProperty.Register("AreAnimationsEnabled", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

    public bool AreAnimationsEnabled
    {
        get { return (bool)GetValue(AreAnimationsEnabledProperty); }
        set { SetValue(AreAnimationsEnabledProperty, value); }
    }

    private List<GpuInfo> _detectedGpus = new();

    void DetectGpusWithDxgi()
    {
        _detectedGpus.Clear();
        try
        {
            Guid factoryGuid = new Guid("7b7166ec-21c7-44ae-b21a-c9ae321ae369");
            int hr = CreateDXGIFactory(ref factoryGuid, out IntPtr factoryPtr);
            if (hr == 0 && factoryPtr != IntPtr.Zero)
            {
                IntPtr factoryVtable = Marshal.ReadIntPtr(factoryPtr);
                IntPtr enumAdaptersPtr = Marshal.ReadIntPtr(factoryVtable, 7 * IntPtr.Size);
                var enumAdapters = Marshal.GetDelegateForFunctionPointer<EnumAdaptersDelegate>(enumAdaptersPtr);

                uint index = 0;
                while (true)
                {
                    hr = enumAdapters(factoryPtr, index, out IntPtr adapterPtr);
                    if (hr != 0 || adapterPtr == IntPtr.Zero) break;

                    IntPtr adapterVtable = Marshal.ReadIntPtr(adapterPtr);
                    IntPtr getDescPtr = Marshal.ReadIntPtr(adapterVtable, 8 * IntPtr.Size);
                    var getDesc = Marshal.GetDelegateForFunctionPointer<GetDescDelegate>(getDescPtr);

                    DXGI_ADAPTER_DESC desc;
                    hr = getDesc(adapterPtr, out desc);
                    if (hr == 0)
                    {
                        string name = desc.Description ?? "";
                        if (!name.Contains("Microsoft Basic Render Driver") && !string.IsNullOrEmpty(name))
                        {
                            long vramBytes = (long)desc.DedicatedVideoMemory.ToUInt64();
                            string vramStr = vramBytes > 0 ? $"{vramBytes / (1024.0 * 1024 * 1024):F1} GB VRAM" : "";
                            
                            _detectedGpus.Add(new GpuInfo
                            {
                                Name = name,
                                Vram = vramStr,
                                LuidLow = desc.AdapterLuid.LowPart,
                                LuidHigh = desc.AdapterLuid.HighPart
                            });
                        }
                    }

                    Marshal.Release(adapterPtr);
                    index++;
                }

                Marshal.Release(factoryPtr);
            }
        }
        catch { }
    }

    static readonly (string rbName, string panelName, string category)[] Nav = new[]
    {
        ("RBHome",        "PanelHome",        "Home"),
        ("RBPrivacy",     "PanelPrivacy",     "Privacy"),
        ("RBPerf",        "PanelPerf",        "Performance"),
        ("RBDebloat",     "PanelDebloat",     "Debloat"),
        ("RBUpdates",     "PanelUpdates",     "Updates"),
        ("RBNetwork",     "PanelNetwork",     "Network"),
        ("RBInternet",    "PanelInternet",    "Internet"),

        ("RBSystem",      "PanelSystem",      "System"),
        ("RBSecurity",    "PanelSecurity",    "Security"),
        ("RBUI",          "PanelUI",          "UI"),
        ("RBAppearance",  "PanelAppearance",  "Appearance"),
        ("RBContextMenu", "PanelContextMenu", "ContextMenu"),
        ("RBInstaller",   "PanelInstaller",   "Installer"),
        ("RBSettings",    "PanelSettings",    "Settings"),
        ("RBAdvanced",    "PanelAdvanced",    "Advanced"),
    };

    string _activeCategory = null;
    private static bool _isProgrammaticChange = false;
    readonly Dictionary<string, CheckBox> _checkboxes = new();
    readonly Dictionary<string, Border>   _cards      = new();
    readonly Dictionary<string, TextBlock> _titleTbs  = new();
    readonly Dictionary<string, TextBlock> _descTbs   = new();
    readonly Dictionary<string, TextBlock> _noteTbs   = new();
    readonly Dictionary<string, Button>    _actionButtons = new();

    HashSet<string> _appliedTweaks = new();

    void LoadAppliedTweaks()
    {
        try {
            var appliedStr = Reg.GetString(@"HKCU:\SOFTWARE\NoveNTweaks", "AppliedTweaks");
            if (!string.IsNullOrEmpty(appliedStr))
            {
                foreach (var id in appliedStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    _appliedTweaks.Add(id);
            }
        } catch { }
    }

    void SaveAppliedTweaks()
    {
        try {
            Reg.SetString(@"HKCU:\SOFTWARE\NoveNTweaks", "AppliedTweaks", string.Join(",", _appliedTweaks));
        } catch { }
    }
    readonly HashSet<string> _builtPanels = new(); // lazy build tracking
    readonly Dictionary<string, bool> _tweakActiveState = new(); // background state cache
    private System.Threading.CancellationTokenSource _detectCts;

    struct SpeedTestEntry
    {
        public string Time { get; set; }
        public string Ping { get; set; }
        public string Down { get; set; }
        public string Up { get; set; }
        public string Loss { get; set; }
    }
    readonly List<SpeedTestEntry> _speedHistory = new();

    // Theme-aware brushes: resolved live from Application resources so every
    // card / text element follows the active theme (Dark / Normal / White).
    static Brush BgOff         => (Brush)Application.Current.Resources["SurfaceLow"];
    static Brush BgOn          => (Brush)Application.Current.Resources["AccentSoft"];
    static Brush BorderOff     => (Brush)Application.Current.Resources["BorderSoft"];
    static Brush BorderOn      => (Brush)Application.Current.Resources["BorderAccent"];
    static Brush BgHover       => (Brush)Application.Current.Resources["HoverBg"];
    static Brush BgHoverBorder => (Brush)Application.Current.Resources["HoverBorder"];
    static Brush FgTitle       => (Brush)Application.Current.Resources["FgPrimary"];
    static Brush FgDesc        => (Brush)Application.Current.Resources["FgMuted"];
    static Brush FgIcon        => (Brush)Application.Current.Resources["FgIcon"];
    static readonly Brush WarnYellow    = Freeze(new SolidColorBrush(Color.FromRgb(0xEA, 0xB3, 0x08)));
    static Brush WarnYellowBg  => (Brush)Application.Current.Resources["WarnYellowBg"];
    static Brush FgWarnText    => (Brush)Application.Current.Resources["FgPrimary"];
    static readonly FontFamily Mdl2     = new FontFamily("Segoe MDL2 Assets");

    static Brush Freeze(Brush b) { b.Freeze(); return b; }

    // ---- Theme palettes (Dark / Normal / White) ----
    // Dark = pure dark bg, no orbs. Normal = default dark glass + orbs.
    // White = bright white bg with dark text/icons for daylight use.
    static readonly Dictionary<string, Color> DarkPalette = new()
    {
        ["RootBg"]        = Color.FromRgb(0x0D, 0x0D, 0x10),
        ["BgVeil"]        = Color.FromRgb(0x0D, 0x0D, 0x10),
        ["FgPrimary"]     = Color.FromArgb(0xF0, 0xFF, 0xFF, 0xFF),
        ["FgHigh"]        = Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF),
        ["FgMid"]         = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF),
        ["FgMuted"]       = Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF),
        ["FgFaint"]       = Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF),
        ["FgIcon"]        = Color.FromArgb(0x75, 0xFF, 0xFF, 0xFF),
        ["FgOnAccent"]    = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["SurfaceLow"]    = Color.FromArgb(0x0D, 0xFF, 0xFF, 0xFF),
        ["SurfaceMid"]    = Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF),
        ["SurfaceHigh"]   = Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        ["SurfaceGlass"]  = Color.FromArgb(0x0C, 0xFF, 0xFF, 0xFF),
        ["HoverBg"]       = Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        ["HoverBorder"]   = Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF),
        ["BorderSoft"]    = Color.FromArgb(0x16, 0xFF, 0xFF, 0xFF),
        ["BorderMid"]     = Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF),
        ["BorderStrong"]  = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF),
        ["BorderAccent"]  = Color.FromArgb(0xB0, 0x3B, 0x82, 0xF6),
        ["Accent"]        = Color.FromRgb(0x3B, 0x82, 0xF6),
        ["AccentSoft"]    = Color.FromArgb(0x20, 0x3B, 0x82, 0xF6),
        ["AccentFg"]      = Color.FromRgb(0x6B, 0x9C, 0xE8),
        ["PillBg"]        = Color.FromArgb(0xE4, 0xFF, 0xFF, 0xFF),
        ["PillFg"]        = Color.FromRgb(0x11, 0x11, 0x11),
        ["PillHover"]     = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["GhostFg"]       = Color.FromArgb(0x90, 0xFF, 0xFF, 0xFF),
        ["GhostBorder"]   = Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF),
        ["GhostHoverBg"]  = Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF),
        ["GhostHoverBorder"] = Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF),
        ["GhostHoverFg"]  = Color.FromArgb(0xD0, 0xFF, 0xFF, 0xFF),
        ["NavHoverBg"]    = Color.FromArgb(0x0E, 0xFF, 0xFF, 0xFF),
        ["NavCheckedBg"]  = Color.FromArgb(0x1C, 0xFF, 0xFF, 0xFF),
        ["WinBtnHoverBg"] = Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF),
        ["WinBtnHoverFg"] = Color.FromArgb(0xA0, 0xFF, 0xFF, 0xFF),
        ["TrackOffBg"]    = Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        ["TrackOffBorder"]= Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF),
        ["KnobFill"]      = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["ThumbBg"]       = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF),
        ["ThumbHoverBg"]  = Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF),
        ["TrackLane"]     = Color.FromArgb(0x0A, 0xFF, 0xFF, 0xFF),
        ["Shimmer"]       = Color.FromArgb(0x70, 0xFF, 0xFF, 0xFF),
        ["Divider"]       = Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF),
        ["WarnYellowBg"]  = Color.FromArgb(0x15, 0xEA, 0xB3, 0x08),
        ["OverlayBg"]     = Color.FromArgb(0xF2, 0x1A, 0x1A, 0x1A),
        ["OverlayBorder"] = Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF),
        ["OverlayFaint"]  = Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF),
        ["OverlayMid"]    = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF),
        ["OverlayPrimary"]= Color.FromArgb(0xF0, 0xFF, 0xFF, 0xFF),
        ["OverlayHigh"]   = Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF),
    };

    static readonly Dictionary<string, Color> WhitePalette = new()
    {
        ["RootBg"]        = Color.FromRgb(0xFA, 0xFA, 0xFC),
        ["BgVeil"]        = Color.FromRgb(0xFA, 0xFA, 0xFC),
        ["FgPrimary"]     = Color.FromRgb(0x14, 0x14, 0x1A),
        ["FgHigh"]        = Color.FromRgb(0x26, 0x26, 0x2E),
        ["FgMid"]         = Color.FromRgb(0x52, 0x52, 0x5C),
        ["FgMuted"]       = Color.FromRgb(0x6B, 0x6B, 0x76),
        ["FgFaint"]       = Color.FromRgb(0x9A, 0x9A, 0xA6),
        ["FgIcon"]        = Color.FromRgb(0x5A, 0x5A, 0x66),
        ["FgOnAccent"]    = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["SurfaceLow"]    = Color.FromRgb(0xF4, 0xF4, 0xF8),
        ["SurfaceMid"]    = Color.FromRgb(0xEE, 0xEE, 0xF3),
        ["SurfaceHigh"]   = Color.FromRgb(0xE7, 0xE7, 0xEE),
        ["SurfaceGlass"]  = Color.FromRgb(0xF6, 0xF6, 0xFA),
        ["HoverBg"]       = Color.FromArgb(0x0A, 0x00, 0x00, 0x00),
        ["HoverBorder"]   = Color.FromArgb(0x14, 0x00, 0x00, 0x00),
        ["BorderSoft"]    = Color.FromRgb(0xE2, 0xE2, 0xEA),
        ["BorderMid"]     = Color.FromRgb(0xD4, 0xD4, 0xDE),
        ["BorderStrong"]  = Color.FromRgb(0xB8, 0xB8, 0xC6),
        ["BorderAccent"]  = Color.FromRgb(0x25, 0x63, 0xEB),
        ["Accent"]        = Color.FromRgb(0x25, 0x63, 0xEB),
        ["AccentSoft"]    = Color.FromArgb(0x1E, 0x25, 0x63, 0xEB),
        ["AccentFg"]      = Color.FromRgb(0x25, 0x63, 0xEB),
        ["PillBg"]        = Color.FromRgb(0x1A, 0x1A, 0x22),
        ["PillFg"]        = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["PillHover"]     = Color.FromRgb(0x00, 0x00, 0x00),
        ["GhostFg"]       = Color.FromRgb(0x44, 0x44, 0x4C),
        ["GhostBorder"]   = Color.FromRgb(0xC8, 0xC8, 0xD2),
        ["GhostHoverBg"]  = Color.FromArgb(0x08, 0x00, 0x00, 0x00),
        ["GhostHoverBorder"] = Color.FromRgb(0xA8, 0xA8, 0xB6),
        ["GhostHoverFg"]  = Color.FromRgb(0x1A, 0x1A, 0x22),
        ["NavHoverBg"]    = Color.FromArgb(0x08, 0x00, 0x00, 0x00),
        ["NavCheckedBg"]  = Color.FromArgb(0x10, 0x00, 0x00, 0x00),
        ["WinBtnHoverBg"] = Color.FromArgb(0x08, 0x00, 0x00, 0x00),
        ["WinBtnHoverFg"] = Color.FromRgb(0x2A, 0x2A, 0x32),
        ["TrackOffBg"]    = Color.FromRgb(0xDF, 0xDF, 0xE6),
        ["TrackOffBorder"]= Color.FromRgb(0xC4, 0xC4, 0xCE),
        ["KnobFill"]      = Color.FromRgb(0xFF, 0xFF, 0xFF),
        ["ThumbBg"]       = Color.FromRgb(0xB0, 0xB0, 0xBC),
        ["ThumbHoverBg"]  = Color.FromRgb(0x90, 0x90, 0x9C),
        ["TrackLane"]     = Color.FromRgb(0xEC, 0xEC, 0xF1),
        ["Shimmer"]       = Color.FromRgb(0x9A, 0x9A, 0xA6),
        ["Divider"]       = Color.FromRgb(0xE2, 0xE2, 0xEA),
        ["WarnYellowBg"]  = Color.FromArgb(0x22, 0xEA, 0xB3, 0x08),
        ["OverlayBg"]     = Color.FromRgb(0xFC, 0xFC, 0xFE),
        ["OverlayBorder"] = Color.FromRgb(0xD4, 0xD4, 0xDE),
        ["OverlayFaint"]  = Color.FromRgb(0x9A, 0x9A, 0xA6),
        ["OverlayMid"]    = Color.FromRgb(0x52, 0x52, 0x5C),
        ["OverlayPrimary"]= Color.FromRgb(0x14, 0x14, 0x1A),
        ["OverlayHigh"]   = Color.FromRgb(0x26, 0x26, 0x2E),
    };

    static void ApplyPalette(Dictionary<string, Color> palette)
    {
        var r = Application.Current.Resources;
        foreach (var kvp in palette)
        {
            r[kvp.Key] = Freeze(new SolidColorBrush(kvp.Value));
            // Also expose the raw Color under "<Key>Color" so GradientStop.Color can bind to it
            r[kvp.Key + "Color"] = kvp.Value;
        }
    }

    static readonly Dictionary<string, string> CatIcons = new()
    {
        ["Privacy"]     = "\xE722",
        ["Performance"] = "\xE945",
        ["Debloat"]     = "\xE74D",
        ["Updates"]     = "\xE72C",
        ["Network"]     = "\xF0B2",
        ["Internet"]    = "\xE909",
        ["SpeedTest"]   = "\xE8B6",
        ["System"]      = "\xE916",
        ["Security"]    = "\xE773",
        ["UI"]          = "\xE7F4",
        ["Appearance"]  = "\xE771",
        ["ContextMenu"] = "\xE8AB",
        ["Installer"]   = "\xE713",
    };

    SplashWindow _splash;
    DispatcherTimer _sysInfoTimer;
    bool _searchOpen = false;
    bool _licFirstRun = false;

    public MainWindow()
    {
        InitializeComponent();
        Opacity = 0;

        // ── Register extra tweaks (v2.0 additions) ──
        // TweaksExtra adds ~50 new tweaks: process reduction, gaming latency,
        // Win11-specific, memory/disk optimizations, and more warnings.
        try
        {
            if (Tweaks.All.Count > 0 && !Tweaks.All.Any(t => t.Id == "UltimatePerfPlan"))
            {
                Tweaks.All.AddRange(TweaksExtra.Build());
            }
        }
        catch (Exception ex) { Debug.WriteLine($"TweaksExtra registration failed: {ex.Message}"); }

        // ── Register v1.4 process/CPU-reduction tweaks (TweaksProc) ──
        // Adds ~55 new SAFE tweaks that disable non-essential Windows services
        // and scheduled tasks, lowering the process count in Task Manager and
        // reducing idle CPU usage — without touching anything a normal user
        // (who just plays CS) would notice.
        try
        {
            if (Tweaks.All.Count > 0 && !Tweaks.All.Any(t => t.Id == "DisablePhoneSvc"))
            {
                Tweaks.All.AddRange(TweaksProc.Build());
            }
        }
        catch (Exception ex) { Debug.WriteLine($"TweaksProc registration failed: {ex.Message}"); }

        try
        {
            _lang = (I18n.Language)(Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "Language") ?? 0);
            if ((int)_lang < 0 || (int)_lang > 8) _lang = I18n.Language.EN;
        }
        catch { _lang = I18n.Language.EN; }
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    bool _forceExit = false;
    void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_forceExit) return;

        bool ask = TglAskClose != null ? TglAskClose.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "ShowConfirm") ?? 1) == 1;
        if (ask)
        {
            var s = I18n.Get(_lang);
            if (!string.IsNullOrEmpty(s.AppExitConfirmPrompt))
            {
                e.Cancel = true;
                // Use the animated custom dialog (blocks via DispatcherFrame until user answers)
                bool yes = MsgBox(s.AppExitConfirmTitle, s.AppExitConfirmPrompt, DialogType.Question, yesNo: true);
                if (yes)
                {
                    _forceExit = true;
                    Application.Current.Shutdown();
                }
            }
        }
        else
        {
            _forceExit = true;
            Application.Current.Shutdown();
        }
    }

    // Retained for backwards-compat with any external references; the exit-confirm
    // flow now goes through MsgBox() / ShowCustomDialog() instead.
    void OnExitYesClick(object sender, RoutedEventArgs e)
    {
        _forceExit = true;
        Application.Current.Shutdown();
    }

    async void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadAppliedTweaks();
        try { await LoadedCore(); }
        catch (Exception ex)
        {
            Opacity = 1;
            var s0 = I18n.Get(_lang);
            MsgBox(s0.StartupErrorTitle, $"{s0.StartupErrorTitle}: {ex.Message}", DialogType.Error);
        }
    }

    async System.Threading.Tasks.Task LoadedCore()
    {
        _splash = new SplashWindow();
        _splash.Show();
        _splash.SetLoadingText(I18n.Get(_lang).Loading);
        await System.Threading.Tasks.Task.Delay(50);
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

        WireChromeButtons();
        WireNavigation();
        WireFooterButtons();
        WireLanguageButton();

        // v2.7: Pre-initialize the click sound player at startup so the first
        // click plays instantly (no lag from lazy init during a click).\
        try { InitClickSound(); } catch { }

        WireSearch();
        WireSidebarAnimation();
        WireSettings();
        WireInstaller();
        WireHomeLinks();
        WireWizard();
        WireLogoSpin();
        // Apply saved HwAccel setting on startup (uses HwAccelEnabled key, matches .ps1)
        ApplyHwAccel((Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "HwAccelEnabled") ?? 1) == 1);
        BuildHomeStats();
        ApplyLanguage();
        _splash.SetLoadingText(_lang == I18n.Language.PL ? "Wykrywanie stanu wszystkich tweaków..." : "Detecting all tweaks state...");
        await StartEarlyBackgroundDetectionAsync();
        SwitchTo(null);
        CheckAndCreateEmergencyFlashback();

        // ── PRE-BUILD all tweak panels during splash (background thread per panel) ──
        // Each panel is built on the UI thread via Dispatcher.InvokeAsync(Normal priority)
        // so the splash animation stays smooth and each panel pre-computes its cards
        // before the user ever clicks the tab — zero lag on first open.
        var tweakCategories = Nav
            .Where(n => n.category != "Home" && n.category != "Installer"
                     && n.category != "Settings" && n.category != "SpeedTest")
            .Select(n => n.category)
            .ToArray();

        int total = tweakCategories.Length;
        int done  = 0;

        foreach (var cat in tweakCategories)
        {
            string catCapture = cat;
            // Build the panel on the UI thread (required for WPF controls),
            // but yield to let the dispatcher process render/input events between panels.
            await Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (!_builtPanels.Contains(catCapture))
                    {
                        BuildPanel(catCapture);
                        _builtPanels.Add(catCapture);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Pre-build failed for {catCapture}: {ex.Message}");
                }
            }, DispatcherPriority.Background);

            done++;
            int pct = done * 100 / total;
            // Update splash subtitle with progress — non-blocking
            _splash?.SetLoadingText($"Loading... {pct}%");
            // Yield a frame so the spinner keeps animating
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
        }

        _splash?.SetLoadingText("Ready!");
        await System.Threading.Tasks.Task.Delay(120); // brief "Ready" flash

        WireLicenseSystem();
        await RunLicenseStartupCheckAsync();

        var args = Environment.GetCommandLineArgs();
        if (args.Contains("--minimized") || args.Contains("-minimized"))
        {
            this.WindowState = WindowState.Minimized;
        }

        bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;

        // ── Close splash with fade-out, then animate main window in ──
        _splash.CloseGracefully();

        if (anims)
        {
            var ease     = new CubicEase { EasingMode = EasingMode.EaseOut };
            var easeBack = new BackEase  { EasingMode = EasingMode.EaseOut, Amplitude = 0.06 };

            // Apply initial scale transform on the root border for a "pop-in" feel
            if (RootBorderScale != null)
            {
                RootBorderScale.ScaleX = 0.94;
                RootBorderScale.ScaleY = 0.94;
                var scaleAnim = new DoubleAnimation(0.94, 1.0, TimeSpan.FromMilliseconds(480))
                    { EasingFunction = easeBack };
                RootBorderScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
                RootBorderScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
            }

            // 1. Fade in the window
            var fa = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(420))
                { EasingFunction = ease };
            BeginAnimation(OpacityProperty, fa);

            // 2. Slide down TitleBar from top (Y = -40 to 0), slight delay
            if (TitleBarTranslate != null)
            {
                var ta = new DoubleAnimation(-40.0, 0.0, TimeSpan.FromMilliseconds(500))
                    { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(40) };
                TitleBarTranslate.BeginAnimation(TranslateTransform.YProperty, ta);
            }

            // 3. Slide in Sidebar from left (X = -56 to 0), slight delay
            if (SidebarTranslate != null)
            {
                var sa = new DoubleAnimation(-56.0, 0.0, TimeSpan.FromMilliseconds(520))
                    { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(60) };
                SidebarTranslate.BeginAnimation(TranslateTransform.XProperty, sa);
            }

            // 4. Slide up RightPanel from below (Y = 28 to 0), slight delay
            if (RightPanelTranslate != null)
            {
                var ra = new DoubleAnimation(28.0, 0.0, TimeSpan.FromMilliseconds(560))
                    { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(80) };
                RightPanelTranslate.BeginAnimation(TranslateTransform.YProperty, ra);
            }
        }
        else
        {
            Opacity = 1.0;
        }

        // Safety net
        var safety = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        safety.Tick += (_, _) => { safety.Stop(); if (Opacity < 0.99) Opacity = 1; };
        safety.Start();
    }


    // ============================================================
    // CUSTOM CHROME
    // ============================================================
    void WireChromeButtons()
    {
        BtnMin.Click += (_, _) => WindowState = WindowState.Minimized;
        BtnMax.Click += (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        BtnClose.Click += (_, _) => Close();
        TitleBarDrag.MouseLeftButtonDown += (_, ev) => { if (ev.ClickCount == 1) DragMove(); };
    }

    // ============================================================
    // SIDEBAR ANIMATION
    // ============================================================
    void WireSidebarAnimation()
    {
        if (Sidebar == null) return;
        Sidebar.MouseEnter += (_, _) =>
        {
            bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
            if (anims)
            {
                var anim = new DoubleAnimation(200, TimeSpan.FromMilliseconds(220))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                Sidebar.BeginAnimation(WidthProperty, anim);
            }
            else
            {
                Sidebar.BeginAnimation(WidthProperty, null);
                Sidebar.Width = 200;
            }
        };
        Sidebar.MouseLeave += (_, _) =>
        {
            bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
            if (anims)
            {
                var anim = new DoubleAnimation(64, TimeSpan.FromMilliseconds(180))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
                Sidebar.BeginAnimation(WidthProperty, anim);
            }
            else
            {
                Sidebar.BeginAnimation(WidthProperty, null);
                Sidebar.Width = 64;
            }
        };
    }

    // ============================================================
    // HOME CREATOR LINKS — open YouTube / Website in the default browser
    // ============================================================
    void WireHomeLinks()
    {
        // HomeYouTube and HomeWebsite are collapsed placeholder borders kept for XAML compatibility.
    }

    static void OpenLink(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* non-fatal */ }
    }

    // ============================================================
    // NOVEN LOGO SPIN-ON-CLICK  (smooth 360° rotation, theme-aware)
    // Works for both the title-bar logo and the home hero logo.
    // The logo Fill is {DynamicResource FgPrimary} so it follows white/dark themes automatically.
    // ============================================================
    bool _logoSpinning;
    void WireLogoSpin()
    {
        if (TitleBarLogo != null)
            TitleBarLogo.MouseLeftButtonUp += (_, _) => SpinLogo(TitleBarLogoRotate);
        if (HomeHeroLogo != null)
            HomeHeroLogo.MouseLeftButtonUp += (_, _) => SpinLogo(HomeHeroLogoRotate);
    }

    void SpinLogo(System.Windows.Media.RotateTransform rt)
    {
        if (rt == null || _logoSpinning) return;
        bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
        if (!anims) return;

        _logoSpinning = true;
        // Smooth, slightly snappy 360° spin with ease-in-out, then snap back to 0 (invisible jump)
        var anim = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(780))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.Stop, // return to base value (0) when done
        };
        anim.Completed += (_, _) =>
        {
            rt.Angle = 0; // ensure clean reset (360 == 0 visually, so no jump)
            _logoSpinning = false;
        };
        rt.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, anim);
    }

    // ============================================================
    // NAVIGATION (lazy panel building)
    // ============================================================
    void WireNavigation()
    {
        foreach (var (rbName, _, cat) in Nav)
        {
            var rb = (RadioButton)FindName(rbName);
            if (rb == null) continue;
            string capture = cat;
            rb.Checked += (_, _) => SwitchTo(capture);
        }
    }

    void SwitchTo(string category)
    {
        try
        {
            if (category == null) category = "Home";
            else PlaySoundEffect();

            // v2.8: Warn before entering Advanced category (danger zone)
            if (category == "Advanced")
            {
                var sAdv = I18n.Get(_lang);
                bool ack = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AdvancedAck") ?? 0) == 1;
                if (!ack)
                {
                    if (!ShowCustomDialog(sAdv.AdvancedWarningTitle, sAdv.AdvancedWarningBody, DialogType.Danger, true))
                    {
                        // User declined — go back to Home
                        var rbHome = (RadioButton)FindName("RBHome");
                        if (rbHome != null) rbHome.IsChecked = true;
                        return;
                    }
                    try { Reg.SetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AdvancedAck", 1); } catch { }
                }
            }

            _activeCategory = category;

            foreach (var (_, panelName, _) in Nav)
            {
                var p = (FrameworkElement)FindName(panelName);
                if (p != null) p.Visibility = Visibility.Collapsed;
            }

            string target = "PanelHome";
            foreach (var (_, panelName, cat) in Nav)
            {
                if (cat == category) { target = panelName; break; }
            }

            var tp = (FrameworkElement)FindName(target);
            if (tp != null)
            {
                tp.Visibility = Visibility.Visible;

                bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
                TimeSpan fadeDuration = anims ? TimeSpan.FromMilliseconds(250) : TimeSpan.Zero;
                TimeSpan slideDuration = anims ? TimeSpan.FromMilliseconds(350) : TimeSpan.Zero;

                var fade = new DoubleAnimation(0.0, 1.0, fadeDuration)
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                tp.BeginAnimation(OpacityProperty, fade);

                if (!(tp.RenderTransform is System.Windows.Media.TranslateTransform))
                {
                    tp.RenderTransform = new System.Windows.Media.TranslateTransform();
                }
                var slide = new DoubleAnimation(15.0, 0.0, slideDuration)
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                tp.RenderTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slide);
            }

            if (category == "Home")
            {
                UpdateLiveStats();
                TelemetryToggle_Click(TelemetryBtnCPU, null);
            }

            // Button visibilities based on tab
            if (category == "Home")
            {
                if (BtnApply != null) BtnApply.Visibility = Visibility.Collapsed;
                if (BtnUndo != null) BtnUndo.Visibility = Visibility.Collapsed;
                if (BtnInstall != null) BtnInstall.Visibility = Visibility.Collapsed;
                if (BtnDownload != null) BtnDownload.Visibility = Visibility.Collapsed;
                if (BtnSelectAll != null) BtnSelectAll.Visibility = Visibility.Collapsed;
                if (BtnDeselectAll != null) BtnDeselectAll.Visibility = Visibility.Collapsed;
            }
            else if (category == "Installer")
            {
                if (BtnApply != null) BtnApply.Visibility = Visibility.Collapsed;
                if (BtnUndo != null) BtnUndo.Visibility = Visibility.Collapsed;
                if (BtnInstall != null) BtnInstall.Visibility = Visibility.Visible;
                if (BtnDownload != null) BtnDownload.Visibility = Visibility.Collapsed;
                if (BtnSelectAll != null) BtnSelectAll.Visibility = Visibility.Collapsed;
                if (BtnDeselectAll != null) BtnDeselectAll.Visibility = Visibility.Collapsed;
            }
            else if (category == "Settings" || category == "SpeedTest")
            {
                if (BtnApply != null) BtnApply.Visibility = Visibility.Collapsed;
                if (BtnUndo != null) BtnUndo.Visibility = Visibility.Collapsed;
                if (BtnInstall != null) BtnInstall.Visibility = Visibility.Collapsed;
                if (BtnDownload != null) BtnDownload.Visibility = Visibility.Collapsed;
                if (BtnSelectAll != null) BtnSelectAll.Visibility = Visibility.Collapsed;
                if (BtnDeselectAll != null) BtnDeselectAll.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (BtnApply != null) BtnApply.Visibility = Visibility.Visible;
                if (BtnUndo != null) BtnUndo.Visibility = Visibility.Visible;
                if (BtnInstall != null) BtnInstall.Visibility = Visibility.Collapsed;
                if (BtnDownload != null) BtnDownload.Visibility = Visibility.Collapsed;
                if (BtnSelectAll != null) BtnSelectAll.Visibility = Visibility.Visible;
                if (BtnDeselectAll != null) BtnDeselectAll.Visibility = Visibility.Visible;
            }

            // LAZY BUILD: only build the panel when user navigates to it
            if (category != "Home" && category != "Installer" && category != "Settings" && category != "SpeedTest")
            {
                if (!_builtPanels.Contains(category))
                {
                    BuildPanel(category);
                    _builtPanels.Add(category);
                }
                else
                {
                    // Refresh states asynchronously when returning to a tab
                    DetectTweaksAsync(category);
                }
            }

            var s = I18n.Get(_lang);
            if (PageTitle != null) PageTitle.Text = category switch
            {
                "Home" => s.NavHome,
                "Privacy" => s.NavPrivacy,
                "Performance" => s.NavPerf,
                "Debloat" => s.NavDebloat,
                "Updates" => s.NavUpdates,
                "Network" => s.NavNetwork,
                "Internet" => s.NavInternet,
                "System" => s.NavSystem,
                "Security" => s.NavSecurity,
                "UI" => s.NavUI,
                "Appearance" => s.NavAppearance,
                "ContextMenu" => s.NavContext,
                "SpeedTest" => s.NavSpeedTest,
                "Installer" => s.NavInstaller,
                "Settings" => s.NavSettings,
                "Advanced" => s.NavAdvanced,
                _ => category,
            };

            int count = (category == "Home" || category == "Installer" || category == "Settings" || category == "SpeedTest")
                ? 0
                : Tweaks.All.Count(t => t.Category == category);

            if (TweakCount != null) TweakCount.Text = string.Format(s.TweaksCount, count);
            if (TweakCountBadge != null) TweakCountBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateStatus();
        }
        catch (Exception ex)
        {
            // v2.1: catch-all to prevent tab-click crashes
            Debug.WriteLine($"SwitchTo crashed for '{category}': {ex.Message}");
            try { SetStatus($"Error loading {category}: {ex.Message}"); } catch { }
        }
    }

    // ============================================================
    // BUILD PANEL (lazy — only called when user opens a category)
    // ============================================================
    void BuildPanel(string category)
    {
        try
        {
            var panelName = category switch
            {
                "Performance" => "PanelPerf",
                _ => "Panel" + category,
            };
            var panel = FindName(panelName) as ScrollViewer;
            if (panel == null) return;

            var grid = new UniformGrid { Columns = 3, VerticalAlignment = VerticalAlignment.Top };
            grid.BeginInit();
            foreach (var tweak in Tweaks.All.Where(t => t.Category == category))
            {
                try { grid.Children.Add(BuildCard(tweak)); }
                catch (Exception ex) { Debug.WriteLine($"BuildCard failed for {tweak.Id}: {ex.Message}"); }
            }
            grid.EndInit();
            panel.Content = grid;

            // Kick off background detection
            DetectTweaksAsync(category);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"BuildPanel failed for {category}: {ex.Message}");
        }
    }

    async System.Threading.Tasks.Task StartEarlyBackgroundDetectionAsync()
    {
        await System.Threading.Tasks.Task.Run(() =>
        {
            System.Threading.Tasks.Parallel.ForEach(Tweaks.All, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, t =>
            {
                try
                {
                    bool active = SafeDetect(t);
                    lock (_tweakActiveState)
                    {
                        _tweakActiveState[t.Id] = active;
                    }
                }
                catch { }
            });
        });
    }

    void DetectTweaksAsync(string category)
    {
        if (_detectCts != null)
        {
            try { _detectCts.Cancel(); } catch { }
            try { _detectCts.Dispose(); } catch { }
        }
        _detectCts = new System.Threading.CancellationTokenSource();
        var token = _detectCts.Token;

        var tweaksInCat = Tweaks.All.Where(t => t.Category == category).ToList();
        System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var t in tweaksInCat)
            {
                if (token.IsCancellationRequested) return;

                bool active = SafeDetect(t, token);

                if (token.IsCancellationRequested) return;

                lock (_tweakActiveState)
                {
                    _tweakActiveState[t.Id] = active;
                }

                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        var s = I18n.Get(_lang);
                        if (_checkboxes.TryGetValue(t.Id, out var cb))
                        {
                            _isProgrammaticChange = true;
                            cb.IsChecked = active;
                            _isProgrammaticChange = false;
                            cb.IsEnabled = true;
                            if (_cards.TryGetValue(t.Id, out var card))
                            {
                                SetCardVisual(card, cb, active);
                            }
                        }
                        if (_actionButtons.TryGetValue(t.Id, out var btn))
                        {
                            SetActionButtonContent(btn, active, s);
                            btn.IsEnabled = true;
                        }
                        UpdateStatus();
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DetectTweaksAsync dispatcher error for {t.Id}: {ex.Message}");
                }
            }
        });
    }

    void SetActionButtonContent(Button btn, bool active, I18n.Strings s)
    {
        if (btn == null) return;
        btn.Content = IconText(active ? "\xE7A6" : "\xE768", active ? s.BtnUndoSingle : s.BtnApplySingle, 9.5);
    }

    // Build an icon + label StackPanel for button content (clean, flat, no glow).
    static StackPanel IconText(string glyph, string text, double size = 11)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            FontFamily = Mdl2,
            FontSize = size,
            Text = glyph,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        sp.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        return sp;
    }

    // v2.6: Builds a button content with the NoveN logo image + text label.
    // Used for the "Run Optimizations" button (replaces the lightning bolt icon).
    static StackPanel LogoText(string text, double logoSize = 18, double fontSize = 13)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        try
        {
            var img = new System.Windows.Controls.Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Assets/logo.png")),
                Width = logoSize,
                Height = logoSize,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            };
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.HighQuality);
            sp.Children.Add(img);
        }
        catch { }
        sp.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });
        return sp;
    }

    void AnimateHoverLift(FrameworkElement card, double scale)
    {
        if (card == null) return;
        if (!(card.RenderTransform is ScaleTransform sf))
        {
            sf = new ScaleTransform(1, 1);
            card.RenderTransformOrigin = new Point(0.5, 0.5);
            card.RenderTransform = sf;
        }
        bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
        if (!anims)
        {
            sf.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            sf.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            sf.ScaleX = scale;
            sf.ScaleY = scale;
            return;
        }
        var dur = TimeSpan.FromMilliseconds(150);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        sf.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(scale, dur) { EasingFunction = ease });
        sf.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(scale, dur) { EasingFunction = ease });
    }

    static string GetTweakIcon(Tweak tweak)
    {
        return tweak.Id switch
        {
            // ───────── Privacy (27 tweaks) ─────────
            "Telemetry"               => "\xE9D9",  // Diagnostics
            "Cortana"                 => "\xE8D3",  // Search mic
            "Notifications"           => "\xE7E7",  // Bell
            "ActivityHist"            => "\xE81C",  // History clock
            "AdvertisingID"           => "\xE193",  // Tag
            "Location"                => "\xE819",  // Map pin
            "ErrorReport"             => "\xEBE8",  // Bug
            "Camera"                  => "\xE722",  // Camera
            "Microphone"              => "\xE720",  // Microphone
            "Feedback"                => "\xE939",  // Feedback smiley
            "WindowsTips"             => "\xEA80",  // Lightbulb
            "InputPersonal"           => "\xE765",  // Keyboard
            "ClipboardSync"           => "\xE8C8",  // Clipboard
            "CloudClipboard"          => "\xE753",  // Cloud
            "NearbySharing"           => "\xE72D",  // Share
            "LockScreenTips"          => "\xEE3F",  // Lock screen
            "ConsumerFeatures"        => "\xE719",  // Store bag
            "SpeechPrivacy"           => "\xEFA9",  // Voice/speech
            "TailoredExperiences"     => "\xE771",  // Paintbrush
            "SearchPermissions"       => "\xE721",  // Search
            "DisableRecentFiles"      => "\xE8A5",  // Recent
            "DisableFrequentFolders"  => "\xE8B7",  // Folder star
            "DisableDefenderCloudProt"=> "\xE783",  // Cloud block
            "DisableSmartScreen"      => "\xE8FB",  // Filter
            "DisableSuggestedContent" => "\xE7B3",  // Suggestion block
            "DisableWifiSense"        => "\xE701",  // Wifi
            "DisableWindowsSearchRep" => "\xE773",  // Shield

            // ───────── Performance (63 tweaks) ─────────
            "CustomPowerPlan"         => "\xE945",  // Speedometer
            "GameDVR"                 => "\xE7FC",  // Video
            "VBS"                     => "\xE74C",  // Virtualization
            "SysMain"                 => "\xE770",  // Service gear
            "DisableHibernation"      => "\xEA63",  // Sleep moon
            "DisablePrefetch"         => "\xEDA2",  // Preload off
            "SetHighPerfPlan"         => "\xE7E8",  // Lightning
            "UltimatePower"           => "\xE83E",  // Double lightning
            "CoreParking"             => "\xE950",  // CPU chip
            "CPUBoostMax"             => "\xEAFC",  // Rocket
            "PowerThrottle"           => "\xE8BE",  // Speed limit
            "NetworkThrottle"         => "\xE839",  // Network speed
            "MMCSSGaming"             => "\xE990",  // Game controller
            "HAGS"                    => "\xE7F4",  // GPU monitor
            "GPUTimeout"              => "\xE785",  // Timer alert
            "HyperV"                  => "\xEBC5",  // Layers
            "SpectreMeltdown"         => "\xE9CE",  // Microchip secure
            "FullscreenOpt"           => "\xE740",  // Fullscreen
            "USBPower"                => "\xEAF6",  // USB plug
            "RAMCompression"          => "\xE964",  // Compress
            "SvcHostSplit"            => "\xE713",  // Gear
            "Nagle"                   => "\xE968",  // Network delay
            "VisualFX"                => "\xE771",  // Brush
            "WinAnimations"           => "\xEC06",  // Motion
            "DisableTransparency"     => "\xEF1F",  // Opacity
            "BgApps"                  => "\xE71D",  // Apps grid
            "ProcScheduling"          => "\xE9D5",  // Processing
            "HPET"                    => "\xE823",  // Stopwatch
            "Hibernate"               => "\xE708",  // Power sleep
            "SearchIndex"             => "\xE721",  // Search magnify
            "XboxServices"            => "\xE7FC",  // Xbox
            "GameMode"                => "\xE7FC",  // Gamepad
            "NTFSLastAccess"          => "\xE8D7",  // File timestamp
            "Prefetch"                => "\xE896",  // Prefetch cache
            "StartupDelay"            => "\xE916",  // Clock fast
            "DisableWakeTimers"       => "\xE81A",  // Alarm off
            "ModernStandby"           => "\xE904",  // Modern sleep
            "DisableDefragTask"       => "\xECAD",  // Defrag off
            "EnableFastStartup"       => "\xE7E8",  // Fast boot
            "MouseFix"                => "\xE962",  // Mouse cursor
            "AudioEnhance"            => "\xE767",  // Speaker
            "CDPService"              => "\xEBD2",  // Connected devices
            "CleanStartup"            => "\xE8A7",  // Clean sweep
            "DiagPolicy"              => "\xE9D9",  // Diagnostics
            "DisableAutoMaintenance"  => "\xE90F",  // Maintenance
            "DisableBackgroundApps"   => "\xE71E",  // App bg
            "DisableGameBarPresenceSvc"=> "\xE990", // Game presence
            "DisablePagingExecutive"  => "\xE964",  // Memory exec
            "DisablePCIeASPM"         => "\xEAF6",  // PCIe power
            "DisableTimerCoalescing"  => "\xE916",  // Timer merge
            "DisableUSBSuspendPower"  => "\xEAF6",  // USB power
            "ForegroundBoost"         => "\xE8A7",  // Foreground
            "FSO"                     => "\xE740",  // Fullscreen opt
            "GameDVRFullDisable"      => "\xE714",  // Video off
            "GPUPriority"             => "\xE7F4",  // GPU priority
            "HWScheduling"            => "\xE950",  // Hardware sched
            "IdleOptimize"            => "\xEA63",  // Idle
            "IRPStack"                => "\xE968",  // IO stack
            "MMCSSResponsiveness"     => "\xE9D5",  // Responsive
            "MPODisable"              => "\xE81E",  // Overlay
            "NagleDisable"            => "\xF12C",  // Nagle off
            "NETOptimize"             => "\xE909",  // .NET
            "NetworkRSS"              => "\xE839",  // RSS
            "PowerThrottling"         => "\xE945",  // Throttle
            "ProcessPriorityAuto"     => "\xE7FD",  // Priority auto
            "TimerResolution"         => "\xE823",  // Timer

            // ───────── Debloat (16 tweaks) ─────────
            "Bloatware"               => "\xE74D",  // Trash can
            "OneDrive"                => "\xE753",  // Cloud
            "Teams"                   => "\xE8BD",  // Chat people
            "Widgets"                 => "\xE71D",  // Grid apps
            "RemoveRecall"            => "\xE81C",  // History recall
            "RemoveCopilot"           => "\xE99A",  // Robot
            "TelemetryTasks"          => "\xE9D9",  // Tasks diag
            "InkWorkspace"            => "\xEDC6",  // Pen ink
            "InsiderService"          => "\xEA8A",  // People insider
            "SearchUIBg"              => "\xE721",  // Search bg
            "OptionalFeatures"        => "\xE8A9",  // Features list
            "RemoveInboxApps"         => "\xE74D",  // Remove apps
            "DisableOneDriveStartup"  => "\xE8F8",  // Cloud off
            "ExtraTelemetryTasks"     => "\xE770",  // Extra tasks
            "RemoveBingApps"          => "\xE8FB",  // Bing remove
            "RemoveMoreBloat"         => "\xE894",  // More cleanup

            // ───────── Updates (8 tweaks) ─────────
            "WUPause"                 => "\xE769",  // Pause
            "WUDriver"                => "\xEA6C",  // Driver chip
            "WURestart"               => "\xE777",  // Restart
            "WUDisable"               => "\xE711",  // Cancel/stop
            "DeliveryOpt"             => "\xE702",  // Download
            "StoreAutoUpdates"        => "\xE719",  // Store
            "AutoRestartSignOn"       => "\xEE57",  // Profile login
            "DisableMaintenanceWakeUp"=> "\xE81A",  // Alarm off

            // ───────── Network (16 tweaks) ─────────
            "FastDNS"                 => "\xE774",  // Globe fast
            "NetTCP"                  => "\xE839",  // Wired speed
            "QoS"                     => "\xE945",  // Speedometer
            "RemoteAssist"            => "\xEE35",  // Remote assist
            "DisableIPv6"             => "\xE8F8",  // IPv6 off
            "DisableLLMNR"            => "\xE711",  // LLMNR off
            "FlushDNS"                => "\xE8A7",  // Clean DNS
            "NICPower"                => "\xEAF6",  // NIC power
            "NetBIOS"                 => "\xF12C",  // NetBIOS
            "WPAD"                    => "\xE721",  // WPAD search
            "SMBSigning"              => "\xE72E",  // Lock/sign
            "DisableNetworkWizard"    => "\xE8FB",  // Wizard off
            "DisableIPHelper"         => "\xE8D8",  // IP helper off
            "DisableLmhosts"          => "\xE8A9",  // LM hosts
            "DisableNetDiscovery"     => "\xE8C9",  // Discovery off
            "TCPChimney"              => "\xE968",  // TCP chimney

            // ───────── Internet (20 tweaks) ─────────
            "DNSAdvanced"             => "\xE774",  // DNS advanced
            "DnsCacheOptimize"        => "\xE895",  // Cache optimize
            "DNSRefresh"              => "\xE72C",  // DNS refresh
            "GamingDNS"               => "\xE990",  // Gaming DNS
            "IPv4Optimize"            => "\xE839",  // IPv4
            "IPv6Optimize"            => "\xE117",  // IPv6
            "IRPStackSize"            => "\xE964",  // IRP stack
            "NetBindingClean"         => "\xE8A7",  // Binding clean
            "NetLatencyOptimize"      => "\xE916",  // Latency
            "NetworkPrivate"          => "\xE72E",  // Private net
            "NetworkThrottleInternet" => "\xE945",  // Throttle net
            "NICAdvanced"             => "\xEAF6",  // NIC advanced
            "NICOffload"              => "\xE896",  // NIC offload
            "ProxyReset"              => "\xE8FB",  // Proxy reset
            "SMBAdvanced"             => "\xE8D8",  // SMB advanced
            "TCPAdvanced"             => "\xE968",  // TCP advanced
            "TCPFastOpen"             => "\xE7E8",  // TCP fast open
            "WakeOnMagic"             => "\xE7E8",  // Wake on LAN
            "WiFiOptimize"            => "\xE701",  // WiFi
            "DisableSMB1"             => "\xE711",  // SMB1 disable

            // ───────── System (37 tweaks) ─────────
            "ClearPageFileAtShutdown" => "\xE8A7",  // Clean pagefile
            "DisableStorageSense"     => "\xEDA2",  // Storage off
            "TempClean"               => "\xE74D",  // Temp trash
            "OldWU"                   => "\xE72C",  // Old updates
            "ShutdownFast"            => "\xE7E8",  // Fast shutdown
            "HighResTimer"            => "\xE823",  // High res timer
            "LongPaths"               => "\xE8B7",  // Long paths
            "NumLock"                 => "\xE765",  // NumLock key
            "CleanEventLog"           => "\xE9F9",  // Event log
            "SetUTC"                  => "\xE916",  // UTC clock
            "PrintSpooler"            => "\xE956",  // Printer
            "ThumbnailCache"          => "\xE8B9",  // Thumbnail
            "ExplorerSearch"          => "\xE721",  // Explorer search
            "OpenExplorerToThisPC"    => "\xE8B7",  // This PC
            "Fax"                     => "\xE715",  // Fax
            "EnableVerboseBoot"       => "\xEA37",  // Terminal log
            "AeroShakeDisable"        => "\xEC06",  // Shake motion
            "ExtraServices"           => "\xE770",  // Services gear
            "CrashDump"               => "\xEBE8",  // Crash dump
            "AdminConsole"            => "\xE756",  // Admin console
            "BootTimeout"             => "\xE81A",  // Boot timeout
            "ChangeResolution"        => "\xE7F4",  // Resolution
            "ClearUserAssist"         => "\xE894",  // Clear user
            "Disable8Dot3Names"       => "\xE8D7",  // 8.3 names
            "DisableAutoAdminLogon"   => "\xEE57",  // Admin logon
            "DisableCrashDump"        => "\xE711",  // Crash off
            "FileHashCheck"           => "\xE8FD",  // Hash check
            "GodMode"                 => "\xE713",  // God mode
            "OpenGitBashHere"         => "\xE756",  // Git Bash
            "OpenWSLHere"             => "\xE756",  // WSL
            "CopyAsPath"              => "\xE8C8",  // Copy as path
            "ImportPowerplan"         => "\xE8B5",  // Import power
            "KillTasks"               => "\xE711",  // Kill tasks
            "RebootToBIOS"            => "\xE950",  // BIOS reboot
            "RestartAppsAfterLogin"   => "\xE72C",  // Restart apps
            "RunWithPriority"         => "\xE7FD",  // Run priority
            "SetNTPServer"            => "\xE968",  // NTP server
            "ShutdownOptions"         => "\xE7E8",  // Shutdown opt
            "TakeOwnership"           => "\xE72E",  // Take own
            "WindowsTools"            => "\xE90F",  // Win tools

            // ───────── Security (17 tweaks) ─────────
            "DisableSMBv1"            => "\xE711",  // Block SMBv1
            "DisableAutoPlay"         => "\xE769",  // Pause auto
            "Firewall"                => "\xE83D",  // Firewall shield
            "DefenderPUA"             => "\xEBE8",  // Bug PUA
            "DisableGuest"            => "\xEE57",  // User off
            "DisableRDP"              => "\xE8A7",  // Remote off
            "UAC"                     => "\xE74C",  // UAC shield
            "RemoteRegistry"          => "\xE8D8",  // Registry
            "NTLMv2Hardening"         => "\xE72E",  // Lock/harden
            "AuditLogon"              => "\xE8FD",  // Audit log
            "DisableAnonymousSAM"     => "\xE77B",  // User block
            "NullSessionRestrict"     => "\xE785",  // Session restrict
            "DisableICMPRedirects"    => "\xE839",  // ICMP
            "DisableIPSourceRouting"  => "\xE774",  // IP routing
            "DisableWDigest"          => "\xE8B9",  // WDigest
            "EnableLSAProtection"     => "\xE773",  // LSA shield
            "SYNAttackProtect"        => "\xE968",  // SYN protect

            // ───────── UI (29 tweaks) ─────────
            "Hide3DObjects"           => "\xE8B7",  // Folder hide
            "ForceNetworkTrayIcon"    => "\xE839",  // Tray icon
            "AlwaysUnloadDLL"         => "\xE964",  // DLL unload
            "AppPermissions"          => "\xE72E",  // Permissions
            "ClassicTaskbar"          => "\xE8FB",  // Taskbar
            "Compact"                 => "\xE7EA",  // Compact mode
            "ContentDeliveryExtra"    => "\xE8FB",  // Content block
            "CursorBlink"             => "\xE962",  // Cursor
            "DisableCapsLock"         => "\xE765",  // Caps off
            "DisableFilterKeys"       => "\xE765",  // Filter keys
            "DisableFolderTypeDetect" => "\xE8B7",  // Folder type
            "DisableRecycleBinConfirm"=> "\xE74D",  // Recycle
            "DisableShellAnimation"   => "\xEC06",  // Anim off
            "DisableShortcutArrow"    => "\xE71B",  // Arrow off
            "DisableShortcutSuffix"   => "\xE8D7",  // Suffix
            "DisableStickyKeys"       => "\xE7B2",  // Sticky keys
            "DisableWindowGhost"      => "\xE8C1",  // Ghost window
            "DisableWindowsSounds"    => "\xE74F",  // Sound off
            "LowDiskAlert"            => "\xEDA2",  // Disk alert
            "MenuDelay"               => "\xE916",  // Menu speed
            "RestartExplorer"         => "\xE72C",  // Restart
            "SearchHighlights"        => "\xE721",  // Highlights
            "SearchPrivacy"           => "\xE773",  // Search privacy
            "ShowExtensions"          => "\xE8D7",  // Extensions
            "ShowHidden"              => "\xE7B3",  // Hidden files
            "ShowProtectedFiles"      => "\xE72E",  // Protected
            "SnapAssist"              => "\xE8A0",  // Snap
            "TaskbarAnimations"       => "\xEC06",  // Taskbar anim
            "TaskbarClean"            => "\xE8A7",  // Clean taskbar

            // ───────── Appearance (5 tweaks) ─────────
            "DarkMode"                => "\xE793",  // Dark/moon
            "MousePrecision"          => "\xE962",  // Mouse
            // VisualFX, WinAnimations, DisableTransparency handled in Performance above (shared IDs)

            // ───────── ContextMenu (13 tweaks) ─────────
            "OldContextMenu"          => "\xE8AB",  // Context menu
            "OpenCMDHere"             => "\xE756",  // CMD
            "OpenPSHere"              => "\xEA37",  // PowerShell
            "OpenTerminalHere"        => "\xE756",  // Terminal
            "OpenVSCodeHere"          => "\xE943",  // VS Code
            "CopyToFolder"            => "\xE8C8",  // Copy
            "MoveToFolder"            => "\xE8DE",  // Move
            "GodModeDesktop"          => "\xE713",  // God mode
            "EditNotepadPP"           => "\xE70F",  // Edit
            "RunAsUserMenu"           => "\xEE57",  // User
            "WallpaperHere"           => "\xEB9F",  // Wallpaper
            "ChecksumMenu"            => "\xE8FD",  // Checksum
            "ContextMenuExtras"       => "\xE712",  // Extras

            // ───────── New optimization tweaks ─────────
            "DisableSleepStudy"       => "\xEA63",  // Sleep moon
            "DisableRSC"              => "\xE839",  // Network offload
            "DNSoverHTTPS"            => "\xE774",  // Secure globe
            "EnableControlledFolderAccess" => "\xE73E", // Ransomware guard
            "EnableNetworkProtection" => "\xE773",  // Network shield
            "DisablePrintingOverHTTP" => "\xE956",  // Printer
            "ClearPrefetch"           => "\xE896",  // Prefetch cache
            "ClearTempOnBoot"         => "\xE8A7",  // Clean sweep
            "ClearIconCache"          => "\xE8B9",  // Image cache
            "DisableStartupSound"     => "\xE74F",  // Sound off
            "DisableFindMyDevice"     => "\xE819",  // Location off
            "DisableSettingSync"      => "\xE714",  // Sync off
            "DisableCameraOnLockScreen"=> "\xE722", // Camera
            "DisableTouchKeyboardAutoInvoke" => "\xE765", // Keyboard
            "DisableLockScreenSlideshow" => "\xEE3F", // Lock screen
            "DisableCopilotPolicy"    => "\xE742", // Robot/Copilot
            "DisableRecallPolicy"     => "\xE7C4", // AI brain
            "ShowThisPC"              => "\xE7F8", // PC/monitor
            "TaskbarWidgetsOff"       => "\xE735", // Widget tile
            "TaskbarSearchIcon"       => "\xE721", // Search
            "DisableLockScreenAds"    => "\xE790", // Ad/block

            _ => CatIcons.TryGetValue(tweak.Category, out var cIcon) ? cIcon : "\xE713"
        };
    }

    Border BuildCard(Tweak tweak)
    {
        bool compact = TglCompact != null ? TglCompact.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "CompactMode") ?? 0) == 1;

        var card = new Border
        {
            Style = (Style)Resources["CardStyle"],
            Margin = compact ? new Thickness(3, 2, 3, 2) : new Thickness(4),
            Background = BgOff,
            BorderBrush = BorderOff,
            MinHeight = compact ? 100 : 118,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        if (compact)
        {
            card.Padding = new Thickness(8, 6, 8, 6);
        }

        var inner = new Grid();
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Top, Margin = compact ? new Thickness(0, 0, 8, 0) : new Thickness(0, 0, 10, 0) };
        var iconRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = compact ? new Thickness(0, 0, 0, 2) : new Thickness(0, 0, 0, 3) };

        // v2.8: Icon in a subtle rounded background container for a cleaner look
        var iconBg = new Border
        {
            Background = (Brush)Application.Current.Resources["SurfaceMid"],
            CornerRadius = new CornerRadius(5),
            Width = compact ? 20 : 22,
            Height = compact ? 20 : 22,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        iconBg.Child = new TextBlock
        {
            FontFamily = Mdl2, FontSize = compact ? 10 : 11, Foreground = FgIcon,
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
            Text = GetTweakIcon(tweak),
        };
        iconRow.Children.Add(iconBg);

        var titleTb = new TextBlock
        {
            FontSize = compact ? 11 : 12, FontWeight = FontWeights.SemiBold, Foreground = FgTitle,
            TextWrapping = TextWrapping.Wrap, Text = Tweaks.Name(tweak, (int)_lang),
            VerticalAlignment = VerticalAlignment.Center,
        };
        iconRow.Children.Add(titleTb);

        // Circled Info Icon ("i") next to Title, triggers description dialog
        var infoIcon = new TextBlock
        {
            Text = "\xE946", // Circled Info (i) symbol in Segoe MDL2 Assets
            FontFamily = Mdl2,
            FontSize = compact ? 10.5 : 12,
            Foreground = FgIcon,
            Opacity = 0.5,
            Cursor = Cursors.Hand,
            Margin = new Thickness(7, 1, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        infoIcon.MouseEnter += (s, e) => infoIcon.Opacity = 1.0;
        infoIcon.MouseLeave += (s, e) => infoIcon.Opacity = 0.5;
        infoIcon.MouseDown += (s, e) =>
        {
            e.Handled = true;
            PlaySoundEffect();
            ShowTweakInfoDialog(tweak);
        };
        iconRow.Children.Add(infoIcon);

        left.Children.Add(iconRow);

        var descTb = new TextBlock
        {
            FontSize = compact ? 9.5 : 9.5, Foreground = FgDesc, TextWrapping = TextWrapping.Wrap,
            Margin = compact ? new Thickness(0, 2, 0, 0) : new Thickness(0, 2, 0, 0), LineHeight = compact ? 13 : 14,
            Text = Tweaks.Desc(tweak, (int)_lang),
        };
        left.Children.Add(descTb);

        var noteText = Tweaks.Note(tweak, (int)_lang);
        if (tweak.Warning && string.IsNullOrWhiteSpace(noteText))
        {
            var s = I18n.Get(_lang);
            var warnBorder = new Border
            {
                Background = WarnYellowBg, CornerRadius = new CornerRadius(6),
                Margin = compact ? new Thickness(0, 4, 0, 0) : new Thickness(0, 8, 0, 0), 
                Padding = compact ? new Thickness(0, 4, 0, 4) : new Thickness(0, 8, 0, 8),
            };
            var warnGrid = new Grid();
            warnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            warnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var pill = new Rectangle
            {
                Width = 3, Fill = WarnYellow, Margin = new Thickness(0, 0, 10, 0),
                RadiusX = 1.5, RadiusY = 1.5, VerticalAlignment = VerticalAlignment.Stretch,
            };
            Grid.SetColumn(pill, 0);
            var warnStack = new StackPanel();
            Grid.SetColumn(warnStack, 1);
            var hdr = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            hdr.Children.Add(new TextBlock { FontFamily = Mdl2, FontSize = 13, Foreground = WarnYellow, Text = "\xE7BA", Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            hdr.Children.Add(new TextBlock { FontSize = 12, FontWeight = FontWeights.Bold, Foreground = WarnYellow, Text = s.WarnTitle, VerticalAlignment = VerticalAlignment.Center });
            warnStack.Children.Add(hdr);
            warnStack.Children.Add(new TextBlock { FontSize = 11, Foreground = FgWarnText, TextWrapping = TextWrapping.Wrap, Text = s.WarnDesc });
            warnGrid.Children.Add(pill);
            warnGrid.Children.Add(warnStack);
            warnBorder.Child = warnGrid;
            left.Children.Add(warnBorder);
        }

        // Researched note (e.g. anti-cheat incompatibility) shown under the description
        if (!string.IsNullOrWhiteSpace(noteText))
        {
            var noteBorder = new Border
            {
                Background = (Brush)Application.Current.Resources["SurfaceLow"],
                CornerRadius = new CornerRadius(6),
                Margin = compact ? new Thickness(0, 4, 0, 0) : new Thickness(0, 8, 0, 0),
                Padding = compact ? new Thickness(8, 4, 8, 4) : new Thickness(10, 6, 10, 7),
                BorderBrush = (Brush)Application.Current.Resources["BorderSoft"],
                BorderThickness = new Thickness(1),
            };
            var noteGrid = new Grid();
            noteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            noteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var ni = new TextBlock
            {
                FontFamily = Mdl2, FontSize = 12, Text = "\xEA80",
                Foreground = (Brush)Application.Current.Resources["AccentFg"],
                VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 0, 8, 0),
            };
            Grid.SetColumn(ni, 0);
            var nt = new TextBlock
            {
                FontSize = 10, TextWrapping = TextWrapping.Wrap, LineHeight = 15,
                Foreground = (Brush)Application.Current.Resources["FgMid"],
                Text = noteText,
            };
            Grid.SetColumn(nt, 1);
            noteGrid.Children.Add(ni);
            noteGrid.Children.Add(nt);
            noteBorder.Child = noteGrid;
            left.Children.Add(noteBorder);
            _noteTbs[tweak.Id] = nt;
        }

        // Add a premium action button for single tweaks (fits the design, uses BtnSec style)
        // v2.8: Improved button styling — rounded, subtle border
        var sCurrent = I18n.Get(_lang);
        var actBtn = new Button
        {
            Style = (Style)Resources["BtnSec"],
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = compact ? new Thickness(0, 5, 0, 0) : new Thickness(0, 9, 0, 0),
            Padding = compact ? new Thickness(10, 3, 10, 3) : new Thickness(12, 5, 12, 5),
            Height = compact ? 22 : 26,
            FontSize = compact ? 9.5 : 11,
            Cursor = Cursors.Hand,
            IsEnabled = false // Disabled until async detection finishes
        };
        SetActionButtonContent(actBtn, false, sCurrent);

        // App-picker tweaks (bloatware) open a custom dialog instead of toggling
        bool isAppPicker = IsAppPickerTweak(tweak.Id);
        if (isAppPicker)
        {
            // Enable immediately and set custom label
            actBtn.IsEnabled = true;
            actBtn.Content = IconText("\xE74D", "Select Apps", compact ? 9.5 : 11);
            actBtn.Click += (_, _) =>
            {
                PlaySoundEffect();
                _ = ShowAppPickerDialog(Tweaks.Name(tweak, (int)_lang));
            };
        }
        else
        {
            actBtn.Click += (_, _) =>
            {
                bool isActive = false;
                lock (_tweakActiveState)
                {
                    _tweakActiveState.TryGetValue(tweak.Id, out var val);
                    isActive = val == true;
                }
                RunSingleTweak(tweak, !isActive);
            };
        }
        left.Children.Add(actBtn);
        _actionButtons[tweak.Id] = actBtn;

        Grid.SetColumn(left, 0);
        inner.Children.Add(left);

        var cb = new CheckBox
        {
            Style = (Style)Resources["ToggleStyle"],
            VerticalAlignment = VerticalAlignment.Center,
            IsEnabled = !isAppPicker // App-picker tweaks enabled immediately; others wait for detection
        };
        Grid.SetColumn(cb, 1);
        inner.Children.Add(cb);

        if (isAppPicker)
        {
            // For app-picker tweaks, clicking the checkbox also opens the dialog
            cb.Click += (_, _) =>
            {
                // Prevent the checkbox from actually toggling — it's just a launcher
                cb.IsChecked = false;
                PlaySoundEffect();
                _ = ShowAppPickerDialog(Tweaks.Name(tweak, (int)_lang));
            };
        }
        else
        {
            cb.Checked += (sender, _) =>
            {
                if (!_isProgrammaticChange) // Warn only on manual user toggling
                {
                    if (tweak.Id == "Microphone")
                    {
                        var s0 = I18n.Get(_lang);
                        string micWarn = _lang == I18n.Language.PL
                            ? "UWAGA: Wyłączenie mikrofonu zablokuje dostęp do wejścia audio dla WSZYSTKICH aplikacji (w tym Discord, Teams, Skype, Zoom, OBS oraz czatów w grach). Czy na pewno chcesz wyłączyć mikrofon?"
                            : "WARNING: Disabling the microphone will block audio input access for ALL applications (including Discord, Teams, Skype, Zoom, OBS, and in-game voice chat). Are you sure you want to disable the microphone?";

                        if (!ShowCustomDialog(s0.WarnTitle, micWarn, DialogType.Warning, true))
                        {
                            cb.IsChecked = false; // Cancel checking
                            return;
                        }
                    }
                    else if (tweak.Id == "Camera")
                    {
                        var s0 = I18n.Get(_lang);
                        string camWarn = _lang == I18n.Language.PL
                            ? "UWAGA: Wyłączenie kamery zablokuje dostęp do kamery internetowej dla WSZYSTKICH aplikacji (w tym Discord, Teams, Zoom, OBS i przeglądarek). Czy na pewno chcesz wyłączyć kamerę?"
                            : "WARNING: Disabling the camera will block webcam access for ALL applications (including Discord, Teams, Zoom, OBS, and browsers). Are you sure you want to disable the camera?";

                        if (!ShowCustomDialog(s0.WarnTitle, camWarn, DialogType.Warning, true))
                        {
                            cb.IsChecked = false; // Cancel checking
                            return;
                        }
                    }
                }
                SetCardVisual(card, cb, true);
                // Only play sound on genuine user interaction (not programmatic detect/pre-build)
                if (!_isProgrammaticChange) PlaySoundEffect();
            };
            cb.Unchecked += (_, _) =>
            {
                SetCardVisual(card, cb, false);
                if (!_isProgrammaticChange) PlaySoundEffect();
            };
        }

        // Hover effects only (no scale zoom to prevent text blurring in WPF)
        card.MouseEnter += (_, _) => { if (cb.IsChecked != true) { card.Background = BgHover; card.BorderBrush = BgHoverBorder; } };
        card.MouseLeave += (_, _) => { if (cb.IsChecked != true) { card.Background = BgOff; card.BorderBrush = BorderOff; } };

        card.Child = inner;
        _checkboxes[tweak.Id] = cb;
        _cards[tweak.Id]      = card;
        _titleTbs[tweak.Id]   = titleTb;
        _descTbs[tweak.Id]    = descTb;
        return card;
    }

    static void SetCardVisual(Border card, CheckBox cb, bool on)
    {
        if (on) { card.BorderBrush = BorderOn; card.Background = BgOn; }
        else { card.BorderBrush = BorderOff; card.Background = BgOff; }
    }

    // ── Detect cache (v2.0 performance optimization) ──
    // Caching detect results avoids re-running expensive PowerShell/process
    // queries every time the UI refreshes. Cache is invalidated on apply/undo.
    readonly Dictionary<string, (bool value, DateTime ts)> _detectCache = new();
    static readonly TimeSpan _detectCacheTtl = TimeSpan.FromSeconds(5);

    void InvalidateDetectCache(string id = null)
    {
        lock (_detectCache)
        {
            if (id != null) _detectCache.Remove(id);
            else _detectCache.Clear();
        }
    }

    bool SafeDetect(Tweak t, System.Threading.CancellationToken token = default)
    {
        if (t == null) return false;
        if (token.IsCancellationRequested) return false;
        lock(_appliedTweaks) { if (_appliedTweaks.Contains(t.Id)) return true; }
        // Check cache first
        lock (_detectCache)
        {
            if (_detectCache.TryGetValue(t.Id, out var entry))
            {
                if (DateTime.Now - entry.ts < _detectCacheTtl)
                    return entry.value;
            }
        }
        if (token.IsCancellationRequested) return false;
        bool result;
        try { result = t.Detect?.Invoke() ?? false; } catch { result = false; }
        lock (_detectCache) { _detectCache[t.Id] = (result, DateTime.Now); }
        return result;
    }

    void RunSingleTweak(Tweak tweak, bool apply)
    {
        var s = I18n.Get(_lang);

        // v2.8: Elevation check — most tweaks need admin (HKLM, system commands).
        // App-picker tweaks (bloatware removal) also need admin for all-users removal.
        if (!ElevationHelper.IsElevated)
        {
            if (ShowElevationDialog(s))
            {
                if (ElevationHelper.RestartAsAdmin())
                    Application.Current.Shutdown();
                return;
            }
            else
            {
                SetStatus(s.ElevCancel);
                return;
            }
        }

        SetStatus(apply ? s.StatusApply : s.StatusUndo);
        ShowTopProgress(apply ? s.StatusApply : s.StatusUndo, -1); // indeterminate for single tweak

        // v2.8: For device-critical OR destructive tweaks, do extra safety check
        // v2.9: Also check IsDestructiveTweak (was dead code before)
        if (apply && (SafetyGuard.IsDeviceCriticalTweak(tweak.Id) || SafetyGuard.IsDestructiveTweak(tweak.Id)))
        {
            var (ok, reason) = SafetyGuard.VerifySafeToApplyNow();
            if (!ok)
            {
                HideTopProgress();
                ShowCustomDialog(s.WarnTitle, reason + "\n\n" + (s.DeviceCriticalWarn ?? ""), DialogType.Warning, false);
                return;
            }
            
            // Add explicit warning for Camera / Microphone toggles
            if (tweak.Id == "Microphone" || tweak.Id == "Camera")
            {
                string warnMsg = "";
                if (tweak.Id == "Microphone")
                {
                    warnMsg = _lang == I18n.Language.PL
                        ? "UWAGA: Wyłączenie mikrofonu zablokuje dostęp do wejścia audio dla WSZYSTKICH aplikacji (w tym Discord, Teams, Skype, Zoom, OBS oraz czatów w grach). Czy na pewno chcesz wyłączyć mikrofon?"
                        : "WARNING: Disabling the microphone will block audio input access for ALL applications (including Discord, Teams, Skype, Zoom, OBS, and in-game voice chat). Are you sure you want to disable the microphone?";
                }
                else
                {
                    warnMsg = _lang == I18n.Language.PL
                        ? "UWAGA: Wyłączenie kamery zablokuje dostęp do kamery internetowej dla WSZYSTKICH aplikacji (w tym Discord, Teams, Zoom, OBS i przeglądarek). Czy na pewno chcesz wyłączyć kamerę?"
                        : "WARNING: Disabling the camera will block webcam access for ALL applications (including Discord, Teams, Zoom, OBS, and browsers). Are you sure you want to disable the camera?";
                }
                if (!ShowCustomDialog(s.WarnTitle, warnMsg, DialogType.Warning, true))
                {
                    HideTopProgress();
                    return; // User cancelled
                }
            }

            // Backup device-critical registry keys before destructive tweak
            try { SafetyGuard.BackupDeviceCriticalKeys(); } catch { }
        }

        // Disable both input controls
        if (_checkboxes.TryGetValue(tweak.Id, out var cb)) cb.IsEnabled = false;
        if (_actionButtons.TryGetValue(tweak.Id, out var btn)) btn.IsEnabled = false;

        bool skipRP3 = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SkipRestorePoints") ?? 0) == 1;
        System.Threading.Tasks.Task.Run(() =>
        {
            bool success = false;
            try
            {
                // v2.1: Ensure a System Restore point exists before any apply.
                // v1.4: Skip if user disabled restore points in settings.
                if (apply && !skipRP3 && SafetyGuard.NeedsRestorePoint(tweak.Id))
                {
                    try { SafetyGuard.EnsureRestorePoint(); } catch { }
                }
                success = apply ? tweak.Apply() : tweak.Undo();
                // v2.9: Log to developer console
                try { DevConsole.LogTweak(tweak.Id, apply ? "Apply" : "Undo", success); } catch { }
                if (success)
                {
                    lock(_appliedTweaks) { if (apply) _appliedTweaks.Add(tweak.Id); else _appliedTweaks.Remove(tweak.Id); }
                    Dispatcher.Invoke(SaveAppliedTweaks); InvalidateDetectCache();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Single tweak run {tweak.Id} error: {ex.Message}");
            }
            
            bool active = SafeDetect(tweak);
            lock (_tweakActiveState)
            {
                _tweakActiveState[tweak.Id] = active;
            }
            
            Dispatcher.Invoke(() =>
            {
                if (cb != null)
                {
                    _isProgrammaticChange = true;
                    cb.IsChecked = active;
                    _isProgrammaticChange = false;
                    cb.IsEnabled = true;
                }
                if (btn != null)
                {
                    // Don't overwrite the "Select Apps" label for app-picker tweaks
                    if (!IsAppPickerTweak(tweak.Id))
                        SetActionButtonContent(btn, active, s);
                    btn.IsEnabled = true;
                }
                if (_cards.TryGetValue(tweak.Id, out var card))
                {
                    SetCardVisual(card, cb, active);
                }
                UpdateStatus();
                
                // If search is currently open, refresh search results so the search row updates too
                if (_searchOpen && SearchBox != null)
                {
                    DoSearch(SearchBox.Text);
                }
                
                if (success)
                {
                    SetStatus(s.StatusDone);
                }
                else
                {
                    SetStatus(s.StatusFailed);
                    ShowCustomDialog(s.WarnTitle, s.ApplyError, DialogType.Error, false);
                }
                HideTopProgress();
            });
        });
    }

    // ============================================================
    // FOOTER BUTTONS
    // ============================================================
    void WireFooterButtons()
    {
        BtnApply.Click += (_, _) => DoApply();
        BtnUndo.Click += (_, _) => DoUndo();
        BtnSelectAll.Click += (_, _) => SelectAll(true);
        BtnDeselectAll.Click += (_, _) => SelectAll(false);
    }

    // ============================================================
    // SMART OPTIMIZER WIZARD  ("Start Optimizing" on Home)
    // ============================================================
    readonly List<Tweak> _wizRecommended = new();
    readonly List<CheckBox> _wizChecks = new();
    readonly List<Tweak> _wizAdvanced = new();
    readonly List<CheckBox> _wizAdvancedChecks = new();

    // ─────────────────────────────────────────────────────────────
    // OPTIMIZATION WIZARD SETS (v2.1 — completely rewritten)
    //
    // Three clear levels with distinct purposes:
    //   • MINIMAL  — ~12 safe tweaks. Privacy + reduce background processes.
    //                Zero impact on daily use. Safe for any PC.
    //   • NORMAL   — Balanced performance. Safe gaming/latency tweaks.
    //                Recommended for most users. No stability risk.
    //   • FULLOPTI — Maximum performance. May be unstable. Advanced users.
    //                Includes aggressive tweaks (NOT the harmful ones though).
    // ─────────────────────────────────────────────────────────────

    // Tweaks that HURT performance — NEVER auto-selected, only in Advanced list.
    static readonly HashSet<string> HarmfulTweaks = new()
    {
        // v2.9 FIX: Corrected tweak IDs to match actual registered IDs
        "ClearPageFileAtShutdown", "AlwaysUnloadDLL", "SysMain",
        "DisableWSearch", "DisableHibernation", "DisableDefragTask",
        "DisableMemoryCompression", "DisableSwapFile", "EnableFastStartup",
        "PrintSpooler", "DisableWcmsvc", "DisableWcnSvc", "DisableDusmSvc", "DisableDot3Svc", "WiFiOptimize",
    };

    // Security-risk tweaks — only in Advanced list, never auto-selected.
    static readonly HashSet<string> SecurityRiskTweaks = new()
    {
        // v2.9 FIX: Corrected tweak IDs (were "DisableSpectreMeltdown", "DisableVBS" etc.)
        "SpectreMeltdown", "VBS", "DisableSmartScreen",
        "DisableDefenderCloudProt",
    };

    // One-shot actions with no persistent state — exclude from wizard entirely.
    static readonly HashSet<string> OneShotTweaks = new()
    {
        "KillTasks", "WindowsTools", "ImportPowerplan", "RebootToBIOS",
        "ScanWindowsUpdate", "ClearPrefetch", "ClearIconCache",
        "FlushDNS", "TempClean", "NETOptimize", "DNSRefresh",
        "CleanEventLog", "RestartExplorer", "NetBindingClean",
        "NetworkPrivate", "OptionalFeatures", "CleanStartup",
    };

    bool IsHarmfulTweak(string id)
        => HarmfulTweaks.Contains(id) || OneShotTweaks.Contains(id) || SecurityRiskTweaks.Contains(id);

    // ── MINIMAL: safe privacy + basic service tweaks ──
    // Zero impact on daily use. Safe for ANY PC. In v1.4 we also add a handful
    // of ultra-safe process-reduction services (PhoneSvc, wisvc, WalletService,
    // WMPNetworkSvc, RemoteRegistry, WinRM, Browser, Netlogon) that almost no
    // consumer ever uses, so even the Minimum preset shaves a few processes off
    // Task Manager.
    bool IsMinimumTweak(string id)
    {
        return id switch
        {
            // Core privacy + telemetry (original 12)
            "Telemetry" or "Cortana" or "AdvertisingID" or "ActivityHist"
            or "ErrorReport" or "Feedback" or "WindowsTips" or "LockScreenTips"
            or "ConsumerFeatures" or "DisableDiagTrack" or "DisableDmwappushservice"
            or "DisableRetailDemo"
            // v1.4: ultra-safe process reducers (no feature loss for a normal user)
            or "DisablePhoneSvc" or "DisableWisvc" or "DisableWalletService"
            or "DisableWMPNetworkSvc" or "DisableRemoteRegistry" or "DisableWinRM"
            or "DisableBrowserSvc" or "DisableNetlogon"
                => true,
            _ => false,
        };
    }

    // ── NORMAL: balanced performance, safe but slightly more aggressive ──
    // Includes Minimal + latency/gaming tweaks + moderately aggressive cleanups.
    // In v1.4 this also includes the full set of safe (no-warning) process/CPU
    // reduction tweaks from TweaksProc — so the Normal preset now makes a very
    // visible dent in Task Manager's process count without touching anything a
    // normal user would miss.
    bool IsNormalTweak(string id)
    {
        if (IsMinimumTweak(id)) return true;
        return id switch
        {
            // Pushed from minimum to normal (moderately aggressive)
            "DisableCopilot" or "DisableRecall" or "DisableAI" or "DisableFax"
            or "DisableEdgeStartupBoost" or "DisableStartupDelay" or "TelemetryTasks"
            or "ExtraTelemetryTasks" or "W11DisableWidgets" or "W11DisableChat"
            // Safe gaming & CPU performance
            or "GameMode" or "DisableGameDVR" or "DisableGameBarPresence"
            or "MMAgent" or "DisableFullscreenOpt"
            or "DisablePowerThrottling" or "TimerResolution" or "DisableHPET2"
            // Network latency
            or "EnableRSS" or "DisableNagle2" or "DisableRSC" or "FastDNS"
            // Disk/SSD optimizations
            or "DisablePrefetch" or "DisableLastAccess"
            // Visuals
            or "VisualEffects" or "DisableTransparency"
            // UI cleanup
            or "W11ClassicMenu" or "W11TaskbarLeft" or "StorageSense"
            // Safe services
            or "KillEdgeBackground" or "DisableAllJoyn" or "DisableDownloadedMaps"
            or "DisableConnectedDevices" or "DisablePcaSvc" or "DisableDVR"
            // ── v1.4: safe (no-warning) process/CPU-reduction services ──
            or "DisableSensorService" or "DisableSensrSvc" or "DisableScardSvr"
            or "DisableScPolicySvc" or "DisableScDeviceEnum" or "DisableTapiSrv"
            or "DisableTrkWks" or "DisableWdiSvc"
            or "DisableSharedAccess" or "DisableCertPropSvc" or "DisableCscService"
            or "DisableDevQueryBroker" or "DisableDmEnrollmentSvc" or "DisableDsSvc"
            or "DisableEntAppSvc" or "DisableIpxlatCfgSvc" or "DisableKPSSvc"
            or "DisableLltdsvc" or "DisableMSiSCSI"
            or "DisableNcbService" or "DisablePeerDistSvc" or "DisablePLA"
            or "DisablePnrpSvc" or "DisablePolicyAgent" or "DisableQWAVE"
            or "DisableRpcLocator" or "DisableSeclogon" or "DisableSessionEnv"
            or "DisableSmphost" or "DisableSNMPTRAP" or "DisableSstpSvc"
            or "DisableStiSvc"
            or "DisableWecsvc" or "DisableWercplsupport" or "DisableWiaRpc"
            or "DisableWmiApSrv" or "DisableWorkfolderssvc" or "DisableWudfSvc"
            or "DisableHvHost" or "DisablePrintNotify" or "DisableFDResPub"
            or "DisableFDPortPub"
            // ── v2.5: additional safe process-reduction services ──
            or "DisableAppVClient" or "DisableBcastDVRBroker" or "DisableDPSvc"
            or "DisableHomeGroupListener" or "DisableHomeGroupProvider"
            or "DisableMSDTC" or "DisablePerfHost" or "DisableSmsRouter"
            or "DisableUdkUserSvc" or "DisableVds"
            or "DisableWPCSvc" or "DisableWPDBusEnum"
            // ── v1.4: safe (no-warning) scheduled tasks ──
            or "DisableCompatAppraiser" or "DisableCEDiagTask" or "DisableMapsTask"
            or "DisableWinSATTask" or "DisableDiskDiagTask" or "DisableBackupTask"
            // ── v1.4: safe (no-warning) CPU-overhead tweaks ──
            or "DisableNTFS83" or "DisableWindowsInkWorkspace" or "DisableEdgeBgMode"
            or "DisableSearchHistory"
                => true,
            _ => false,
        };
    }

    // ── FULLOPTI: maximum gaming performance ──
    // Selects ALL tweaks EXCEPT one-shot actions, security risks,
    // and gaming essentials (microphone, camera, search indexing).
    bool IsFullOptiTweak(string id)
    {
        // Safety: never auto-apply harmful, security-risk, or one-shot tweaks
        if (IsHarmfulTweak(id)) return false;

        // Gaming/productivity essentials: MUST stay enabled
        if (id == "Microphone" || id == "Camera" || id == "SearchIndex" 
            || id == "SearchHighlights" || id == "TaskbarSearchIcon" 
            || id == "SearchPrivacy" || id == "SearchPermissions" || id == "SearchUIBg")
        {
            return false;
        }

        // Apply everything else
        return true;
    }

    // Keep old name for compatibility
    bool IsStandardTweak(string id) => IsNormalTweak(id);

    // Braille-dot spinner characters (same sequence as SplashWindow / original .ps1).
    static readonly char[] SpinChars = {
        (char)0x280B, (char)0x2819, (char)0x2839, (char)0x2838,
        (char)0x283C, (char)0x2834, (char)0x2826, (char)0x2827,
        (char)0x2807, (char)0x280F,
    };

    DispatcherTimer _overlaySpinTimer;
    int _overlaySpinIdx = 0;

    void StartOverlaySpinner()
    {
        if (_overlaySpinTimer == null)
        {
            _overlaySpinTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
            _overlaySpinTimer.Tick += (_, _) =>
            {
                if (OverlaySpinner != null)
                {
                    OverlaySpinner.Text = SplashWindow.SpinChars[_overlaySpinIdx].ToString();
                    _overlaySpinIdx = (_overlaySpinIdx + 1) % SplashWindow.SpinChars.Length;
                }
            };
        }
        _overlaySpinTimer.Start();
    }

    void StopOverlaySpinner()
    {
        _overlaySpinTimer?.Stop();
    }

    void ShowLoadingOverlay(string title)
    {
        if (LoadingOverlay == null) return;
        StartOverlaySpinner();
        if (OverlayTitleLbl != null) OverlayTitleLbl.Text = title;
        if (OverlayCounter != null) OverlayCounter.Text = "";
        if (OverlayStatusLbl != null) OverlayStatusLbl.Text = "";
        if (OverlayProgressFill != null) OverlayProgressFill.Width = 0;

        AnimateShowOverlay(LoadingOverlay, LoadingBox);
    }

    void HideLoadingOverlay()
    {
        if (LoadingOverlay == null) return;
        StopOverlaySpinner();
        AnimateHideOverlay(LoadingOverlay, LoadingBox);
    }

    void ShowTweakInfoDialog(Tweak tweak)
    {
        var s = I18n.Get(_lang);
        string name = Tweaks.Name(tweak, (int)_lang);
        string categoryName = tweak.Category;
        string desc = Tweaks.Desc(tweak, (int)_lang);
        string note = Tweaks.Note(tweak, (int)_lang);

        string categoryLabel = _lang == I18n.Language.PL ? "Kategoria" : "Category";
        string descLabel = _lang == I18n.Language.PL ? "Szczegółowy opis" : "Detailed Description";
        string noteLabel = _lang == I18n.Language.PL ? "Ważna uwaga" : "Important Note";
        string safetyLabel = _lang == I18n.Language.PL ? "Stan bezpieczeństwa" : "Safety Status";
        string safeText = IsHarmfulTweak(tweak.Id)
            ? (_lang == I18n.Language.PL ? "Niebezpieczny (wymaga ostrożności)" : "Dangerous (requires caution)")
            : (_lang == I18n.Language.PL ? "Bezpieczny" : "Safe");

        string body = $"{categoryLabel}: {categoryName}\n" +
                      $"{safetyLabel}: {safeText}\n\n" +
                      $"{descLabel}:\n{desc}";

        if (!string.IsNullOrEmpty(note))
        {
            body += $"\n\n{noteLabel}:\n{note}";
        }

        ShowCustomDialog(name, body, IsHarmfulTweak(tweak.Id) ? DialogType.Warning : DialogType.Info, false);
    }

    void ShowWizLevelsOverlay()
    {
        AnimateShowOverlay(WizLevelsOverlay, WizLevelsBox);
    }

    void HideWizLevelsOverlay()
    {
        AnimateHideOverlay(WizLevelsOverlay, WizLevelsBox);
    }

    void WireWizard()
    {
        if (BtnWizMin != null)
        {
            BtnWizMin.Click += (_, _) =>
            {
                PlaySoundEffect();
                HideWizLevelsOverlay();
                _ = StartWizard("Minimum");
            };
        }
        if (BtnWizStd != null)
        {
            BtnWizStd.Click += (_, _) =>
            {
                PlaySoundEffect();
                HideWizLevelsOverlay();
                _ = StartWizard("Normal");
            };
        }
        if (BtnWizFull != null)
        {
            BtnWizFull.Click += (_, _) =>
            {
                PlaySoundEffect();
                HideWizLevelsOverlay();
                _ = StartWizard("FullOpti");
            };
        }
        if (BtnWizCloseOverlay != null)
        {
            BtnWizCloseOverlay.Click += (_, _) =>
            {
                PlaySoundEffect();
                HideWizLevelsOverlay();
            };
        }
        if (BtnWizCancel3 != null) BtnWizCancel3.Click += (_, _) => CloseWizard();
        if (BtnWizApply != null) BtnWizApply.Click += (_, _) => _ = ApplyWizard();
    }

    void CloseWizard()
    {
        AnimateHideOverlay(WizardOverlay, WizardBox);
    }

    async System.Threading.Tasks.Task StartWizard(string level = "FullOpti")
    {
        if (WizardOverlay == null) return;
        var s = I18n.Get(_lang);

        // v2.1: FullOpti stability warning — confirm before proceeding with 3 consecutive dialogs
        if (level == "FullOpti")
        {
            bool ask = TglAskClose != null ? TglAskClose.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "ShowConfirm") ?? 1) == 1;
            if (ask)
            {
                // Warning 1
                string warn1Title = _lang == I18n.Language.PL ? "Pełna Optymalizacja - Ostrzeżenie 1/3" : "Full Optimization - Warning 1/3";
                string warn1Body = _lang == I18n.Language.PL
                    ? "Pełna optymalizacja (Full Opti) wyłączy wiele zaawansowanych funkcji systemu Windows, takich jak Windows Update, usługi sieciowe Xbox (brak rozgrywek multiplayer w MS Store), telemetria oraz niektóre protokoły Bluetooth/drukarek. Czy na pewno chcesz kontynuować?"
                    : "Full Optimization (Full Opti) will disable many advanced Windows features, including Windows Update, Xbox Live services (no Microsoft Store multiplayer), telemetry, and some Bluetooth/printer protocols. Are you sure you want to proceed?";

                if (!ShowCustomDialog(warn1Title, warn1Body, DialogType.Warning, true)) return;

                // Warning 2
                string warn2Title = _lang == I18n.Language.PL ? "Bezpieczeństwo Systemu - Ostrzeżenie 2/3" : "System Security - Warning 2/3";
                string warn2Body = _lang == I18n.Language.PL
                    ? "Uwaga: System Windows NIE BĘDZIE automatycznie pobierał aktualizacji bezpieczeństwa po wdrożeniu tych ustawień. Może to wpłynąć na bezpieczeństwo Twojego komputera. Czy akceptujesz to ryzyko?"
                    : "Attention: Windows will NOT automatically download security updates after applying these settings. This may affect your computer security. Do you accept this risk?";

                if (!ShowCustomDialog(warn2Title, warn2Body, DialogType.Warning, true)) return;

                // Warning 3
                string warn3Title = _lang == I18n.Language.PL ? "Kluczowa Decyzja - Ostrzeżenie 3/3" : "Critical Decision - Warning 3/3";
                string warn3Body = _lang == I18n.Language.PL
                    ? "Czy na pewno chcesz zastosować profil Full Opti? Zalecamy upewnienie się, że utworzyłeś punkt przywracania (Flashback) przed kontynuacją."
                    : "Are you sure you want to apply the Full Opti profile? We highly recommend making sure you have created a restore point (Flashback) before continuing.";

                if (!ShowCustomDialog(warn3Title, warn3Body, DialogType.Danger, true)) return;
            }
        }

        AnimateShowOverlay(WizardOverlay, WizardBox);
        if (WizStep2 != null) WizStep2.Visibility = Visibility.Visible;
        if (WizStep3 != null) WizStep3.Visibility = Visibility.Collapsed;
        if (WizProgressBar != null)
        {
            bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
            if (anims)
            {
                WizProgressBar.Width = 0;
                var anim = new DoubleAnimation(0, 400, TimeSpan.FromMilliseconds(1400))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
                WizProgressBar.BeginAnimation(WidthProperty, anim);
            }
            else
            {
                WizProgressBar.BeginAnimation(WidthProperty, null);
                WizProgressBar.Width = 400;
            }
        }
        if (WizStatusMain != null) WizStatusMain.Text = s.WizScanMain;
        if (WizStatusSub != null) WizStatusSub.Text = s.WizScanSub;

        // Detect every tweak's state off-thread with PARALLEL detection.
        // v2.0: parallelization reduces scan time from 60-120s to 10-20s on
        // typical hardware by running PowerShell/process detects concurrently.
        var (rec, done, adv) = await System.Threading.Tasks.Task.Run(() =>
        {
            var r = new List<Tweak>();
            var d = new List<Tweak>();
            var a = new List<Tweak>();
            // Pre-compute detect results in parallel (thread-safe)
            var toScan = Tweaks.All.Where(t => t.Category != "Installer" && t.Category != "Settings").ToList();
            var results = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();
            System.Threading.Tasks.Parallel.ForEach(toScan, new System.Threading.Tasks.ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(8, Environment.ProcessorCount)
            }, t =>
            {
                bool on = SafeDetect(t);
                results[t.Id] = on;
            });
            // Bucket the results sequentially (list ops aren't thread-safe)
            foreach (var t in toScan)
            {
                bool on = results.TryGetValue(t.Id, out var v) && v;
                if (IsHarmfulTweak(t.Id))
                {
                    if (!on) a.Add(t); else d.Add(t);
                }
                else if (t.Warning) { if (!on) a.Add(t); else d.Add(t); }
                else { if (on) d.Add(t); else r.Add(t); }
            }
            return (r, d, a);
        });

        // Let the progress bar finish for a clean transition
        // v2.0: reduced from 1450ms to 600ms — parallel detect is much faster now,
        // so the long artificial delay was making the wizard feel sluggish.
        await System.Threading.Tasks.Task.Delay(600);

        var filteredRec = new List<Tweak>();
        foreach (var t in rec)
        {
            bool select = level switch
            {
                "Minimum" => IsMinimumTweak(t.Id),
                "Normal" => IsNormalTweak(t.Id),
                "FullOpti" => IsFullOptiTweak(t.Id),
                _ => IsMinimumTweak(t.Id)
            };
            if (select)
            {
                filteredRec.Add(t);
            }
        }

        _wizRecommended.Clear(); _wizRecommended.AddRange(filteredRec);
        _wizChecks.Clear();
        _wizAdvanced.Clear(); _wizAdvanced.AddRange(adv);
        _wizAdvancedChecks.Clear();

        if (WizStep2 != null) WizStep2.Visibility = Visibility.Collapsed;
        if (WizStep3 != null) WizStep3.Visibility = Visibility.Visible;

        if (WizRecommendedList != null)
        {
            WizRecommendedList.Items.Clear();
            foreach (var t in filteredRec)
            {
                var (row, cb) = BuildWizRow(t, true);
                WizRecommendedList.Items.Add(row);
                _wizChecks.Add(cb);
            }
        }
        if (WizAlreadyAppliedList != null)
        {
            WizAlreadyAppliedList.Items.Clear();
            foreach (var t in done) WizAlreadyAppliedList.Items.Add(BuildWizAppliedRow(t));
        }
        if (WizAdvancedList != null)
        {
            WizAdvancedList.Items.Clear();
            foreach (var t in adv)
            {
                var (row, cb) = BuildWizRow(t, false);
                WizAdvancedList.Items.Add(row);
                _wizAdvancedChecks.Add(cb);
            }
        }
        if (WizStats != null) WizStats.Text = string.Format(s.WizStatsFmt, rec.Count, done.Count, adv.Count);
    }

    (Border, CheckBox) BuildWizRow(Tweak t, bool isChecked)
    {
        var cb = new CheckBox { Style = (Style)Resources["ToggleStyle"], VerticalAlignment = VerticalAlignment.Center, IsChecked = isChecked };
        var row = new Border
        {
            Padding = new Thickness(10, 7, 10, 7), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 0, 4),
            Background = BgOff, BorderBrush = t.Warning ? WarnYellowBg : BorderOff, BorderThickness = new Thickness(1),
        };
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock { FontFamily = Mdl2, FontSize = 12, Foreground = FgIcon, Text = GetTweakIcon(t), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
        nameRow.Children.Add(new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = FgTitle, Text = Tweaks.Name(t, (int)_lang), VerticalAlignment = VerticalAlignment.Center });
        // Warning glyph for advanced (risky) tweaks
        if (t.Warning)
            nameRow.Children.Add(new TextBlock { FontFamily = Mdl2, FontSize = 11, Foreground = WarnYellow, Text = "\xE7BA", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, ToolTip = Tweaks.Note(t, (int)_lang) });
        sp.Children.Add(nameRow);
        var descText = $"{t.Category} — {Tweaks.Desc(t, (int)_lang)}";
        var descTb = new TextBlock { FontSize = 9.5, Foreground = FgDesc, Text = descText, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(20, 2, 0, 0) };
        sp.Children.Add(descTb);
        Grid.SetColumn(sp, 0); g.Children.Add(sp);
        Grid.SetColumn(cb, 1); g.Children.Add(cb);
        row.Child = g;
        return (row, cb);
    }

    Border BuildWizAppliedRow(Tweak t)
    {
        var row = new Border
        {
            Padding = new Thickness(10, 7, 10, 7), CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 0, 4),
            Background = BgOn, BorderBrush = BorderOn, BorderThickness = new Thickness(1),
        };
        var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock { FontFamily = Mdl2, FontSize = 12, Foreground = FgIcon, Text = GetTweakIcon(t), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
        nameRow.Children.Add(new TextBlock { FontSize = 12, Foreground = FgTitle, Text = Tweaks.Name(t, (int)_lang), VerticalAlignment = VerticalAlignment.Center });
        sp.Children.Add(nameRow);
        
        var descText = $"{t.Category} — {Tweaks.Desc(t, (int)_lang)}";
        var descTb = new TextBlock { FontSize = 9.5, Foreground = FgDesc, Text = descText, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(20, 2, 0, 0) };
        sp.Children.Add(descTb);
        
        row.Child = sp;
        return row;
    }

    async System.Threading.Tasks.Task ApplyWizard()
    {
        var s = I18n.Get(_lang);
        var toApply = new List<Tweak>();
        for (int i = 0; i < _wizRecommended.Count && i < _wizChecks.Count; i++)
            if (_wizChecks[i].IsChecked == true) toApply.Add(_wizRecommended[i]);
        // Also include any selected advanced (risky) tweaks
        for (int i = 0; i < _wizAdvanced.Count && i < _wizAdvancedChecks.Count; i++)
            if (_wizAdvancedChecks[i].IsChecked == true) toApply.Add(_wizAdvanced[i]);
        if (toApply.Count == 0) { CloseWizard(); return; }

        bool ask = TglAskClose != null ? TglAskClose.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "ShowConfirm") ?? 1) == 1;
        if (ask)
        {
            if (!ShowCustomDialog(s.ConfirmTitle, s.ConfirmApply, DialogType.Question, true)) return;
        }

        CloseWizard();
        SetStatus(s.StatusApply);
        bool skipRP = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SkipRestorePoints") ?? 0) == 1;
        if (!skipRP && TglRestorePoint != null && TglRestorePoint.IsChecked == true)
        {
            SetStatus(s.StatusCreatingRestorePoint);
            ShowTopProgress(s.StatusCreatingRestorePoint, -1);
            bool success = false;
            if (IsSystemRestoreSupported())
            {
                success = await CreateRestorePointAsync("NoveN Tweaks Flashback");
            }
            if (!success)
            {
                await System.Threading.Tasks.Task.Run(() => CreateLocalRegistryBackup("NoveN Tweaks Flashback (Auto)"));
            }
            HideTopProgress();
        }
        SetStatus(s.StatusApply);

        int ok = 0, fail = 0;
        int totalOps = toApply.Count;
        int completedOps = 0;
        ShowTopProgress(s.StatusApply, totalOps > 0 ? 0 : -1);
        await System.Threading.Tasks.Task.Run(() =>
        {
            if (!skipRP)
            {
                try { SafetyGuard.EnsureRestorePoint(); } catch { }
            }
            foreach (var t in toApply)
            {
                try { if (t.Apply()) { ok++; lock (_appliedTweaks) _appliedTweaks.Add(t.Id); } else fail++; }
                catch (Exception ex) { fail++; Debug.WriteLine($"Wizard apply {t.Id}: {ex.Message}"); }
                completedOps++;
                if (totalOps > 0) UpdateTopProgress(completedOps * 100 / totalOps);
            }
            Dispatcher.Invoke(SaveAppliedTweaks); InvalidateDetectCache();
        });
        HideTopProgress();

        SetStatus($"{ok} OK / {fail} FAIL - {s.StatusDone}");
        bool autoRestart = TglAutoRestart != null ? TglAutoRestart.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AutoRestart") ?? 0) == 1;
        if (autoRestart && ok > 0)
        {
            try
            {
                Process.Start("shutdown.exe", "/r /t 5 /f");
            }
            catch { }
        }
        else
        {
            if (fail > 0) ShowCustomDialog(s.WarnTitle, s.ApplyError, DialogType.Error, false);
            else ShowCustomDialog(s.StartupErrorTitle, s.RestartNote, DialogType.Info, false);
        }

        // Refresh: clear cached builds so panels rebuild with fresh state/colours
        try { _builtPanels.Clear(); } catch { }
        if (!string.IsNullOrEmpty(_activeCategory) && _activeCategory != "Home" && _activeCategory != "Settings" && _activeCategory != "Installer")
            SwitchTo(_activeCategory);
        else
            RefreshActiveCards();
        UpdateStatus();
    }

    void SelectAll(bool on)
    {
        if (string.IsNullOrEmpty(_activeCategory)) return;
        _isProgrammaticChange = true;
        foreach (var t in Tweaks.All.Where(x => x.Category == _activeCategory))
            if (_checkboxes.TryGetValue(t.Id, out var cb)) cb.IsChecked = on;
        _isProgrammaticChange = false;
        UpdateStatus();
    }

    async void DoApply()
    {
        var s = I18n.Get(_lang);

        // v2.8: Elevation check — if not running as admin, prompt before applying.
        // Most tweaks write to HKLM or run system commands that require admin.
        if (!ElevationHelper.IsElevated)
        {
            if (ShowElevationDialog(s))
            {
                // User accepted — restart elevated and exit this instance.
                if (ElevationHelper.RestartAsAdmin())
                    Application.Current.Shutdown();
                return;
            }
            else
            {
                // User declined — abort.
                SetStatus(s.ElevCancel);
                return;
            }
        }

        var toApply = new List<Tweak>();
        var toUndo = new List<Tweak>();
        
        foreach (var t in Tweaks.All)
        {
            if ((string.IsNullOrEmpty(_activeCategory) || t.Category == _activeCategory) && _checkboxes.TryGetValue(t.Id, out var cb))
            {
                if (cb.IsChecked == true)
                {
                    if (t.Apply != null) toApply.Add(t);
                }
            }
        }

        if (toApply.Count == 0 && toUndo.Count == 0) { SetStatus(s.StatusNone); return; }

        bool ask = TglAskClose != null ? TglAskClose.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "ShowConfirm") ?? 1) == 1;
        if (ask)
        {
            if (!ShowCustomDialog(s.ConfirmTitle, s.ConfirmApply, DialogType.Question, true)) return;
        }
        
        SetStatus(s.StatusApply);
        ForceRender();

        if (BtnApply != null) BtnApply.IsEnabled = false;
        if (BtnUndo != null) BtnUndo.IsEnabled = false;

        ShowLoadingOverlay(s.StatusApply);

        try
        {
            // If automatic restore point toggle is checked, create one
            // v1.4: Skip restore point if user has disabled it in settings (with prior confirmation).
            bool skipRestorePoint = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SkipRestorePoints") ?? 0) == 1;
            if (!skipRestorePoint && TglRestorePoint != null && TglRestorePoint.IsChecked == true)
            {
                SetStatus(s.StatusCreatingRestorePoint);
                Dispatcher.Invoke(() => {
                    if (OverlayTitleLbl != null) OverlayTitleLbl.Text = s.StatusCreatingRestorePoint;
                });
                ShowTopProgress(s.StatusCreatingRestorePoint, -1); // indeterminate
                bool success = false;
                if (IsSystemRestoreSupported())
                {
                    success = await CreateRestorePointAsync("NoveN Tweaks Flashback");
                }
                if (!success)
                {
                    await System.Threading.Tasks.Task.Run(() => CreateLocalRegistryBackup("NoveN Tweaks Flashback (Auto)"));
                }
                HideTopProgress();
            }

            SetStatus(s.StatusApply);
            Dispatcher.Invoke(() => {
                if (OverlayTitleLbl != null) OverlayTitleLbl.Text = s.StatusApply;
            });

            int ok = 0, fail = 0;
            int totalOps = toApply.Count + toUndo.Count;
            int completedOps = 0;
            // Show top progress for the apply phase
            ShowTopProgress(s.StatusApply, totalOps > 0 ? 0 : -1);

            await System.Threading.Tasks.Task.Run(() =>
            {
                // v2.1: Create a restore point before the batch (only if not skipped).
                if (!skipRestorePoint)
                {
                    try { SafetyGuard.EnsureRestorePoint(); } catch { }
                }
                var failedList = new List<(Tweak tweak, string action, string error)>();
                foreach (var t in toApply)
                {
                    string name = Tweaks.Name(t, (int)_lang);
                    Dispatcher.Invoke(() => {
                        if (OverlayStatusLbl != null) OverlayStatusLbl.Text = _lang == I18n.Language.PL ? $"Wdrażanie: {name}..." : $"Applying: {name}...";
                        if (OverlayCounter != null) OverlayCounter.Text = $"{completedOps} / {totalOps}";
                        if (OverlayProgressFill != null) OverlayProgressFill.Width = 280.0 * completedOps / totalOps;
                    });

                    try {
                        if (t.Apply()) {
                            ok++;
                            lock(_appliedTweaks) { _appliedTweaks.Add(t.Id); }
                        } else {
                            fail++;
                            failedList.Add((t, "Apply", "Apply returned false"));
                        }
                    }
                    catch (Exception ex) {
                        fail++;
                        failedList.Add((t, "Apply", ex.Message));
                        Debug.WriteLine($"Apply {t.Id}: {ex.Message}");
                    }
                    completedOps++;
                    if (totalOps > 0) UpdateTopProgress(completedOps * 100 / totalOps);
                    System.Threading.Thread.Sleep(5);
                }
                foreach (var t in toUndo)
                {
                    string name = Tweaks.Name(t, (int)_lang);
                    Dispatcher.Invoke(() => {
                        if (OverlayStatusLbl != null) OverlayStatusLbl.Text = _lang == I18n.Language.PL ? $"Cofanie: {name}..." : $"Undoing: {name}...";
                        if (OverlayCounter != null) OverlayCounter.Text = $"{completedOps} / {totalOps}";
                        if (OverlayProgressFill != null) OverlayProgressFill.Width = 280.0 * completedOps / totalOps;
                    });

                    try {
                        if (t.Undo()) {
                            ok++;
                            lock(_appliedTweaks) { _appliedTweaks.Remove(t.Id); }
                        } else {
                            fail++;
                            failedList.Add((t, "Undo", "Undo returned false"));
                        }
                    }
                    catch (Exception ex) {
                        fail++;
                        failedList.Add((t, "Undo", ex.Message));
                        Debug.WriteLine($"Undo {t.Id}: {ex.Message}");
                    }
                    completedOps++;
                    if (totalOps > 0) UpdateTopProgress(completedOps * 100 / totalOps);
                    System.Threading.Thread.Sleep(5);
                }
                if (failedList.Count > 0)
                {
                    LogFailedTweaks(failedList);
                }
                Dispatcher.Invoke(SaveAppliedTweaks); InvalidateDetectCache();
            });

            SetStatus($"{ok} OK / {fail} FAIL - {s.StatusDone}");
            bool autoRestart = TglAutoRestart != null ? TglAutoRestart.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AutoRestart") ?? 0) == 1;
            if (autoRestart && ok > 0)
            {
                try
                {
                    Process.Start("shutdown.exe", "/r /t 5 /f");
                }
                catch { }
            }
            else
            {
                if (fail > 0)
                {
                    string failMsg = _lang == I18n.Language.PL
                        ? $"{s.ApplyError}\n\nRaport uszkodzonych/nieudanych tweaków został zapisany w pliku:\nNoveNTweaks_Failed_Tweaks.txt na Pulpicie."
                        : $"{s.ApplyError}\n\nA report of failed tweaks was saved to:\nNoveNTweaks_Failed_Tweaks.txt on your Desktop.";
                    ShowCustomDialog(s.WarnTitle, failMsg, DialogType.Error, false);
                }
                else ShowCustomDialog(s.StartupErrorTitle, s.RestartNote, DialogType.Info, false);
            }
            RefreshActiveCards();
            UpdateStatus();
        }
        finally
        {
            HideTopProgress();
            HideLoadingOverlay();
            if (BtnApply != null) BtnApply.IsEnabled = true;
            if (BtnUndo != null) BtnUndo.IsEnabled = true;
        }
    }

    void LogFailedTweaks(List<(Tweak tweak, string action, string error)> failedList)
    {
        if (failedList == null || failedList.Count == 0) return;
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var path = System.IO.Path.Combine(desktop, "NoveNTweaks_Failed_Tweaks.txt");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("============================================================");
            sb.AppendLine($" NoveNTweaks - Failed Tweaks Log [{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
            sb.AppendLine("============================================================");
            sb.AppendLine($"Total Failed Operations: {failedList.Count}");
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine();

            foreach (var (t, action, err) in failedList)
            {
                string title = Tweaks.Name(t, (int)_lang);
                string desc = Tweaks.Desc(t, (int)_lang);
                sb.AppendLine($"[FAILED] ID: {t.Id} | Category: {t.Category} | Action: {action}");
                sb.AppendLine($"  Title:   {title}");
                sb.AppendLine($"  Command: {desc}");
                if (!string.IsNullOrEmpty(err))
                    sb.AppendLine($"  Error:   {err}");
                sb.AppendLine();
            }

            System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
            Debug.WriteLine($"LogFailedTweaks saved to {path}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LogFailedTweaks failed: {ex.Message}");
        }
    }

    async void DoUndo()
    {
        var s = I18n.Get(_lang);
        
        var toUndo = new List<Tweak>();
        foreach (var t in Tweaks.All)
        {
            if ((string.IsNullOrEmpty(_activeCategory) || t.Category == _activeCategory) && _checkboxes.TryGetValue(t.Id, out var cb) && cb.IsChecked == true)
            {
                bool currentlyActive = false;
                lock (_tweakActiveState)
                {
                    _tweakActiveState.TryGetValue(t.Id, out var act);
                    currentlyActive = act == true;
                }
                if (currentlyActive && t.Undo != null)
                {
                    toUndo.Add(t);
                }
            }
        }

        if (toUndo.Count == 0) { SetStatus(s.StatusNone); return; }

        bool ask = TglAskClose != null ? TglAskClose.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "ShowConfirm") ?? 1) == 1;
        if (ask)
        {
            if (!ShowCustomDialog(s.ConfirmTitle, s.ConfirmUndo, DialogType.Question, true)) return;
        }
        
        SetStatus(s.StatusUndo);
        ForceRender();

        if (BtnApply != null) BtnApply.IsEnabled = false;
        if (BtnUndo != null) BtnUndo.IsEnabled = false;

        ShowLoadingOverlay(s.StatusUndo);

        // If automatic restore point toggle is checked, create one
        // v1.4: Skip if user disabled restore points in settings.
        bool skipRP2 = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SkipRestorePoints") ?? 0) == 1;
        if (!skipRP2 && TglRestorePoint != null && TglRestorePoint.IsChecked == true)
        {
            SetStatus(s.StatusCreatingRestorePoint);
            Dispatcher.Invoke(() => {
                if (OverlayTitleLbl != null) OverlayTitleLbl.Text = s.StatusCreatingRestorePoint;
            });
            ShowTopProgress(s.StatusCreatingRestorePoint, -1);
            bool success = false;
            if (IsSystemRestoreSupported())
            {
                success = await CreateRestorePointAsync("NoveN Tweaks Flashback");
            }
            if (!success)
            {
                await System.Threading.Tasks.Task.Run(() => CreateLocalRegistryBackup("NoveN Tweaks Flashback (Auto)"));
            }
            HideTopProgress();
        }

        SetStatus(s.StatusUndo);
        Dispatcher.Invoke(() => {
            if (OverlayTitleLbl != null) OverlayTitleLbl.Text = s.StatusUndo;
        });

        int ok = 0, fail = 0;
        int totalOps = toUndo.Count;
        int completedOps = 0;
        ShowTopProgress(s.StatusUndo, totalOps > 0 ? 0 : -1);
        await System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var t in toUndo)
            {
                string name = Tweaks.Name(t, (int)_lang);
                Dispatcher.Invoke(() => {
                    if (OverlayStatusLbl != null) OverlayStatusLbl.Text = _lang == I18n.Language.PL ? $"Cofanie: {name}..." : $"Undoing: {name}...";
                    if (OverlayCounter != null) OverlayCounter.Text = $"{completedOps} / {totalOps}";
                    if (OverlayProgressFill != null) OverlayProgressFill.Width = 280.0 * completedOps / totalOps;
                });

                try {
                    if (t.Undo()) {
                        ok++;
                        lock(_appliedTweaks) { _appliedTweaks.Remove(t.Id); }
                    } else fail++;
                }
                catch (Exception ex) { fail++; Debug.WriteLine($"Undo {t.Id}: {ex.Message}"); }
                completedOps++;
                if (totalOps > 0) UpdateTopProgress(completedOps * 100 / totalOps);
            }
            Dispatcher.Invoke(SaveAppliedTweaks); InvalidateDetectCache();
        });
        HideTopProgress();
        HideLoadingOverlay();

        if (BtnApply != null) BtnApply.IsEnabled = true;
        if (BtnUndo != null) BtnUndo.IsEnabled = true;

        SetStatus($"{ok} OK - {s.StatusDone}");
        RefreshActiveCards();
        UpdateStatus();
    }

    IEnumerable<Tweak> SelectedTweaks()
        => Tweaks.All.Where(t => t.Category == _activeCategory && _checkboxes.TryGetValue(t.Id, out var cb) && cb.IsChecked == true);

    void RefreshActiveCards()
    {
        foreach (var t in Tweaks.All.Where(x => x.Category == _activeCategory))
        {
            if (!_checkboxes.TryGetValue(t.Id, out var cb)) continue;
            if (!_cards.TryGetValue(t.Id, out var card)) continue;
            bool applied = SafeDetect(t);
            lock (_tweakActiveState)
            {
                _tweakActiveState[t.Id] = applied;
            }
            if (applied != (cb.IsChecked == true)) cb.IsChecked = applied;
            else SetCardVisual(card, cb, applied);
        }
    }

    void SetStatus(string text)
    {
        if (StatusLabel != null) StatusLabel.Text = text;
        if (FooterStatus != null) FooterStatus.Text = text;
    }

    // v2.8: Elevation dialog — shown when an operation needs admin privileges.
    // Returns true if the user wants to restart as admin, false if they cancel.
    bool ShowElevationDialog(I18n.Strings s)
    {
        if (s == null) s = I18n.Get(_lang);
        return ShowCustomDialog(s.ElevTitle, s.ElevBody, DialogType.Warning, true);
    }

    // ───────── v2.5: Animated top progress bar helpers (cleaner design) ─────────
    // Show the top blue animated progress bar with a status text + percentage.
    // percent: 0-100, or -1 for indeterminate (no specific %).
    void ShowTopProgress(string text, int percent = -1)
    {
        Dispatcher.Invoke(() =>
        {
            if (TopProgressBar != null)
            {
                TopProgressBar.Visibility = Visibility.Visible;
                if (percent < 0)
                {
                    TopProgressBar.IsIndeterminate = true;
                }
                else
                {
                    TopProgressBar.IsIndeterminate = false;
                    TopProgressBar.Value = Math.Max(0, Math.Min(100, percent));
                }
            }
            if (TopProgressLabelWrap != null) TopProgressLabelWrap.Visibility = Visibility.Visible;
            if (TopProgressLabel != null)
            {
                TopProgressLabel.Text = percent >= 0 ? $"{text}  ·  {percent}%" : text;
            }
        });
    }

    // Update only the percentage on the top progress bar (text stays the same).
    void UpdateTopProgress(int percent)
    {
        Dispatcher.Invoke(() =>
        {
            if (TopProgressBar != null && !TopProgressBar.IsIndeterminate)
                TopProgressBar.Value = Math.Max(0, Math.Min(100, percent));
            if (TopProgressLabel != null && TopProgressLabelWrap != null && TopProgressLabelWrap.Visibility == Visibility.Visible)
            {
                var current = TopProgressLabel.Text ?? "";
                var idx = current.IndexOf("·");
                var base_text = idx >= 0 ? current.Substring(0, idx).TrimEnd() : current;
                TopProgressLabel.Text = $"{base_text}  ·  {Math.Max(0, Math.Min(100, percent))}%";
            }
        });
    }

    // Update both text and percentage.
    void UpdateTopProgress(string text, int percent)
    {
        Dispatcher.Invoke(() =>
        {
            if (TopProgressBar != null && !TopProgressBar.IsIndeterminate)
                TopProgressBar.Value = Math.Max(0, Math.Min(100, percent));
            if (TopProgressLabel != null)
            {
                TopProgressLabel.Text = percent >= 0 ? $"{text}  ·  {percent}%" : text;
            }
        });
    }

    // Hide the top progress bar.
    void HideTopProgress()
    {
        Dispatcher.Invoke(() =>
        {
            if (TopProgressBar != null)
            {
                TopProgressBar.IsIndeterminate = false;
                TopProgressBar.Visibility = Visibility.Collapsed;
            }
            if (TopProgressLabelWrap != null) TopProgressLabelWrap.Visibility = Visibility.Collapsed;
        });
    }

    // ───────── Animation helpers for custom overlays ─────────
    void AnimateShowOverlay(Grid overlay, FrameworkElement box)
    {
        if (overlay == null) return;
        overlay.IsHitTestVisible = true;

        bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
        if (!anims)
        {
            overlay.Visibility = Visibility.Visible;
            overlay.Opacity = 1.0;
            if (box != null)
            {
                box.RenderTransform = null;
            }
            return;
        }

        overlay.Visibility = Visibility.Visible;
        overlay.Opacity = 0;

        var fadeAnim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.24)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        overlay.BeginAnimation(OpacityProperty, fadeAnim);

        if (box != null)
        {
            var scale = new ScaleTransform(0.93, 0.93);
            var translate = new TranslateTransform(0, 20);
            var group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(translate);

            box.RenderTransformOrigin = new Point(0.5, 0.5);
            box.RenderTransform = group;

            var scaleAnim = new DoubleAnimation(0.93, 1.0, new Duration(TimeSpan.FromSeconds(0.28)))
            { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.15 } };

            var slideAnim = new DoubleAnimation(20, 0, new Duration(TimeSpan.FromSeconds(0.28)))
            { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.15 } };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            translate.BeginAnimation(TranslateTransform.YProperty, slideAnim);
        }
    }

    void AnimateHideOverlay(Grid overlay, FrameworkElement box, Action onComplete = null)
    {
        if (overlay == null)
        {
            onComplete?.Invoke();
            return;
        }

        overlay.IsHitTestVisible = false;
        overlay.Visibility = Visibility.Collapsed;
        overlay.Opacity = 0.0;
        if (box != null)
        {
            box.RenderTransform = null;
        }
        onComplete?.Invoke();
    }

    // ───────── v2.5: Custom dialog system (replaces default MessageBox) ─────────
    // Dialog types: info, warning, error, danger, question, success
    // Each type has its own color banner + icon.
    enum DialogType { Info, Warning, Error, Danger, Question, Success }

    // Shows a custom dialog and returns true for Yes/OK, false for No/Cancel.
    bool ShowCustomDialog(string title, string message, DialogType type = DialogType.Info,
        bool yesNo = false)
    {
        if (DialogOverlay == null || DialogTitle == null || DialogMessage == null)
        {
            // Fallback to MessageBox if overlay not available
            var btn = yesNo ? MessageBoxButton.YesNo : MessageBoxButton.OK;
            var img = type switch
            {
                DialogType.Warning or DialogType.Danger => MessageBoxImage.Warning,
                DialogType.Error => MessageBoxImage.Error,
                DialogType.Question => MessageBoxImage.Question,
                DialogType.Success => MessageBoxImage.Information,
                _ => MessageBoxImage.Information,
            };
            return MessageBox.Show(message, title, btn, img, MessageBoxResult.No) == MessageBoxResult.Yes
                || (!yesNo && MessageBox.Show(message, title, btn, img) == MessageBoxResult.OK);
        }

        // Set up dialog content
        DialogTitle.Text = title ?? "";
        DialogMessage.Text = message ?? "";

        // Configure header banner color + icon based on type
        string iconGlyph, iconColor;
        switch (type)
        {
            case DialogType.Warning:
                iconGlyph = "\xE7BA"; iconColor = "#F59E0B"; break;
            case DialogType.Error:
                iconGlyph = "\xEB90"; iconColor = "#EF4444"; break;
            case DialogType.Danger:
                iconGlyph = "\xE7BA"; iconColor = "#DC2626"; break;
            case DialogType.Question:
                iconGlyph = "\xE9CE"; iconColor = "#3B82F6"; break;
            case DialogType.Success:
                iconGlyph = "\xE73E"; iconColor = "#10B981"; break;
            default: // Info
                iconGlyph = "\xE946"; iconColor = "#A1A1AA"; break;
        }
        // header background remains transparent for modern visual style
        if (DialogIcon != null)
        {
            DialogIcon.Text = iconGlyph;
            DialogIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(iconColor));
        }

        // Show appropriate buttons
        if (BtnDialogYes != null) BtnDialogYes.Visibility = yesNo ? Visibility.Visible : Visibility.Collapsed;
        if (BtnDialogNo != null) BtnDialogNo.Visibility = yesNo ? Visibility.Visible : Visibility.Collapsed;
        if (BtnDialogOK != null) BtnDialogOK.Visibility = yesNo ? Visibility.Collapsed : Visibility.Visible;

        // Show overlay with a smooth slide + fade-in (Apple-style)
        AnimateShowOverlay(DialogOverlay, DialogBox);

        // Block until user clicks a button (using a TaskCompletionSource)
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        RoutedEventHandler yesHandler = null, noHandler = null, okHandler = null;
        void cleanup(bool dialogResult)
        {
            if (BtnDialogYes != null && yesHandler != null) BtnDialogYes.Click -= yesHandler;
            if (BtnDialogNo != null && noHandler != null) BtnDialogNo.Click -= noHandler;
            if (BtnDialogOK != null && okHandler != null) BtnDialogOK.Click -= okHandler;
            
            AnimateHideOverlay(DialogOverlay, DialogBox, () =>
            {
                tcs.TrySetResult(dialogResult);
            });
        }
        if (BtnDialogYes != null)
        {
            yesHandler = (_, _) => { cleanup(true); };
            BtnDialogYes.Click += yesHandler;
        }
        if (BtnDialogNo != null)
        {
            noHandler = (_, _) => { cleanup(false); };
            BtnDialogNo.Click += noHandler;
        }
        if (BtnDialogOK != null)
        {
            okHandler = (_, _) => { cleanup(true); };
            BtnDialogOK.Click += okHandler;
        }
        // Wait for the result (this blocks the calling thread, but we expect
        // to be on UI thread; the Click handlers run synchronously when clicked)
        // To avoid blocking UI, we'll use a Dispatcher frame.
        var frame = new DispatcherFrame();
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var result = await tcs.Task;
                _dialogResult = result;
            }
            catch { _dialogResult = false; }
            frame.Continue = false;
        });
        Dispatcher.PushFrame(frame);
        return _dialogResult;
    }


    private bool _dialogResult = false;

    /// <summary>
    /// Thread-safe wrapper around ShowCustomDialog. Use this everywhere instead of
    /// MessageBox.Show — it dispatches to the UI thread when called from a
    /// background task and renders the custom Apple-style animated dialog.
    /// </summary>
    bool MsgBox(string title, string message, DialogType type = DialogType.Info, bool yesNo = false)
    {
        if (DialogOverlay == null)
        {
            // Graceful fallback to system MessageBox if the overlay isn't built yet
            var btn = yesNo ? MessageBoxButton.YesNo : MessageBoxButton.OK;
            var img = type switch
            {
                DialogType.Warning or DialogType.Danger => MessageBoxImage.Warning,
                DialogType.Error => MessageBoxImage.Error,
                DialogType.Question => MessageBoxImage.Question,
                DialogType.Success => MessageBoxImage.Information,
                _ => MessageBoxImage.Information,
            };
            var r = MessageBox.Show(message, title, btn, img);
            return yesNo ? r == MessageBoxResult.Yes : r == MessageBoxResult.OK;
        }
        if (Dispatcher.CheckAccess())
            return ShowCustomDialog(title, message, type, yesNo);
        return (bool)Dispatcher.Invoke(new Func<bool>(() => ShowCustomDialog(title, message, type, yesNo)));
    }

    void UpdateStatus()
    {
        var s = I18n.Get(_lang);
        if (string.IsNullOrEmpty(_activeCategory) || _activeCategory == "Home" || _activeCategory == "Installer" || _activeCategory == "Settings" || _activeCategory == "SpeedTest")
        {
            SetStatus(s.StatusReady);
            UpdateTweakProgress(null, 0, 0);
            return;
        }
        int selected = 0, active = 0, total = 0;
        foreach (var t in Tweaks.All.Where(x => x.Category == _activeCategory))
        {
            total++;
            if (_checkboxes.TryGetValue(t.Id, out var cb) && cb.IsChecked == true) selected++;

            bool isActive = false;
            lock (_tweakActiveState)
            {
                if (_tweakActiveState.TryGetValue(t.Id, out var val) && val == true)
                    isActive = true;
            }
            if (isActive) active++;
        }
        SetStatus(s.StatusReady);
        UpdateTweakProgress(_activeCategory, active, total);
    }

    /// <summary>
    /// Updates the per-category "applied / total" progress indicator in the page header.
    /// Hides it for non-tweak views (Home/Installer/Settings).
    /// </summary>
    void UpdateTweakProgress(string category, int applied, int total)
    {
        if (TweakProgressBadge == null) return;
        bool show = !string.IsNullOrEmpty(category) && total > 0;
        TweakProgressBadge.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show) return;

        if (TweakProgressText != null)
        {
            string appliedLbl = _lang switch
            {
                I18n.Language.PL => "zastosowanych",
                I18n.Language.DE => "angewendet",
                I18n.Language.FR => "appliqués",
                I18n.Language.ES => "aplicados",
                I18n.Language.IT => "applicati",
                I18n.Language.CZ => "aplikováno",
                I18n.Language.RU => "применено",
                I18n.Language.ZH => "已应用",
                _ => "applied",
            };
            TweakProgressText.Text = $"{applied} / {total} {appliedLbl}";
        }

        if (TweakProgressFill != null)
        {
            double pct = total > 0 ? (double)applied / total : 0;
            // the track is 64px wide
            TweakProgressFill.Width = 64.0 * pct;
        }
    }

    void ForceRender() => Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

    // ============================================================
    // BLOATWARE APP-PICKER DIALOG (auto-detect + checkbox list)
    // ============================================================

    /// <summary>Catalog of bloatware/sponsored apps: pattern → friendly name.</summary>
    static readonly (string Pattern, string Name)[] BloatwareCatalog = new[]
    {
        ("*king.com.CandyCrushSaga*", "Candy Crush Saga"),
        ("*king.com.CandyCrushSodaSaga*", "Candy Crush Soda Saga"),
        ("*Netflix*", "Netflix"),
        ("*TikTok*", "TikTok"),
        ("*Spotify*", "Spotify"),
        ("*Disney.*", "Disney+"),
        ("*PrimeVideo*", "Prime Video"),
        ("*AdobeSystemsIncorporated.AdobePhotoshopExpress*", "Adobe Photoshop Express"),
        ("*DevHome*", "Dev Home"),
        ("*MSTeams*", "Microsoft Teams"),
        ("*OutlookForWindows*", "Outlook (New)"),
        ("*Instagram*", "Instagram"),
        ("*Facebook*", "Facebook"),
        ("*Twitter*", "Twitter"),
        ("*Duolingo-LearnLanguagesforFree*", "Duolingo"),
        ("*Coinbase*", "Coinbase"),
        ("*PandoraMediaInc*", "Pandora"),
        ("*iHeartRadio*", "iHeartRadio"),
        ("*EclipseManager*", "Eclipse Manager"),
        ("*ActiproSoftwareLLC*", "Actipro Software"),
        ("*Microsoft.NetworkSpeedTest*", "Network Speed Test"),
        ("*Microsoft.BingTranslator*", "Bing Translator"),
        ("*BingWeather*", "Bing Weather"),
        ("*BingNews*", "Bing News"),
        ("*BingFinance*", "Bing Finance"),
        ("*BingSports*", "Bing Sports"),
        ("*Microsoft3DViewer*", "3D Viewer"),
        ("*Microsoft.Microsoft3DViewer*", "3D Viewer (Alt)"),
        ("*MicrosoftSolitaireCollection*", "Microsoft Solitaire Collection"),
        ("*MicrosoftWindowsMaps*", "Windows Maps"),
        ("*Microsoft.People*", "People"),
        ("*Microsoft.ZuneMusic*", "Zune Music (Groove)"),
        ("*Microsoft.ZuneVideo*", "Zune Video (Movies & TV)"),
        ("*Microsoft.GetHelp*", "Get Help"),
        ("*Microsoft.Getstarted*", "Get Started"),
        ("*Microsoft.Messaging*", "Messaging"),
        ("*Microsoft.OneConnect*", "Paid Wi-Fi & Cellular"),
        ("*Microsoft.WindowsSoundRecorder*", "Sound Recorder"),
        ("*Microsoft.MicrosoftStickyNotes*", "Sticky Notes"),
        ("*Microsoft.MicrosoftOfficeHub*", "Office Hub"),
        ("*Microsoft.Office.OneNote*", "OneNote"),
        ("*Microsoft.SkypeApp*", "Skype"),
        ("*Microsoft.WindowsFeedbackHub*", "Feedback Hub"),
        ("*Microsoft.Print3D*", "Print 3D"),
        ("*3DBuilder*", "3D Builder"),
        ("*Microsoft.MixedReality.Portal*", "Mixed Reality Portal"),
        ("*Microsoft.Wallet*", "Wallet"),
        ("*YourPhone*", "Phone Link (Your Phone)"),
        ("*GamingApp*", "Xbox App / Gaming Hub"),
        ("*Clipchamp*", "Clipchamp"),
        ("*OneDrive*", "OneDrive"),
        ("*PowerAutomate*", "Power Automate"),
    };

    /// <summary>Tracks whether the app-picker is currently open (prevents re-entry).</summary>
    bool _appPickerOpen = false;

    /// <summary>
    /// Shows the bloatware app-picker dialog. Scans for installed apps from the catalog,
    /// shows a checkbox list, and removes the selected apps.
    /// Returns true if at least one app was removed.
    /// </summary>
    bool ShowAppPickerDialog(string title)
    {
        if (AppPickerOverlay == null || _appPickerOpen) return false;
        _appPickerOpen = true;

        // Set title
        if (AppPickerTitle != null) AppPickerTitle.Text = title;
        if (AppPickerSubtitle != null) AppPickerSubtitle.Text = "Detecting installed applications...";
        if (AppPickerCount != null) AppPickerCount.Text = "";
        if (AppPickerList != null) AppPickerList.Children.Clear();
        if (BtnAppPickerRemove != null) BtnAppPickerRemove.IsEnabled = false;

        // Show overlay with slide+fade animation
        AnimateShowOverlay(AppPickerOverlay, AppPickerBox);

        // Scan for installed apps in background
        var detectedApps = new System.Collections.Generic.List<(string Pattern, string Name, string PackageFullName, bool IsPattern)>();
        System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var (pattern, name) in BloatwareCatalog)
            {
                try
                {
                    var packages = Run.ListAppxPackages(pattern);
                    if (packages.Count > 0)
                    {
                        foreach (var pkg in packages)
                        {
                            var displayName = string.IsNullOrEmpty(pkg.Name) ? name : pkg.Name;
                            detectedApps.Add((pattern, displayName, pkg.PackageFullName, false));
                        }
                    }
                    else
                    {
                        detectedApps.Add((pattern, name, pattern, true));
                    }
                }
                catch 
                {
                    detectedApps.Add((pattern, name, pattern, true));
                }
            }

            Dispatcher.Invoke(() =>
            {
                int installedCount = 0;
                foreach (var app in detectedApps)
                {
                    if (!app.IsPattern) installedCount++;
                }
                if (AppPickerSubtitle != null)
                {
                    AppPickerSubtitle.Text = $"{installedCount} installed application{(installedCount == 1 ? "" : "s")} detected. Select which to remove:";
                }
                PopulateAppPickerList(detectedApps);
                UpdateCount();
            });
        });

        // Block until user clicks Remove or Cancel
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        RoutedEventHandler removeHandler = null, cancelHandler = null, selectAllHandler = null;

        void UpdateCount()
        {
            int checkedCount = 0;
            foreach (var child in AppPickerList.Children)
            {
                if (child is CheckBox cb && cb.IsChecked == true) checkedCount++;
            }
            if (AppPickerCount != null)
                AppPickerCount.Text = checkedCount == 0 ? "" : $"{checkedCount} selected";
            if (BtnAppPickerRemove != null)
                BtnAppPickerRemove.IsEnabled = checkedCount > 0;
        }

        void closePicker(bool removed)
        {
            if (BtnAppPickerRemove != null && removeHandler != null) BtnAppPickerRemove.Click -= removeHandler;
            if (BtnAppPickerCancel != null && cancelHandler != null) BtnAppPickerCancel.Click -= cancelHandler;
            if (BtnAppPickerSelectAll != null && selectAllHandler != null) BtnAppPickerSelectAll.Click -= selectAllHandler;
            
            AnimateHideOverlay(AppPickerOverlay, AppPickerBox, () =>
            {
                _appPickerOpen = false;
                tcs.TrySetResult(removed);
            });
        }

        if (BtnAppPickerRemove != null)
        {
            removeHandler = (_, _) =>
            {
                // Collect selected packages
                var toRemove = new System.Collections.Generic.List<string>();
                foreach (var child in AppPickerList.Children)
                {
                    if (child is CheckBox cb && cb.IsChecked == true && cb.Tag is string pkg)
                        toRemove.Add(pkg);
                }
                if (toRemove.Count == 0) return;

                if (BtnAppPickerRemove != null) BtnAppPickerRemove.IsEnabled = false;
                if (AppPickerSubtitle != null) AppPickerSubtitle.Text = $"Removing {toRemove.Count} application(s)...";

                // Remove in background
                System.Threading.Tasks.Task.Run(() =>
                {
                    int removed = 0;
                    foreach (var pkg in toRemove)
                    {
                        try
                        {
                            if (pkg.Contains("*"))
                            {
                                if (Run.RemoveAppxPackageByPattern(pkg)) removed++;
                            }
                            else
                            {
                                if (Run.RemoveAppxPackage(pkg)) removed++;
                            }
                        }
                        catch { }
                    }
                    Dispatcher.Invoke(() =>
                    {
                        closePicker(true);
                        // Refresh tweak detection
                        InvalidateDetectCache("RemoveMoreBloat");
                        InvalidateDetectCache("Bloatware");
                        var s = I18n.Get(_lang);
                        MsgBox(s.DataMgmtTitle, $"Successfully removed {removed} application(s).", DialogType.Success);
                    });
                });
            };
            BtnAppPickerRemove.Click += removeHandler;
        }

        if (BtnAppPickerCancel != null)
        {
            cancelHandler = (_, _) => closePicker(false);
            BtnAppPickerCancel.Click += cancelHandler;
        }

        if (BtnAppPickerSelectAll != null)
        {
            selectAllHandler = (_, _) =>
            {
                bool allChecked = true;
                foreach (var child in AppPickerList.Children)
                    if (child is CheckBox cb && cb.IsChecked != true) { allChecked = false; break; }
                foreach (var child in AppPickerList.Children)
                    if (child is CheckBox cb) cb.IsChecked = !allChecked;
                UpdateCount();
            };
            BtnAppPickerSelectAll.Click += selectAllHandler;
        }

        // Wait for result using a DispatcherFrame (same pattern as ShowCustomDialog)
        var frame = new DispatcherFrame();
        System.Threading.Tasks.Task.Run(async () =>
        {
            try { await tcs.Task; }
            catch { }
            frame.Continue = false;
        });
        Dispatcher.PushFrame(frame);
        return tcs.Task.Result;
    }

    /// <summary>Populates the app-picker checkbox list with detected apps and placeholders.</summary>
    void PopulateAppPickerList(System.Collections.Generic.List<(string Pattern, string Name, string PackageFullName, bool IsPattern)> apps)
    {
        if (AppPickerList == null) return;
        AppPickerList.Children.Clear();

        // Sort detected first, then placeholders
        var sorted = new System.Collections.Generic.List<(string Pattern, string Name, string PackageFullName, bool IsPattern)>();
        foreach (var app in apps)
        {
            if (!app.IsPattern) sorted.Add(app);
        }
        foreach (var app in apps)
        {
            if (app.IsPattern) sorted.Add(app);
        }

        foreach (var app in sorted)
        {
            var cb = new CheckBox
            {
                Style = (Style)Resources["AppPickerToggleStyle"],
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4),
                Tag = app.PackageFullName,
                IsEnabled = true,
                IsChecked = !app.IsPattern,
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            
            sp.Children.Add(new TextBlock
            {
                Text = app.IsPattern ? "\xE7B8" : "\xE73E",
                FontFamily = Mdl2,
                FontSize = 13,
                Foreground = app.IsPattern 
                    ? (Brush)Application.Current.Resources["FgMuted"] 
                    : (Brush)Application.Current.Resources["Accent"],
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
            });

            sp.Children.Add(new TextBlock
            {
                Text = app.IsPattern ? $"{app.Name} (not detected)" : app.Name,
                FontSize = 12,
                FontWeight = app.IsPattern ? FontWeights.Normal : FontWeights.SemiBold,
                Foreground = app.IsPattern 
                    ? (Brush)Application.Current.Resources["FgMuted"] 
                    : (Brush)Application.Current.Resources["FgPrimary"],
                VerticalAlignment = VerticalAlignment.Center,
            });

            cb.Content = sp;
            cb.Checked += (_, _) => { PlaySoundEffect(); UpdateAppPickerCount(); };
            cb.Unchecked += (_, _) => { PlaySoundEffect(); UpdateAppPickerCount(); };
            AppPickerList.Children.Add(cb);
        }
    }

    void UpdateAppPickerCount()
    {
        if (AppPickerList == null || AppPickerCount == null || BtnAppPickerRemove == null) return;
        int checkedCount = 0;
        foreach (var child in AppPickerList.Children)
        {
            if (child is CheckBox cb && cb.IsChecked == true) checkedCount++;
        }
        AppPickerCount.Text = checkedCount == 0 ? "" : $"{checkedCount} selected";
        BtnAppPickerRemove.IsEnabled = checkedCount > 0;
    }

    /// <summary>True if a tweak should open the app-picker instead of a normal toggle.</summary>
    static bool IsAppPickerTweak(string tweakId)
        => tweakId == "RemoveMoreBloat" || tweakId == "Bloatware";

    // ============================================================
    // LANGUAGE
    // ============================================================
    // v2.6: Language selector is now a dropdown ContextMenu that slides out
    // showing all 9 languages with flags + native names. Previously it was
    // click-to-cycle which forced the user to click repeatedly to find their
    // language.
    void WireLanguageButton()
    {
        if (BtnLang == null) return;
        UpdateLangButtonLabel();
        BtnLang.Click += (_, _) => ShowLanguageModal();
    }

    private void OnBtnLangClick(object sender, RoutedEventArgs e)
    {
        ShowLanguageModal();
    }

    private void ShowLanguageModal()
    {
        if (LanguageModal != null)
            LanguageModal.Visibility = Visibility.Visible;
    }

    private void OnLanguageModalClose(object sender, RoutedEventArgs e)
    {
        if (LanguageModal != null)
            LanguageModal.Visibility = Visibility.Collapsed;
    }

    private void OnSelectLanguagePill(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int idx))
        {
            _lang = (I18n.Language)idx;
            try { Reg.SetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "Language", (int)_lang); } catch { }
            if (BtnLang != null) UpdateLangButtonLabel();

            // Clear panel build cache so every category tab is regenerated fresh with pure language strings
            _builtPanels.Clear();
            _checkboxes.Clear();
            _cards.Clear();
            _titleTbs.Clear();
            _descTbs.Clear();
            _noteTbs.Clear();
            _actionButtons.Clear();

            ApplyLanguage();
            RebuildActivePanel();
            PlaySoundEffect();
        }
        if (LanguageModal != null)
            LanguageModal.Visibility = Visibility.Collapsed;
    }

    void UpdateLangButtonLabel()
    {
        if (BtnLang == null) return;
        // Show language code + a small dropdown chevron so the user knows it opens a menu.
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = I18n.Code(_lang),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        });
        sp.Children.Add(new TextBlock
        {
            Text = " \u25BE", // ▾ small down-pointing triangle
            FontSize = 9,
            Foreground = (Brush)FindResource("FgMuted"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(1, 1, 0, 0),
        });
        BtnLang.Content = sp;
    }

    System.Windows.Controls.ContextMenu _langMenu;

    static readonly (I18n.Language lang, string nativeName)[] LanguageEntries =
    {
        (I18n.Language.EN, "English"),
        (I18n.Language.PL, "Polski"),
        (I18n.Language.DE, "Deutsch"),
        (I18n.Language.FR, "Français"),
        (I18n.Language.ES, "Español"),
        (I18n.Language.IT, "Italiano"),
        (I18n.Language.CZ, "Čeština"),
        (I18n.Language.RU, "Русский"),
        (I18n.Language.ZH, "中文"),
    };

    void BuildLanguageMenu()
    {
        if (_langMenu != null) return;
        _langMenu = new System.Windows.Controls.ContextMenu
        {
            Padding = new Thickness(2),
            BorderBrush = (Brush)FindResource("BorderSoft"),
            BorderThickness = new Thickness(1),
        };
        foreach (var entry in LanguageEntries)
        {
            var item = new System.Windows.Controls.MenuItem
            {
                Header = $"{I18n.FlagEmoji(entry.lang)}  {entry.nativeName}",
                Tag = entry.lang,
                Padding = new Thickness(16, 8, 16, 8),
                FontSize = 12.5,
            };
            item.Click += (_, _) =>
            {
                _lang = (I18n.Language)item.Tag;
                try { Reg.SetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "Language", (int)_lang); } catch { }
                if (BtnLang != null) UpdateLangButtonLabel();
                ApplyLanguage();
                PlaySoundEffect();
            };
            _langMenu.Items.Add(item);
        }
    }

    void ApplyLanguage()
    {
        var s = I18n.Get(_lang);

        if (BtnApply != null) BtnApply.Content = IconText("\xE73E", s.BtnApply);
        if (BtnUndo != null) BtnUndo.Content = IconText("\xE7A7", s.BtnUndo);
        if (BtnSelectAll != null) BtnSelectAll.Content = IconText("\xE8B3", s.BtnSelAll);
        if (BtnDeselectAll != null) BtnDeselectAll.Content = IconText("\xE711", s.BtnDesel);
        if (TxtFlashbackHeader != null) TxtFlashbackHeader.Text = IsSystemRestoreSupported() ? s.FlashbackHeader : s.FlashbackLocalHeader;
        if (TxtFlashbackTitle != null) TxtFlashbackTitle.Text = s.FlashbackTitle;
        if (TxtFlashbackDesc != null) TxtFlashbackDesc.Text = IsSystemRestoreSupported() ? s.FlashbackDesc : s.FlashbackLocalDesc;
        if (TxtCreateRestorePointLabel != null) TxtCreateRestorePointLabel.Text = s.BtnCreateFlashback;
        if (TxtRestoreSystemLabel != null) TxtRestoreSystemLabel.Text = s.BtnRollbackFlashback;

        // Search
        if (TxtSearchToggleLabel != null) TxtSearchToggleLabel.Text = s.BtnSearch;
        if (TxtSearchPlaceholder != null) TxtSearchPlaceholder.Text = s.SearchPlaceholder;

        // Sidebar Navigation
        if (RBHome != null) RBHome.Content = s.NavHome;
        if (RBPrivacy != null) RBPrivacy.Content = s.NavPrivacy;
        if (RBPerf != null) RBPerf.Content = s.NavPerf;
        if (RBDebloat != null) RBDebloat.Content = s.NavDebloat;
        if (RBUpdates != null) RBUpdates.Content = s.NavUpdates;
        if (RBNetwork != null) RBNetwork.Content = s.NavNetwork;
        if (RBSystem != null) RBSystem.Content = s.NavSystem;
        if (RBInternet != null) RBInternet.Content = s.NavInternet;
        if (RBSecurity != null) RBSecurity.Content = s.NavSecurity;
        if (RBUI != null) RBUI.Content = s.NavUI;
        if (RBSettings != null) RBSettings.Content = s.NavSettings;
        if (RBContextMenu != null) RBContextMenu.Content = s.NavContext;

        if (RBAppearance != null) RBAppearance.Content = s.NavAppearance;
        if (RBInstaller != null) RBInstaller.Content = s.NavInstaller;
        if (RBAdvanced != null) RBAdvanced.Content = s.NavAdvanced;

        // Home page
        if (TxtHomeSysInfoTitle != null) TxtHomeSysInfoTitle.Text = s.HomeSysInfo;
        if (TxtHomeTotalTweaks != null) TxtHomeTotalTweaks.Text = s.HomeTotalTweaks;
        if (TxtHomeCategories != null) TxtHomeCategories.Text = s.HomeCategories;
        if (TxtHomeVersion != null) TxtHomeVersion.Text = s.HomeVersion;
        if (TxtHomeCatsHeader != null) TxtHomeCatsHeader.Text = s.HomeCatsHeader;
        if (TxtHomeCpuLabel != null) TxtHomeCpuLabel.Text = s.HomeCpu;
        if (TxtHomeRamLabel != null) TxtHomeRamLabel.Text = s.HomeRam;
        if (TxtHomeSysLabel != null) TxtHomeSysLabel.Text = s.HomeSys;
        if (TxtHomeGpuLabel != null) TxtHomeGpuLabel.Text = s.HomeGpu;
        if (TxtHomeGpu2Label != null) TxtHomeGpu2Label.Text = s.HomeGpu2;
        if (TxtHomeSubtitle != null) TxtHomeSubtitle.Text = s.HomeSubtitle;

        // Home - extra labels
        if (TxtHomeCreatorHeader != null) TxtHomeCreatorHeader.Text = s.HomeCreator;

        // v3.0 — Live telemetry + thank-you footer + dialog button localization
        if (TxtHomeTelemetryTitle != null) TxtHomeTelemetryTitle.Text = _lang switch
        {
            I18n.Language.PL => "Telemetria na żywo",
            I18n.Language.DE => "Echtzeit-Telemetrie",
            I18n.Language.FR => "Télémétrie en direct",
            I18n.Language.ES => "Telemetría en vivo",
            I18n.Language.IT => "Telemetria in tempo reale",
            I18n.Language.CZ => "Telemetrie živě",
            I18n.Language.RU => "Телеметрия в реальном времени",
            I18n.Language.ZH => "实时遥测",
            _ => "Live Telemetry",
        };
        if (TxtTelemetryCPU != null) TxtTelemetryCPU.Text = _lang == I18n.Language.PL ? "CPU" : "CPU";
        if (TxtTelemetryGPU != null) TxtTelemetryGPU.Text = _lang == I18n.Language.PL ? "GPU" : "GPU";
        if (TxtTelemetryRAM != null) TxtTelemetryRAM.Text = s.HomeRam;
        if (TxtThankYouFooter != null) TxtThankYouFooter.Text = _lang switch
        {
            I18n.Language.PL => "Dziękujemy za korzystanie z NoveN Tweaks! Twój system jest w dobrych rękach.",
            I18n.Language.DE => "Danke, dass Sie NoveN Tweaks nutzen! Ihr System ist in guten Händen.",
            I18n.Language.FR => "Merci d'utiliser NoveN Tweaks ! Votre système est entre de bonnes mains.",
            I18n.Language.ES => "¡Gracias por usar NoveN Tweaks! Tu sistema está en buenas manos.",
            I18n.Language.IT => "Grazie per utilizzare NoveN Tweaks! Il tuo sistema è in buone mani.",
            I18n.Language.CZ => "Děkujeme za použití NoveN Tweaks! Váš systém je v dobrých rukou.",
            I18n.Language.RU => "Спасибо за использование NoveN Tweaks! Ваша система в надежных руках.",
            I18n.Language.ZH => "感谢您使用 NoveN Tweaks！您的系统正处于良好运行状态。",
            _ => "Thank you for using NoveN Tweaks! Your system is in good hands.",
        };
        // Dialog buttons (now that the exit-confirm uses the animated dialog too)
        if (BtnDialogYes != null) BtnDialogYes.Content = _lang switch
        {
            I18n.Language.PL => "Tak", I18n.Language.DE => "Ja", I18n.Language.FR => "Oui",
            I18n.Language.ES => "Sí", I18n.Language.IT => "Sì", I18n.Language.CZ => "Ano",
            I18n.Language.RU => "Да", I18n.Language.ZH => "是", _ => "Yes",
        };
        if (BtnDialogNo != null) BtnDialogNo.Content = _lang switch
        {
            I18n.Language.PL => "Nie", I18n.Language.DE => "Nein", I18n.Language.FR => "Non",
            I18n.Language.ES => "No", I18n.Language.IT => "No", I18n.Language.CZ => "Ne",
            I18n.Language.RU => "Нет", I18n.Language.ZH => "否", _ => "No",
        };
        if (BtnDialogOK != null) BtnDialogOK.Content = _lang switch
        {
            I18n.Language.PL => "OK", I18n.Language.DE => "OK", I18n.Language.FR => "OK",
            I18n.Language.ES => "OK", I18n.Language.IT => "OK", I18n.Language.CZ => "OK",
            I18n.Language.RU => "OK", I18n.Language.ZH => "确定", _ => "OK",
        };
        if (TxtHomeYouTube != null) TxtHomeYouTube.Text = s.HomeYouTube;
        if (TxtHomeWebsite != null) TxtHomeWebsite.Text = s.HomeWebsite;
        // "Detecting..." placeholder (only if not yet filled)
        if (StatCpuName != null && (StatCpuName.Text == "Detecting..." || StatCpuName.Text == s.HomeDetecting)) StatCpuName.Text = s.HomeDetecting;
        if (StatRamInfo != null && (StatRamInfo.Text == "Detecting..." || StatRamInfo.Text == s.HomeDetecting)) StatRamInfo.Text = s.HomeDetecting;
        if (StatOsName  != null && (StatOsName.Text  == "Detecting..." || StatOsName.Text  == s.HomeDetecting)) StatOsName.Text  = s.HomeDetecting;
        if (StatGpuName != null && (StatGpuName.Text == "Detecting..." || StatGpuName.Text == s.HomeDetecting)) StatGpuName.Text = s.HomeDetecting;
        if (StatGpu2Name!= null && (StatGpu2Name.Text== "Detecting..." || StatGpu2Name.Text== s.HomeDetecting)) StatGpu2Name.Text= s.HomeDetecting;

    // Settings Page

        if (TxtSettingTheme != null) TxtSettingTheme.Text = s.SettingTheme;
        if (TxtSettingThemeSub != null) TxtSettingThemeSub.Text = s.SettingThemeSub;
        if (TxtSettingThemeDesc != null) TxtSettingThemeDesc.Text = s.SettingThemeDesc;
        if (TxtThemeDarkLabel != null) TxtThemeDarkLabel.Text = s.ThemeDark;
        if (TxtThemeNormalLabel != null) TxtThemeNormalLabel.Text = s.ThemeNormal;
        if (TxtThemeWhiteLabel != null) TxtThemeWhiteLabel.Text = s.ThemeWhite;
        if (TxtSettingBg != null) TxtSettingBg.Text = s.SettingBg;
        if (TxtSettingBgDesc != null) TxtSettingBgDesc.Text = s.SettingBgDesc;
        if (BtnBgImage != null) BtnBgImage.Content = IconText("\uE8B9", s.BtnChooseImage);
        if (BtnBgVideo != null) BtnBgVideo.Content = IconText("\uE8B2", s.BtnChooseVideo);
        if (BtnBgClear != null) BtnBgClear.Content = IconText("\uE74D", s.BtnClearBg);
        if (TxtSettingData != null) TxtSettingData.Text = s.SettingData;
        if (TxtSettingDataSub != null) TxtSettingDataSub.Text = s.SettingDataSub;
        if (TxtSettingDataDesc != null) TxtSettingDataDesc.Text = s.SettingDataDesc;
        if (BtnExport != null) BtnExport.Content = s.BtnExport;
        if (BtnImport != null) BtnImport.Content = s.BtnImport;
        if (BtnOpenFolder != null) BtnOpenFolder.Content = s.BtnOpenFolder;
        if (BtnDefender != null) BtnDefender.Content = s.BtnDefender;
        if (TxtSettingBehavior != null) TxtSettingBehavior.Text = s.SettingBehavior;
        // v2.8: Admin status row
        if (TxtAdminStatusTitle != null) TxtAdminStatusTitle.Text = s.ElevTitle;
        if (TxtAdminStatusDesc != null)
        {
            TxtAdminStatusDesc.Text = ElevationHelper.IsElevated
                ? s.ElevRunningAsAdmin
                : s.ElevBody;
        }
        if (BtnRestartAdmin != null)
        {
            BtnRestartAdmin.Content = ElevationHelper.IsElevated
                ? "\xE73E  " + s.ElevRunningAsAdmin
                : "\xE7E8  " + s.ElevRestart;
            BtnRestartAdmin.IsEnabled = !ElevationHelper.IsElevated;
            BtnRestartAdmin.Opacity = ElevationHelper.IsElevated ? 0.5 : 1.0;
        }
        if (TxtAutoStartTitle != null) TxtAutoStartTitle.Text = s.AutoStartTitle;
        if (TxtAutoStartDesc != null) TxtAutoStartDesc.Text = s.AutoStartDesc;
        if (TxtAskCloseTitle != null) TxtAskCloseTitle.Text = s.AskCloseTitle;
        if (TxtAskCloseDesc != null) TxtAskCloseDesc.Text = s.AskCloseDesc;
        if (TxtRestorePointTitle != null) TxtRestorePointTitle.Text = s.RestorePointTitle;
        if (TxtRestorePointDesc != null)
        {
            TxtRestorePointDesc.Text = IsSystemRestoreSupported()
                ? s.RestorePointDesc
                : (s.RestorePointDesc.Contains("System Restore Point")
                    ? s.RestorePointDesc.Replace("System Restore Point", "Registry Backup")
                    : s.RestorePointDesc.Replace("Punkt Przywracania Systemu", "kopię zapasową rejestru (Registry Backup)"));
        }
        // v1.4: SkipRestorePoint setting
        if (TxtSkipRestorePointTitle != null) TxtSkipRestorePointTitle.Text = s.SkipRestorePointTitle;
        if (TxtSkipRestorePointDesc != null) TxtSkipRestorePointDesc.Text = s.SkipRestorePointDesc;
        if (TxtAutoRestartTitle != null) TxtAutoRestartTitle.Text = s.AutoRestartTitle;
        if (TxtAutoRestartDesc != null) TxtAutoRestartDesc.Text = s.AutoRestartDesc;
        if (TxtSoundsTitle != null) TxtSoundsTitle.Text = s.SoundsTitle;
        if (TxtSoundsDesc != null) TxtSoundsDesc.Text = s.SoundsDesc;
        if (TxtAnimationsTitle != null) TxtAnimationsTitle.Text = s.AnimationsTitle;
        if (TxtAnimationsDesc != null) TxtAnimationsDesc.Text = s.AnimationsDesc;
        if (TxtHwAccelTitle != null) TxtHwAccelTitle.Text = s.HwAccelTitle;
        if (TxtHwAccelDesc != null) TxtHwAccelDesc.Text = s.HwAccelDesc;
        if (TxtAutoUpdateTitle != null) TxtAutoUpdateTitle.Text = s.AutoUpdateTitle;
        if (TxtAutoUpdateDesc != null) TxtAutoUpdateDesc.Text = s.AutoUpdateDesc;
        if (TxtCompactTitle != null) TxtCompactTitle.Text = s.CompactTitle;
        if (TxtCompactDesc != null) TxtCompactDesc.Text = s.CompactDesc;
        if (TxtSettingReset != null) TxtSettingReset.Text = s.SettingReset;
        if (TxtSettingResetSub != null) TxtSettingResetSub.Text = s.SettingResetSub;
        if (TxtSettingResetDesc != null) TxtSettingResetDesc.Text = s.SettingResetDesc;
        if (BtnResetSettings != null) BtnResetSettings.Content = IconText("\uE72C", s.BtnResetSettings);

        // Installer
        if (BtnInstall != null) BtnInstall.Content = IconText("\xE7E8", s.BtnInstall);
        if (BtnDownload != null) BtnDownload.Content = IconText("\xE896", s.BtnDownloadOnly);
        // Rebuild installer category headers + placeholder so they follow the active language
        if (_installerRootStack != null) BuildInstallerPanel();
        // Installer search placeholder is rebuilt with BuildInstallerPanel

        // License card (Settings)
        if (TxtSettingsLicenseHeader != null) TxtSettingsLicenseHeader.Text = s.LicenseHeader;
        if (LblLicenseStatus != null) LblLicenseStatus.Text = s.LicenseStatusActive;
        if (LblLicenseStatusSub != null) LblLicenseStatusSub.Text = s.LicenseStatusActiveSub;
        if (BtnActivateLicense != null) BtnActivateLicense.Content = IconText("\uE73E", s.LicenseActivateBtn.Replace("  \uE73E", ""));

        // Smart Optimizer Wizard scan complete panel
        if (TxtScanComplete != null) TxtScanComplete.Text = s.ScanComplete;
        if (TxtScanRecommended != null) TxtScanRecommended.Text = s.ScanRecommended;
        if (TxtScanAlreadyApplied != null) TxtScanAlreadyApplied.Text = s.ScanAlreadyApplied;
        if (TxtScanAdvanced != null) TxtScanAdvanced.Text = s.ScanAdvanced;
        if (TxtScanAdvancedHint != null) TxtScanAdvancedHint.Text = s.ScanAdvancedHint;
        if (BtnWizCancel3 != null) BtnWizCancel3.Content = IconText("\xE711", s.WizCancel);
        if (BtnWizApply != null) BtnWizApply.Content = IconText("\xE73E", s.WizApply);

        // v2.1: Wizard level selector (3-button popup)
        if (TxtWizLevelTitle != null) TxtWizLevelTitle.Text = s.WizLevelTitle;
        if (TxtWizLevelSubtitle != null) TxtWizLevelSubtitle.Text = s.WizLevelSubtitle ?? "";
        if (TxtWizMinTitle != null) TxtWizMinTitle.Text = s.WizLevelMinTitle;
        if (TxtWizMinDesc != null) TxtWizMinDesc.Text = s.WizLevelMinDesc;
        if (TxtWizStdTitle != null) TxtWizStdTitle.Text = s.WizLevelStdTitle;
        if (TxtWizStdDesc != null) TxtWizStdDesc.Text = s.WizLevelStdDesc;
        if (TxtWizFullTitle != null) TxtWizFullTitle.Text = s.WizLevelFullTitle;
        if (TxtWizFullDesc != null) TxtWizFullDesc.Text = s.WizLevelFullDesc;
        // v2.8: Wizard footer note + close button
        if (TxtWizFooterNote != null) TxtWizFooterNote.Text = "\xE7BA  " + (s.WizFooterNote ?? "");
        if (BtnWizCloseOverlay != null) BtnWizCloseOverlay.Content = s.WizCancel;

        // License activation overlay
        if (TxtLicenseActivation != null) TxtLicenseActivation.Text = s.LicenseHeader;
        if (TxtLicensePrompt != null) TxtLicensePrompt.Text = s.LicensePrompt;
        if (TxtLicenseFormat != null) TxtLicenseFormat.Text = s.LicenseKeyFormat;
        if (TxtLicenseKey != null) TxtLicenseKey.ToolTip = s.LicenseKeyFormat;

        // Disclaimer overlay
        if (TxtDisclaimerTitle != null) TxtDisclaimerTitle.Text = s.DisclaimerTitle;
        if (TxtDisclaimerBody1 != null) TxtDisclaimerBody1.Text = s.DisclaimerBody1;
        if (TxtDisclaimerBody2 != null) TxtDisclaimerBody2.Text = s.DisclaimerBody2;
        if (TxtDisclaimerBody3 != null) TxtDisclaimerBody3.Text = s.DisclaimerBody3;
        if (TxtDisclaimerCheckbox != null) TxtDisclaimerCheckbox.Text = s.DisclaimerCheckbox;
        if (BtnDisclaimerAccept != null) BtnDisclaimerAccept.Content = s.DisclaimerAccept;

        // Language picker buttons (highlight active)
        // (panel removed - language changed via topbar button)

        // Tweak cards
        foreach (var t in Tweaks.All)
        {
            if (_titleTbs.TryGetValue(t.Id, out var tb)) tb.Text = Tweaks.Name(t, (int)_lang);
            if (_descTbs.TryGetValue(t.Id, out var db)) db.Text = Tweaks.Desc(t, (int)_lang);
            if (_noteTbs.TryGetValue(t.Id, out var nb)) { var n = Tweaks.Note(t, (int)_lang); nb.Text = n ?? ""; }
            if (_actionButtons.TryGetValue(t.Id, out var btn))
            {
                bool active = false;
                lock (_tweakActiveState) { _tweakActiveState.TryGetValue(t.Id, out var val); active = val == true; }
                SetActionButtonContent(btn, active, s);
            }
        }
        if (HomeCategoryGrid != null) BuildHomeCategoryCards();
        if (LocalBackupsHeader != null)
        {
            LocalBackupsHeader.Text = _lang == I18n.Language.PL ? "Lokalne kopie zapasowe (Ostatnie 5)" : "Local Backups (Last 5)";
        }
        PopulateLocalBackupsList();
        SwitchTo(_activeCategory);
    }

    // Language picker buttons panel (built dynamically, refreshed on lang change)
    void BuildLangPickerButtons()
    {
        var panel = FindName("LangPickerPanel") as WrapPanel;
        if (panel == null) return;
        panel.Children.Clear();
        var langs = new[] { I18n.Language.EN, I18n.Language.PL, I18n.Language.DE, I18n.Language.FR, I18n.Language.ES, I18n.Language.IT, I18n.Language.CZ, I18n.Language.RU };
        foreach (var lang in langs)
        {
            var code = I18n.Code(lang);
            var flag = I18n.FlagEmoji(lang);
            var btn = new Button
            {
                Content = $"{flag}  {code}",
                Tag = lang,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(14, 8, 14, 8),
                Cursor = Cursors.Hand,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Style = (Style)FindResource(lang == _lang ? "BtnPri" : "BtnSec"),
            };
            btn.Click += (_, _) =>
            {
                _lang = (I18n.Language)btn.Tag;
                try { Reg.SetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "Language", (int)_lang); } catch { }
                if (BtnLang != null) UpdateLangButtonLabel();
                ApplyLanguage();
            };
            panel.Children.Add(btn);
        }
    }

    void RefreshLangPickerButtons()
    {
        var panel = FindName("LangPickerPanel") as WrapPanel;
        if (panel == null) return;
        if (panel.Children.Count == 0) { BuildLangPickerButtons(); return; }
        foreach (Button btn in panel.Children)
        {
            if (btn.Tag is I18n.Language lang)
                btn.Style = (Style)FindResource(lang == _lang ? "BtnPri" : "BtnSec");
        }
    }


    // ============================================================
    // HOME STATS

    // ============================================================
    void BuildHomeStats()
    {
        if (HomeTotalCount != null) HomeTotalCount.Text = Tweaks.All.Count.ToString();
        var cats = new[] { "Privacy", "Performance", "Debloat", "Updates", "Network", "Internet", "System", "Security", "UI", "Appearance", "ContextMenu", "Advanced" };
        if (HomeCatCount != null) HomeCatCount.Text = cats.Length.ToString();
        if (HomeCategoryGrid != null) BuildHomeCategoryCards();
        LoadSystemInfo();
    }

    void BuildHomeCategoryCards()
    {
        HomeCategoryGrid.Children.Clear();
        var cats = new[] { "Privacy", "Performance", "Debloat", "Updates", "Network", "Internet", "System", "Security", "UI", "Appearance", "ContextMenu", "Advanced" };
        var s = I18n.Get(_lang);
        HomeCategoryGrid.BeginInit();
        foreach (var cat in cats)
        {
            string label = cat switch
            {
                "Privacy" => s.NavPrivacy, "Performance" => s.NavPerf, "Debloat" => s.NavDebloat,
                "Updates" => s.NavUpdates, "Network" => s.NavNetwork, "Internet" => s.NavInternet,
                "System" => s.NavSystem, "Security" => s.NavSecurity, "UI" => s.NavUI,
                "Appearance" => s.NavAppearance, "ContextMenu" => s.NavContext, "Advanced" => s.NavAdvanced, _ => cat,
            };
            int count = Tweaks.All.Count(t => t.Category == cat);
            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["SurfaceMid"],
                CornerRadius = new CornerRadius(10), Padding = new Thickness(14), Margin = new Thickness(4),
                BorderBrush = (Brush)Application.Current.Resources["BorderSoft"],
                BorderThickness = new Thickness(1), Cursor = Cursors.Hand,
            };
            // Subtle hover — accent border only, no scale to prevent text blur
            card.MouseEnter += (_, _) => { card.BorderBrush = (Brush)Application.Current.Resources["BorderMid"]; };
            card.MouseLeave += (_, _) => { card.BorderBrush = (Brush)Application.Current.Resources["BorderSoft"]; };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { FontFamily = Mdl2, FontSize = 18, Foreground = (Brush)Application.Current.Resources["AccentFg"], Text = CatIcons.TryGetValue(cat, out var ic) ? ic : "\xE713", Margin = new Thickness(0, 0, 0, 8) });
            sp.Children.Add(new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = (Brush)Application.Current.Resources["FgHigh"], Text = label });
            sp.Children.Add(new TextBlock { FontSize = 10, Foreground = (Brush)Application.Current.Resources["FgMuted"], Text = count + " tweaks" });
            card.Child = sp;
            string capture = cat;
            card.MouseLeftButtonUp += (_, _) => { var rb = (RadioButton)FindName("RB" + cat); if (rb != null) rb.IsChecked = true; };
            HomeCategoryGrid.Children.Add(card);
        }
        HomeCategoryGrid.EndInit();
    }

    // ============================================================
    // SYSTEM INFO (P/Invoke + live updates)
    // ============================================================
    [StructLayout(LayoutKind.Sequential)]
    struct MEMORYSTATUSEX
    {
        public uint dwLength; public uint dwMemoryLoad; public ulong ullTotalPhys;
        public ulong ullAvailPhys; public ulong ullTotalPageFile; public ulong ullAvailPageFile;
        public ulong ullTotalVirtual; public ulong ullAvailVirtual; public ulong ullAvailExtendedVirtual;
    }
    [DllImport("dxgi.dll", SetLastError = true)]
    static extern int CreateDXGIFactory(ref Guid riid, out IntPtr ppFactory);

    [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetSystemTimes(out ulong lpIdleTime, out ulong lpKernelTime, out ulong lpUserTime);

    private static ulong _prevIdleTime;
    private static ulong _prevKernelTime;
    private static ulong _prevUserTime;

    private static double CalculateCpuUsage()
    {
        if (!GetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime)) return 0;

        ulong prevIdle = _prevIdleTime;
        ulong prevKernel = _prevKernelTime;
        ulong prevUser = _prevUserTime;

        _prevIdleTime = idleTime;
        _prevKernelTime = kernelTime;
        _prevUserTime = userTime;

        if (prevIdle == 0) return 0; // First call initialization

        ulong diffIdle = idleTime - prevIdle;
        ulong diffKernel = kernelTime - prevKernel;
        ulong diffUser = userTime - prevUser;

        ulong total = diffKernel + diffUser;
        if (total == 0) return 0;

        if (total < diffIdle) return 0;

        double usage = (double)(total - diffIdle) / total * 100.0;
        return usage < 0 ? 0 : (usage > 100 ? 100 : usage);
    }

    /// <summary>
    /// Reliably detects Windows 10 vs Windows 11.
    /// The registry "ProductName" value still reports "Windows 10" on Windows 11
    /// (Microsoft keeps it for backward compatibility), so we use the CurrentBuild
    /// number combined with WMI cross-check. Build &gt;= 22000 = Windows 11.
    /// </summary>
    static string DetectWindowsVersion()
    {
        string edition = "";
        string displayVer = "";
        int build = 0;
        int ubr = 0;

        // 1) Primary source: registry (fast, always available)
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                displayVer = key.GetValue("DisplayVersion") as string ?? "";
                var buildObj = key.GetValue("CurrentBuild");
                if (buildObj != null) int.TryParse(buildObj.ToString(), out build);

                // EditionName (Win10+) or ProductName fallback
                edition = key.GetValue("EditionId") as string
                          ?? key.GetValue("ProductName") as string
                          ?? "";
                // ProductName often is "Windows 10 Pro" even on Win11 — strip the "Windows N" prefix
                // so we can rebuild the correct label from the build number.
                if (!string.IsNullOrEmpty(edition))
                {
                    // keep just the edition token (e.g. "Pro", "Home", "Enterprise")
                    var ed = edition;
                    foreach (var prefix in new[] { "Windows 10 ", "Windows 11 ", "Windows " })
                        if (ed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { ed = ed.Substring(prefix.Length); break; }
                    edition = ed;
                }

                // UBR (Update Build Revision) for a more precise build label
                var ubrObj = key.GetValue("UBR");
                if (ubrObj != null) int.TryParse(ubrObj.ToString(), out ubr);
            }
        }
        catch { /* fall through to WMI */ }

        // 2) Decide Win10 vs Win11 from build number (authoritative)
        //    Windows 11 RTM = 22000. Anything >= 22000 is Windows 11.
        bool isWin11 = build >= 22000;

        // 3) If registry build missing, cross-check via WMI (OSVersion / Caption)
        if (build == 0)
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT Version, Caption FROM Win32_OperatingSystem");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    var ver = obj["Version"] as string;
                    if (!string.IsNullOrEmpty(ver))
                    {
                        var parts = ver.Split('.');
                        if (parts.Length >= 3 && int.TryParse(parts[2], out int b)) build = b;
                    }
                    var caption = obj["Caption"] as string;
                    if (!string.IsNullOrEmpty(caption))
                    {
                        // Caption is like "Microsoft Windows 11 Pro" or "Microsoft Windows 10 Pro"
                        isWin11 = caption.IndexOf("Windows 11", StringComparison.OrdinalIgnoreCase) >= 0 || build >= 22000;
                        if (string.IsNullOrEmpty(edition))
                        {
                            // try to pull edition out of caption
                            var c = caption;
                            foreach (var prefix in new[] { "Microsoft Windows 11 ", "Microsoft Windows 10 ", "Microsoft Windows " })
                                if (c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { c = c.Substring(prefix.Length); break; }
                            edition = c;
                        }
                    }
                }
            }
            catch { /* ignore WMI failures */ }
        }

        string major = isWin11 ? "Windows 11" : "Windows 10";
        if (build == 0) return !string.IsNullOrEmpty(edition) ? $"{major} {edition}".Trim() : major;

        string buildLabel = ubr > 0 ? $"{build}.{ubr}" : build.ToString();
        var label = !string.IsNullOrEmpty(edition) ? $"{major} {edition}".Trim() : major;
        if (!string.IsNullOrEmpty(displayVer)) label += $" {displayVer}";
        label += $" (build {buildLabel})";
        return label;
    }

    void LoadSystemInfo()
    {
        var s = I18n.Get(_lang);
        try
        {
            if (StatOsName != null)
            {
                try
                {
                    StatOsName.Text = DetectWindowsVersion();
                }
                catch { StatOsName.Text = s.FallbackOs; }
            }
            if (StatCpuName != null)
            {
                try { using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"); StatCpuName.Text = key?.GetValue("ProcessorNameString") as string ?? s.FallbackCpu; }
                catch { StatCpuName.Text = s.FallbackCpu; }
            }
            if (StatRamInfo != null)
            {
                try
                {
                    var ms = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
                    if (GlobalMemoryStatusEx(ref ms)) StatRamInfo.Text = $"{ms.ullTotalPhys / (1024.0 * 1024 * 1024):F1} GB";
                }
                catch { StatRamInfo.Text = s.FallbackRam; }
            }
            
            // Detect active GPUs using DXGI to reliably fetch LUIDs
            DetectGpusWithDxgi();

            if (_detectedGpus.Count > 0)
            {
                if (StatGpuName != null) StatGpuName.Text = _detectedGpus[0].Name;
                if (StatGpuVRAM != null) StatGpuVRAM.Text = _detectedGpus[0].Vram;
            }
            else
            {
                if (StatGpuName != null) StatGpuName.Text = s.FallbackGpu;
                if (StatGpuVRAM != null) StatGpuVRAM.Text = "";
            }

            if (_detectedGpus.Count > 1)
            {
                if (StatGpu2Name != null) StatGpu2Name.Text = _detectedGpus[1].Name;
                if (StatGpu2VRAM != null) StatGpu2VRAM.Text = _detectedGpus[1].Vram;
                if (Gpu2Section != null) Gpu2Section.Visibility = Visibility.Visible;
            }
            else
            {
                if (Gpu2Section != null) Gpu2Section.Visibility = Visibility.Collapsed;
            }

            StartSystemInfoTimer();
        }
        catch { }
    }

    void StartSystemInfoTimer()
    {
        if (_sysInfoTimer != null) return;
        _sysInfoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _sysInfoTimer.Tick += (_, _) => UpdateLiveStats();
        _sysInfoTimer.Start();
        UpdateLiveStats();
    }

    void UpdateCircularArc(System.Windows.Media.PathFigure figure, System.Windows.Media.ArcSegment arc, TextBlock textBlock, double percent)
    {
        if (figure == null || arc == null) return;

        if (percent < 0) percent = 0;
        if (percent > 100) percent = 100;

        if (textBlock != null)
        {
            textBlock.Text = $"{(int)Math.Round(percent)}%";
        }

        double r = 16.5;
        double cx = 18;
        double cy = 18;

        // v2.8 FIX: Avoid degenerate arc (start == end) which causes WPF rendering
        // artifacts — the circle would glitch/flicker, especially visible with 2 GPUs.
        // At 0% we render a tiny 0.3° sliver (visually invisible) instead of a zero-length arc.
        // At ~100% we clamp to 99.7% to avoid the near-full-circle degenerate case.
        if (percent <= 0.1)
            percent = 0.1;
        if (percent >= 99.7)
            percent = 99.7;

        double angle = -90.0 + (percent / 100.0) * 360.0;
        double rad = angle * Math.PI / 180.0;

        double x = cx + r * Math.Cos(rad);
        double y = cy + r * Math.Sin(rad);

        figure.StartPoint = new Point(cx, cy - r);
        arc.Point = new Point(x, y);
        arc.IsLargeArc = percent > 50.0;
        arc.SweepDirection = System.Windows.Media.SweepDirection.Clockwise;
    }

    void UpdateLiveStats()
    {
        if (_activeCategory != "Home") return;
        double cpuVal = 0, ramVal = 0;
        try
        {
            var ms = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref ms))
            {
                ramVal = ms.dwMemoryLoad;
                UpdateCircularArc(RamArcFigure, RamArcSegment, StatRamUsage, ms.dwMemoryLoad);
            }
        }
        catch { }

        try
        {
            if (StatOsUptime != null)
            {
                var t = TimeSpan.FromMilliseconds(Environment.TickCount64);
                var s = I18n.Get(_lang);
                StatOsUptime.Text = string.Format(s.UptimeFormat, t.Days, t.Hours, t.Minutes);
            }
        }
        catch { }

        try
        {
            cpuVal = CalculateCpuUsage();
            UpdateCircularArc(CpuArcFigure, CpuArcSegment, StatCpuUsage, cpuVal);
        }
        catch { }

        if (StatGpuUsage != null) UpdateCircularArc(GpuArcFigure, GpuArcSegment, StatGpuUsage, 0);
        if (StatGpu2Usage != null) UpdateCircularArc(Gpu2ArcFigure, Gpu2ArcSegment, StatGpu2Usage, 0);
        PushTelemetrySample(cpuVal, ramVal, 0);
    }

    // ============================================================
    // LIVE TELEMETRY GRAPH (CPU / GPU / RAM real-time line chart)
    // ============================================================
    enum TelemetrySource { CPU, GPU, RAM }
    const int TelemetryMaxPoints = 60; // ~2 minutes of history at 2s cadence
    readonly System.Collections.Generic.Queue<double> _cpuHistory = new();
    readonly System.Collections.Generic.Queue<double> _gpuHistory = new();
    readonly System.Collections.Generic.Queue<double> _ramHistory = new();
    TelemetrySource _telemetrySource = TelemetrySource.CPU;

    // --- Smooth telemetry animation (v2.8: simplified — no sliding wave) ---
    // v2.8 FIX: The previous approach lerped ALL points from old→new positions.
    // When a new sample arrived, every point shifted left, so the interpolation
    // created a weird "sliding wave" morph effect. Now we use a short 200ms ease
    // that only smooths the transition of the line shape, not a full slide.
    readonly System.Collections.Generic.List<Point> _telemetryPrevPts = new();
    readonly System.Collections.Generic.List<Point> _telemetryTargetPts = new();
    DispatcherTimer _telemetryAnimTimer;
    long _telemetryAnimStartTicks;
    readonly System.Diagnostics.Stopwatch _telemetrySw = System.Diagnostics.Stopwatch.StartNew();
    double _telemetryAnimH = 180;
    double _telemetryLastW = -1, _telemetryLastH = -1;
    const double TelemetryAnimMs = 200; // v2.8: short, clean transition (was 650)

    // v2.6 OPTIMIZATION: Cached, frozen brushes per telemetry source.
    // Avoids reallocating 3 brush objects every 2-second tick.
    Color _telemetryCachedColor;
    SolidColorBrush _telemetryLineBrush;
    LinearGradientBrush _telemetryAreaBrush;
    RadialGradientBrush _telemetryGlowBrush;

    void EnsureTelemetryBrushes(Color themeColor)
    {
        // Only rebuild when the source (and thus the color) changes.
        if (_telemetryLineBrush != null && themeColor == _telemetryCachedColor) return;
        _telemetryCachedColor = themeColor;

        _telemetryLineBrush = new SolidColorBrush(themeColor);
        _telemetryLineBrush.Freeze();

        _telemetryAreaBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        _telemetryAreaBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x30, themeColor.R, themeColor.G, themeColor.B), 0));
        _telemetryAreaBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x08, themeColor.R, themeColor.G, themeColor.B), 0.8));
        _telemetryAreaBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, themeColor.R, themeColor.G, themeColor.B), 1));
        _telemetryAreaBrush.Freeze();

        // v2.8: Simple solid brush for the endpoint dot (no radial glow)
        _telemetryGlowBrush = new RadialGradientBrush();
        _telemetryGlowBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x80, themeColor.R, themeColor.G, themeColor.B), 0));
        _telemetryGlowBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, themeColor.R, themeColor.G, themeColor.B), 1));
        _telemetryGlowBrush.Freeze();

        if (TelemetryLine != null) TelemetryLine.Stroke = _telemetryLineBrush;
        if (TelemetryArea != null) TelemetryArea.Fill = _telemetryAreaBrush;
        if (TelemetryGlowHalo != null) TelemetryGlowHalo.Fill = _telemetryGlowBrush;
        if (TelemetryGlowCore != null) TelemetryGlowCore.Fill = _telemetryLineBrush;
    }

    /// <summary>Stores a new sample in all three histories and re-renders the active series.</summary>
    void PushTelemetrySample(double cpu, double ram, double gpu)
    {
        void Push(System.Collections.Generic.Queue<double> q, double v)
        {
            double clamped = v < 0 ? 0 : (v > 100 ? 100 : v);
            // v2.6: Pre-fill the queue to TelemetryMaxPoints on the very first sample.
            // This keeps the point count constant from the start, so the animation
            // never snaps (teleports) during the initial fill-up phase. The chart
            // starts as a flat line at the first value and smoothly evolves.
            if (q.Count == 0)
            {
                for (int i = 0; i < TelemetryMaxPoints; i++) q.Enqueue(clamped);
            }
            q.Enqueue(clamped);
            while (q.Count > TelemetryMaxPoints) q.Dequeue();
        }
        Push(_cpuHistory, cpu);
        Push(_ramHistory, ram);
        Push(_gpuHistory, gpu);
        RenderTelemetryGraph();
    }

    void UpdateTelemetryTitle()
    {
        if (TxtHomeTelemetryTitle == null) return;
        TxtHomeTelemetryTitle.Text = _telemetrySource switch
        {
            TelemetrySource.CPU => StatCpuName?.Text ?? "CPU",
            TelemetrySource.GPU => StatGpuName?.Text ?? "GPU",
            _ => (StatRamInfo != null && StatRamInfo.Text != "Detecting...") ? $"System Memory ({StatRamInfo.Text})" : "Memory",
        };
    }

    /// <summary>Renders the active telemetry series as a smooth line + filled area.</summary>
    void RenderTelemetryGraph()
    {
        if (TelemetryLine == null || TelemetryCanvas == null) return;

        UpdateTelemetryTitle();

        var hist = _telemetrySource switch
        {
            TelemetrySource.CPU => _cpuHistory,
            TelemetrySource.GPU => _gpuHistory,
            _ => _ramHistory,
        };
        if (hist.Count == 0) return;

        double w = TelemetryCanvas.ActualWidth > 0 ? TelemetryCanvas.ActualWidth : 600;
        double h = TelemetryCanvas.ActualHeight > 0 ? TelemetryCanvas.ActualHeight : 180;

        DrawTelemetryGridlines(w, h);

        var pts = new System.Collections.Generic.List<Point>();
        int n = hist.Count;
        int i = 0;
        double spacing = w / (TelemetryMaxPoints - 1);
        foreach (var v in hist)
        {
            double x = w - (n - 1 - i) * spacing;
            double y = h - (h * v / 100.0);
            pts.Add(new Point(x, y));
            i++;
        }

        var themeColor = _telemetrySource switch
        {
            TelemetrySource.CPU => Color.FromRgb(0x34, 0xD3, 0x99),
            TelemetrySource.GPU => Color.FromRgb(0xF8, 0x71, 0x71),
            _ => Color.FromRgb(0xFB, 0xBF, 0x24),
        };

        // v2.6 OPTIMIZATION: Cache and freeze brushes per source.
        EnsureTelemetryBrushes(themeColor);

        // v2.8: DropShadowEffect removed from XAML — no more glow on the line.

        // Dot visibility (position is updated by the animation tick).
        if (TelemetryGlowHalo != null && TelemetryGlowCore != null && pts.Count > 0)
        {
            TelemetryGlowHalo.Visibility = Visibility.Visible;
            TelemetryGlowCore.Visibility = Visibility.Visible;
        }

        // Show the REAL latest value immediately (the line animates toward it).
        if (TelemetryCurrentVal != null && pts.Count > 0)
        {
            var cur = pts[pts.Count - 1];
            double pct = h > 0 ? (h - cur.Y) / h * 100.0 : 0;
            TelemetryCurrentVal.Text = $"{(int)Math.Round(pct)}%";
        }

        // Kick off a smooth interpolation from the previously-rendered points to the new target.
        StartTelemetryAnim(pts, w, h);
    }

    /// <summary>Begins a short ease-out interpolation from the previous frame to the new target points.</summary>
    void StartTelemetryAnim(System.Collections.Generic.List<Point> target, double w, double h)
    {
        _telemetryTargetPts.Clear();
        _telemetryTargetPts.AddRange(target);
        _telemetryAnimH = h;

        // If the canvas was resized or the point count changed (e.g. source switch),
        // snap instead of interpolating to avoid skewing the line.
        bool snap = _telemetryPrevPts.Count != target.Count
                    || Math.Abs(_telemetryLastW - w) > 1.0
                    || Math.Abs(_telemetryLastH - h) > 1.0;
        _telemetryLastW = w;
        _telemetryLastH = h;

        if (snap)
        {
            _telemetryPrevPts.Clear();
            _telemetryPrevPts.AddRange(target);
            ApplyTelemetryPoints(1.0);
            return;
        }

        if (_telemetryAnimTimer == null)
        {
            _telemetryAnimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) }; // v2.6: 50fps, smooth and lighter on CPU
            _telemetryAnimTimer.Tick += TelemetryAnimTick;
        }
        _telemetryAnimStartTicks = _telemetrySw.ElapsedTicks;
        _telemetryAnimTimer.Start();
    }

    /// <summary>DispatcherTimer tick: lerps the line/area/glow from previous to target with easeOutCubic.</summary>
    void TelemetryAnimTick(object sender, EventArgs e)
    {
        double elapsedMs = (_telemetrySw.ElapsedTicks - _telemetryAnimStartTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        double t = Math.Min(1, elapsedMs / TelemetryAnimMs);
        ApplyTelemetryPoints(t);

        if (t >= 1)
        {
            _telemetryAnimTimer.Stop();
            _telemetryPrevPts.Clear();
            _telemetryPrevPts.AddRange(_telemetryTargetPts);
        }
    }

    /// <summary>Writes the interpolated line/area/glow geometry at progress p (0..1, eased).</summary>
    void ApplyTelemetryPoints(double p)
    {
        double e = 1 - Math.Pow(1 - p, 3); // easeOutCubic

        var pc = new PointCollection();
        int n = Math.Min(_telemetryPrevPts.Count, _telemetryTargetPts.Count);
        for (int k = 0; k < n; k++)
        {
            double x = _telemetryPrevPts[k].X + (_telemetryTargetPts[k].X - _telemetryPrevPts[k].X) * e;
            double y = _telemetryPrevPts[k].Y + (_telemetryTargetPts[k].Y - _telemetryPrevPts[k].Y) * e;
            pc.Add(new Point(x, y));
        }
        // If target has more points than prev (new sample appended), append the extras at full target.
        for (int k = n; k < _telemetryTargetPts.Count; k++)
            pc.Add(_telemetryTargetPts[k]);

        TelemetryLine.Points = pc;

        if (TelemetryArea != null && pc.Count >= 2)
        {
            var area = new PointCollection();
            area.Add(new Point(pc[0].X, _telemetryAnimH));
            foreach (var pt in pc) area.Add(pt);
            area.Add(new Point(pc[pc.Count - 1].X, _telemetryAnimH));
            TelemetryArea.Points = area;
        }

        if (TelemetryGlowHalo != null && TelemetryGlowCore != null && pc.Count > 0)
        {
            var cur = pc[pc.Count - 1];
            Canvas.SetLeft(TelemetryGlowHalo, cur.X - TelemetryGlowHalo.Width / 2);
            Canvas.SetTop(TelemetryGlowHalo, cur.Y - TelemetryGlowHalo.Height / 2);
            Canvas.SetLeft(TelemetryGlowCore, cur.X - TelemetryGlowCore.Width / 2);
            Canvas.SetTop(TelemetryGlowCore, cur.Y - TelemetryGlowCore.Height / 2);
        }
    }

    /// <summary>Draws faint horizontal gridlines aligned with the Y-axis labels.</summary>
    void DrawTelemetryGridlines(double w, double h)
    {
        if (TelemetryGridCanvas == null) return;
        TelemetryGridCanvas.Children.Clear();
        var brush = Application.Current.Resources["BorderSoft"] as Brush;
        for (int i = 1; i <= 4; i++)
        {
            double y = h * i / 5.0;
            var line = new System.Windows.Shapes.Line
            {
                X1 = 0, Y1 = y, X2 = w, Y2 = y,
                Stroke = brush,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection(new double[] { 2, 4 }),
            };
            TelemetryGridCanvas.Children.Add(line);
        }
    }

    /// <summary>Toggle handler for the CPU/GPU/RAM pill buttons.</summary>
    void TelemetryToggle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        var tag = fe.Tag as string ?? "";
        _telemetrySource = tag switch
        {
            "GPU" => TelemetrySource.GPU,
            "RAM" => TelemetrySource.RAM,
            _ => TelemetrySource.CPU,
        };

        void SetPill(Border b, bool active)
        {
            if (b == null) return;
            var targetBrush = active ? FindResource("SurfaceHigh") as SolidColorBrush : null;
            var targetForeground = active ? FindResource("FgPrimary") as SolidColorBrush : FindResource("FgMuted") as SolidColorBrush;

            var targetColor = targetBrush?.Color ?? Color.FromArgb(0, 0, 0, 0);
            var targetFgColor = targetForeground?.Color ?? Color.FromRgb(0x80, 0x80, 0x80);

            if (b.Background == null || b.Background == System.Windows.Media.Brushes.Transparent)
            {
                b.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
            if (b.Background is SolidColorBrush scb)
            {
                if (scb.IsFrozen)
                {
                    b.Background = scb.Clone();
                    scb = (SolidColorBrush)b.Background;
                }
                bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
                if (!anims)
                {
                    scb.BeginAnimation(SolidColorBrush.ColorProperty, null);
                    scb.Color = targetColor;
                }
                else
                {
                    var anim = new System.Windows.Media.Animation.ColorAnimation(targetColor, new Duration(TimeSpan.FromSeconds(0.18)))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                    scb.BeginAnimation(SolidColorBrush.ColorProperty, anim);
                }
            }

            if (b.Child is TextBlock tb)
            {
                if (tb.Foreground == null)
                {
                    tb.Foreground = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));
                }
                if (tb.Foreground is SolidColorBrush tscb)
                {
                    if (tscb.IsFrozen)
                    {
                        tb.Foreground = tscb.Clone();
                        tscb = (SolidColorBrush)tb.Foreground;
                    }
                    bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
                    if (!anims)
                    {
                        tscb.BeginAnimation(SolidColorBrush.ColorProperty, null);
                        tscb.Color = targetFgColor;
                    }
                    else
                    {
                        var anim = new System.Windows.Media.Animation.ColorAnimation(targetFgColor, new Duration(TimeSpan.FromSeconds(0.18)))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                        tscb.BeginAnimation(SolidColorBrush.ColorProperty, anim);
                    }
                }
            }
        }
        SetPill(TelemetryBtnCPU, _telemetrySource == TelemetrySource.CPU);
        SetPill(TelemetryBtnGPU, _telemetrySource == TelemetrySource.GPU);
        SetPill(TelemetryBtnRAM, _telemetrySource == TelemetrySource.RAM);

        RenderTelemetryGraph();
    }

    // ============================================================
    // SETTINGS PANEL — registry key names match the original .ps1
    // ============================================================
    void WireSettings()
    {
        LoadSettingsToggles();
        WireToggle(TglAutoStart, "AutoStart");

        // v2.8: Restart as Admin button
        if (BtnRestartAdmin != null)
        {
            BtnRestartAdmin.Click += (_, _) =>
            {
                var s2 = I18n.Get(_lang);
                if (ShowCustomDialog(s2.ElevTitle, s2.ElevBody, DialogType.Warning, true))
                {
                    if (ElevationHelper.RestartAsAdmin())
                        Application.Current.Shutdown();
                }
            };
        }
        if (TglAutoStart != null)
        {
            TglAutoStart.Checked += (_, _) => ApplyAutoStartSetting(true);
            TglAutoStart.Unchecked += (_, _) => ApplyAutoStartSetting(false);
        }

        WireToggle(TglAskClose, "ShowConfirm", defaultVal: 1);
        WireToggle(TglRestorePoint, "CreateRestorePoint", defaultVal: 1);

        // v1.4: SkipRestorePoints toggle — requires explicit confirmation when enabling.
        if (TglSkipRestorePoint != null)
        {
            TglSkipRestorePoint.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SkipRestorePoints") ?? 0) == 1;
            TglSkipRestorePoint.Checked += (_, _) =>
            {
                var sC = I18n.Get(_lang);
                // v2.5: Custom danger dialog instead of default MessageBox
                var confirm = ShowCustomDialog(sC.SkipRestorePointConfirmTitle,
                    sC.SkipRestorePointConfirmBody, DialogType.Danger, true);
                if (confirm)
                {
                    try { Reg.SetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SkipRestorePoints", 1); } catch { }
                }
                else
                {
                    // User declined — revert checkbox
                    TglSkipRestorePoint.IsChecked = false;
                    try { Reg.SetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SkipRestorePoints", 0); } catch { }
                }
            };
            TglSkipRestorePoint.Unchecked += (_, _) =>
            {
                try { Reg.SetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SkipRestorePoints", 0); } catch { }
            };
        }

        WireToggle(TglAutoRestart, "AutoRestart");
        WireToggle(TglSounds, "SoundsEnabled", defaultVal: 0); // v2.8: Sound OFF by default
        WireToggle(TglAnimations, "AnimationsEnabled", defaultVal: 1);
        WireToggle(TglAutoUpdate, "AutoUpdateCheck", defaultVal: 1);

        WireToggle(TglCompact, "CompactMode");
        WireToggle(TglFlashbackMode, "FlashbackMode", defaultVal: 0);
        if (TglCompact != null)
        {
            TglCompact.Checked += (_, _) => { _builtPanels.Clear(); RebuildActivePanel(); };
            TglCompact.Unchecked += (_, _) => { _builtPanels.Clear(); RebuildActivePanel(); };
        }

        if (TglHwAccel != null)
        {
            TglHwAccel.Checked += (_, _) => { SaveSetting("HwAccelEnabled", 1); ApplyHwAccel(true); };
            TglHwAccel.Unchecked += (_, _) => { SaveSetting("HwAccelEnabled", 0); ApplyHwAccel(false); };
        }

        if (ThemeDark != null) ThemeDark.MouseLeftButtonUp += (_, _) => { SetTheme("Dark"); };
        if (ThemeNormal != null) ThemeNormal.MouseLeftButtonUp += (_, _) => { SetTheme("Normal"); };
        if (ThemeWhite != null) ThemeWhite.MouseLeftButtonUp += (_, _) => { SetTheme("White"); };

        if (BtnResetSettings != null) BtnResetSettings.Click += (_, _) =>
        {
            if (MsgBox(s.ResetConfirmTitle, s.ResetConfirmPrompt, DialogType.Question, yesNo: true))
            {
                try
                {
                    foreach (var k in new[] { "AutoStart","ShowConfirm","CreateRestorePoint","AutoRestart","SoundsEnabled","AnimationsEnabled","HwAccelEnabled","AutoUpdateCheck","CompactMode","Theme","BackgroundPath","BackgroundKind" })
                        Reg.RemoveValue("HKCU:\\SOFTWARE\\NoveNTweaks", k);
                }
                catch { }
                LoadSettingsToggles();
                SetTheme("Normal");
                ClearCustomBackground();
                LocalAccess.Reset();
                LockApp();
                ShowLicenseOverlay();
                if (TxtLicenseKey != null) TxtLicenseKey.Text = "";
            }
        };



        if (BtnDisclaimerAccept != null && DisclaimerOverlay != null)
        {
            // v2.3: Button is disabled until checkbox is checked.
            // v2.6: Warning shows every launch — no longer persisted to registry.
            BtnDisclaimerAccept.Click += (_, _) =>
            {
                if (ChkDisclaimerConfirm == null || ChkDisclaimerConfirm.IsChecked != true) return;
                AnimateHideOverlay(DisclaimerOverlay, DisclaimerBox, () =>
                {
                    // Show flashback creation dialog after warning
                    ShowPostWarningFlashbackDialog();
                });
                // Reset checkbox for next launch
                ChkDisclaimerConfirm.IsChecked = false;
            };
        }
        // v2.3: Checkbox enables the accept button.
        if (ChkDisclaimerConfirm != null && BtnDisclaimerAccept != null)
        {
            ChkDisclaimerConfirm.Checked += (_, _) => BtnDisclaimerAccept.IsEnabled = true;
            ChkDisclaimerConfirm.Unchecked += (_, _) => BtnDisclaimerAccept.IsEnabled = false;
        }

        // NOTE: BtnDialogYes/No/OK click handlers are registered per-call inside
        // ShowCustomDialog() so they can run the smooth fade-out animation.
        // Do NOT add global collapse handlers here — they would fight the animation.

        if (BtnExport != null) BtnExport.Click += (_, _) => DoExportSettings();
        if (BtnImport != null) BtnImport.Click += (_, _) => DoImportSettings();
        if (BtnOpenFolder != null) BtnOpenFolder.Click += (_, _) =>
        {
            try
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                if (System.IO.Directory.Exists(exeDir))
                    Process.Start(new ProcessStartInfo { FileName = exeDir, UseShellExecute = true });
            }
            catch { }
        };
        if (BtnDefender != null) BtnDefender.Click += (_, _) => DoDefenderExclusion();
        if (BtnCreateRestorePoint != null) BtnCreateRestorePoint.Click += (_, _) => DoCreateRestorePoint();
        if (BtnRestoreSystem != null) BtnRestoreSystem.Click += (_, _) => DoRestoreSystem();

        // Load background settings
        WireBackgroundSettings();

        // Load saved theme on startup
        try
        {
            var savedTheme = Reg.GetString("HKCU:\\SOFTWARE\\NoveNTweaks", "Theme");
            if (savedTheme == "Light" || string.IsNullOrWhiteSpace(savedTheme)) savedTheme = "Normal";
            SetTheme(savedTheme);
        }
        catch { }
    }

    // ============================================================
    // LICENSE SYSTEM
    // ============================================================

    void WireLicenseSystem()
    {
        if (TxtLicenseKey != null)
        {
            TxtLicenseKey.TextChanged += (_, _) => LblLicenseError.Text = "";
            TxtLicenseKey.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Return && BtnActivateLicense.IsEnabled)
                {
                    BtnActivateLicense.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                }
            };
        }

        BtnActivateLicense.Click += (_, _) =>
        {
            if (!LocalAccess.TryEnter(TxtLicenseKey.Text))
            {
                LblLicenseError.Text = _lang == I18n.Language.PL
                    ? "Wpisz dowolny tekst, aby wejść." : "Enter any text to continue.";
                TxtLicenseKey.Focus();
                return;
            }

            BtnActivateLicense.IsEnabled = false;
            TxtLicenseKey.Clear();
            UnlockApp();
            UpdateLicenseStatusDisplay();
            AnimateHideOverlay(LicenseOverlay, LicenseBox, () =>
            {
                BtnActivateLicense.IsEnabled = true;
                if (_licFirstRun)
                {
                    _licFirstRun = false;
                    AnimateShowOverlay(DisclaimerOverlay, DisclaimerBox);
                }
            });
        };
    }

    System.Threading.Tasks.Task RunLicenseStartupCheckAsync()
    {
        _licFirstRun = true;
        UpdateLicenseStatusDisplay();
        if (LocalAccess.HasEntered)
        {
            _licFirstRun = false;
            UnlockApp();
            AnimateShowOverlay(DisclaimerOverlay, DisclaimerBox);
        }
        else
        {
            LockApp();
            ShowLicenseOverlay();
        }
        return System.Threading.Tasks.Task.CompletedTask;
    }

    void UpdateLicenseStatusDisplay()
    {
        if (LblLicenseStatus != null) LblLicenseStatus.Text = s.LicenseStatusActive;
        if (LblLicenseStatusSub != null) LblLicenseStatusSub.Text = s.LicenseStatusActiveSub;
    }

    void ShowLicenseOverlay()
    {
        if (LicenseOverlay != null)
        {
            AnimateShowOverlay(LicenseOverlay, LicenseBox);
            TxtLicenseKey?.Focus();
        }
    }

    void HideLicenseOverlay()
    {
        AnimateHideOverlay(LicenseOverlay, LicenseBox);
    }

    void LockApp()
    {
        if (BtnApply != null) BtnApply.IsEnabled = false;
        if (BtnUndo != null) BtnUndo.IsEnabled = false;
        if (BtnInstall != null) BtnInstall.IsEnabled = false;
        if (BtnDownload != null) BtnDownload.IsEnabled = false;
    }

    void UnlockApp()
    {
        if (BtnApply != null) BtnApply.IsEnabled = true;
        if (BtnUndo != null) BtnUndo.IsEnabled = true;
        if (BtnInstall != null) BtnInstall.IsEnabled = true;
        if (BtnDownload != null) BtnDownload.IsEnabled = true;
    }

    void SetTheme(string theme)
    {
        try
        {
            if (RootBorder == null) return;

            // Swap the full semantic palette so every DynamicResource binding
            // (cards, text, borders, buttons, overlays...) follows the theme.
            bool white = (theme == "White");
            ApplyPalette(white ? WhitePalette : DarkPalette);

            // Pure "Dark" uses a slightly different root + hides ambient orbs;
            // "Normal" keeps the default glass root + visible orbs.
            var r = Application.Current.Resources;
            if (theme == "Dark")
            {
                r["RootBg"] = Freeze(new SolidColorBrush(Color.FromRgb(0x0E, 0x0E, 0x0E)));
                if (OrbCanvas != null) OrbCanvas.Visibility = Visibility.Collapsed;
            }
            else if (theme == "Normal")
            {
                if (OrbCanvas != null) OrbCanvas.Visibility = Visibility.Visible;
            }
            else // White
            {
                if (OrbCanvas != null) OrbCanvas.Visibility = Visibility.Collapsed;
            }

            // Visual feedback for the theme selector cards (theme-aware)
            var selColor   = (Brush)Application.Current.Resources["AccentSoft"];
            var unselColor = (Brush)Application.Current.Resources["SurfaceMid"];
            if (ThemeDark != null)   ThemeDark.Background   = theme == "Dark"   ? selColor : unselColor;
            if (ThemeNormal != null) ThemeNormal.Background = theme == "Normal" ? selColor : unselColor;
            if (ThemeWhite != null)  ThemeWhite.Background  = theme == "White"  ? selColor : unselColor;

            // Rebuild home category cards so their code-built brushes follow the new theme
            try { if (HomeCategoryGrid != null) BuildHomeCategoryCards(); } catch { }

            // Force tweak category panels to rebuild with the new palette on next visit
            // (cards capture brushes at build time, so a stale build would keep old colours)
            try { _builtPanels.Clear(); } catch { }

            Reg.SetString("HKCU:\\SOFTWARE\\NoveNTweaks", "Theme", theme);
        }
        catch { }
    }

    void WireBackgroundSettings()
    {
        if (BtnBgImage != null) BtnBgImage.Click += (_, _) => ChooseCustomBackground("Image");
        if (BtnBgVideo != null) BtnBgVideo.Click += (_, _) => ChooseCustomBackground("Video");
        if (BtnBgClear != null) BtnBgClear.Click += (_, _) => ClearCustomBackground();

        // Load saved background on startup
        try
        {
            var path = Reg.GetString("HKCU:\\SOFTWARE\\NoveNTweaks", "BackgroundPath");
            var kind = Reg.GetString("HKCU:\\SOFTWARE\\NoveNTweaks", "BackgroundKind");
            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                ApplyCustomBackground(path, kind);
            }
        }
        catch { }
    }

    void ChooseCustomBackground(string kind)
    {
        try
        {
            var filter = kind == "Video" 
                ? "Video files|*.mp4;*.webm;*.avi;*.wmv" 
                : "Image files|*.png;*.jpg;*.jpeg;*.bmp";
            var dlg = new OpenFileDialog { Filter = filter };
            if (dlg.ShowDialog() == true)
            {
                var path = dlg.FileName;
                Reg.SetString("HKCU:\\SOFTWARE\\NoveNTweaks", "BackgroundPath", path);
                Reg.SetString("HKCU:\\SOFTWARE\\NoveNTweaks", "BackgroundKind", kind);
                ApplyCustomBackground(path, kind);
            }
        }
        catch (Exception ex) { MsgBox(s.ErrorTitle, $"{s.ErrorTitle}: {ex.Message}", DialogType.Error); }
    }

    void ClearCustomBackground()
    {
        try
        {
            Reg.RemoveValue("HKCU:\\SOFTWARE\\NoveNTweaks", "BackgroundPath");
            Reg.RemoveValue("HKCU:\\SOFTWARE\\NoveNTweaks", "BackgroundKind");
            ApplyCustomBackground("", "");
        }
        catch { }
    }

    private RoutedEventHandler _bgVideoMediaEndedHandler;

    void ApplyCustomBackground(string path, string kind)
    {
        try
        {
            if (BgImage == null || BgVideo == null || BgOverlay == null || OrbCanvas == null) return;

            BgImage.Visibility = Visibility.Collapsed;
            BgVideo.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                BgOverlay.Visibility = Visibility.Collapsed;
                if (LblBgPath != null) LblBgPath.Text = s.BgNoSelection;
                OrbCanvas.Visibility = Visibility.Visible;
                return;
            }

            if (kind == "Video")
            {
                BgVideo.Source = new Uri(path);
                BgVideo.Visibility = Visibility.Visible;

                if (_bgVideoMediaEndedHandler != null)
                {
                    try { BgVideo.MediaEnded -= _bgVideoMediaEndedHandler; } catch { }
                }

                _bgVideoMediaEndedHandler = (s, e) =>
                {
                    try
                    {
                        BgVideo.Position = TimeSpan.Zero;
                        BgVideo.Play();
                    }
                    catch { }
                };

                BgVideo.MediaEnded += _bgVideoMediaEndedHandler;
                try { BgVideo.Play(); } catch { }
            }
            else
            {
                var bmp = new System.Windows.Media.Imaging.BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                BgImage.Source = bmp;
                BgImage.Visibility = Visibility.Visible;
            }

            BgOverlay.Visibility = Visibility.Visible;
            OrbCanvas.Visibility = Visibility.Collapsed;

            if (LblBgPath != null)
            {
                var shortPath = path;
                if (shortPath.Length > 60) shortPath = "..." + shortPath.Substring(shortPath.Length - 57);
                LblBgPath.Text = $"{kind} : {shortPath}";
            }
        }
        catch (Exception ex)
        {
            MsgBox(s.BgErrorTitle, string.Format(s.BgLoadError, ex.Message), DialogType.Error);
        }
    }

    void DoExportSettings()
    {
        try
        {
            var dlg = new SaveFileDialog { Filter = "Registry Backup (*.reg)|*.reg", FileName = "NoveN_Settings_Backup.reg" };
            if (dlg.ShowDialog() == true)
            {
                var path = dlg.FileName;
                var code = Run.Hidden($"reg.exe export HKCU\\Software\\NoveNTweaks \"{path}\" /y");
                if (code == 0)
                    MsgBox(s.DataMgmtTitle, string.Format(s.ExportSuccess, path), DialogType.Success);
                else
                    MsgBox(s.ErrorTitle, s.ExportFailed, DialogType.Error);
            }
        }
        catch (Exception ex) { MsgBox(s.ErrorTitle, string.Format(s.ExportFailedWithTitle, ex.Message), DialogType.Error); }
    }

    void DoImportSettings()
    {
        try
        {
            var dlg = new OpenFileDialog { Filter = "Registry Backup (*.reg)|*.reg" };
            if (dlg.ShowDialog() == true)
            {
                var path = dlg.FileName;
                var code = Run.Hidden($"reg.exe import \"{path}\"");
                if (code == 0)
                {
                    MsgBox(s.DataMgmtTitle, s.ImportSuccess, DialogType.Success);
                    LoadSettingsToggles();
                }
                else
                    MsgBox(s.ErrorTitle, s.ImportFailed, DialogType.Error);
            }
        }
        catch (Exception ex) { MsgBox(s.ErrorTitle, string.Format(s.ImportFailedWithTitle, ex.Message), DialogType.Error); }
    }

    void DoDefenderExclusion()
    {
        try
        {
            var s = I18n.Get(_lang);
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var msg = s.DefenderExclTitle.ToUpper() + "\n\n" +
                      s.DefenderExclBody +
                      s.DefenderExclStep1Line1 + "\n" +
                      string.Format(s.DefenderExclStep1Line2, appDir) +
                      s.DefenderExclStep2Line1 + "\n" +
                      s.DefenderExclStep2Line2 + "\n" +
                      s.DefenderExclStep2Line3 + "\n" +
                      s.DefenderExclStep2Line4 +
                      s.DefenderExclQuestion;

            bool result = MsgBox(s.DefenderExclTitle, msg, DialogType.Question, yesNo: true);
            if (result)
            {
                var cmd = $"Add-MpPreference -ExclusionPath '{appDir}' -Force; Start-Sleep -Seconds 1; exit";
                var bytes = System.Text.Encoding.Unicode.GetBytes(cmd);
                var encoded = Convert.ToBase64String(bytes);
                var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}")
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var proc = Process.Start(psi);
                proc?.WaitForExit(15000);

                bool excluded = false;
                try
                {
                    var verifyPsi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"(Get-MpPreference).ExclusionPath\"")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    };
                    var verifyProc = Process.Start(verifyPsi);
                    if (verifyProc != null)
                    {
                        var output = verifyProc.StandardOutput.ReadToEnd();
                        verifyProc.WaitForExit();
                        if (output.Contains(appDir)) excluded = true;
                    }
                }
                catch { }

                if (excluded)
                {
                    MsgBox(s.DefenderExclSuccessTitle, s.DefenderExclSuccess, DialogType.Success);
                }
                else
                {
                    MsgBox(s.DefenderExclManualTitle, string.Format(s.DefenderExclManual, appDir), DialogType.Warning);
                    Process.Start(new ProcessStartInfo("cmd.exe", "/c start windowsdefender://threatsettings") { CreateNoWindow = true, UseShellExecute = false });
                }
            }
        }
        catch (Exception ex)
        {
            var s = I18n.Get(_lang);
            MsgBox(s.DefenderExclErrorTitle, $"{s.DefenderExclErrorTitle}: {ex.Message}", DialogType.Error);
        }
    }

    private bool IsSystemRestoreSupported()
    {
        try
        {
            string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string rstruiPath = System.IO.Path.Combine(system32, "rstrui.exe");
            string srclientPath = System.IO.Path.Combine(system32, "srclient.dll");
            return System.IO.File.Exists(rstruiPath) || System.IO.File.Exists(srclientPath);
        }
        catch
        {
            return false;
        }
    }

    private string GetBackupsDir()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return System.IO.Path.Combine(appData, "NoveNTweaks", "backups");
    }

    private bool CreateLocalRegistryBackup(string description)
    {
        try
        {
            string backupsDir = GetBackupsDir();
            System.IO.Directory.CreateDirectory(backupsDir);

            string prefix = description.Contains("Emergency") ? "Emergency_Base" : "Backup";
            string filename = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.nvnbak";
            string filepath = System.IO.Path.Combine(backupsDir, filename);

            var values = new Dictionary<string, object>();
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\NoveNTweaks"))
            {
                if (key != null)
                {
                    foreach (var valName in key.GetValueNames())
                    {
                        values[valName] = key.GetValue(valName);
                    }
                }
            }

            var activeSystemTweaks = new List<string>();
            foreach (var t in Tweaks.All)
            {
                try
                {
                    if (t.Detect != null && t.Detect())
                    {
                        activeSystemTweaks.Add(t.Id);
                    }
                }
                catch { }
            }

            using (var sw = new System.IO.StreamWriter(filepath))
            {
                sw.WriteLine("NoveNTweaks Backup v2");
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sw.WriteLine(description);
                sw.WriteLine($"SystemAppliedTweaks|{string.Join(",", activeSystemTweaks)}");

                foreach (var kvp in values)
                {
                    string name = kvp.Key;
                    object val = kvp.Value;
                    string typeStr = "";
                    string valStr = "";

                    if (val is int || val is uint)
                    {
                        typeStr = "DWORD";
                        valStr = val.ToString();
                    }
                    else if (val is string s)
                    {
                        typeStr = "SZ";
                        valStr = s;
                    }
                    else if (val is string[] arr)
                    {
                        typeStr = "MULTI_SZ";
                        valStr = string.Join("\n", arr);
                    }
                    else
                    {
                        continue;
                    }

                    sw.WriteLine($"{typeStr}|{name}|{valStr}");
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create local registry backup: {ex.Message}");
            return false;
        }
    }

    private bool RestoreLocalRegistryBackup(string filepath)
    {
        try
        {
            if (!System.IO.File.Exists(filepath)) return false;

            var newValues = new Dictionary<string, (string type, string val)>();
            string description = "";
            var targetSystemApplied = new HashSet<string>();
            bool hasSystemApplied = false;

            using (var sr = new System.IO.StreamReader(filepath))
            {
                string header = sr.ReadLine();
                if (header != "NoveNTweaks Backup v1" && header != "NoveNTweaks Backup v2") return false;

                string timestamp = sr.ReadLine();
                description = sr.ReadLine();

                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (header == "NoveNTweaks Backup v2" && line.StartsWith("SystemAppliedTweaks|"))
                    {
                        var partsSystem = line.Split('|');
                        if (partsSystem.Length >= 2)
                        {
                            var list = partsSystem[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var item in list) targetSystemApplied.Add(item.Trim());
                            hasSystemApplied = true;
                        }
                        continue;
                    }

                    var parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        string type = parts[0];
                        string name = parts[1];
                        string val = parts[2];
                        newValues[name] = (type, val);
                    }
                }
            }

            if (!hasSystemApplied)
            {
                if (newValues.TryGetValue("AppliedTweaks", out var appTweaksData))
                {
                    var list = appTweaksData.val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in list) targetSystemApplied.Add(item.Trim());
                }
            }

            var currentSystemApplied = new HashSet<string>();
            foreach (var t in Tweaks.All)
            {
                try
                {
                    if (t.Detect != null && t.Detect())
                    {
                        currentSystemApplied.Add(t.Id);
                    }
                }
                catch { }
            }

            foreach (var tweakId in currentSystemApplied)
            {
                if (!targetSystemApplied.Contains(tweakId))
                {
                    var t = Tweaks.All.Find(x => x.Id == tweakId);
                    if (t != null)
                    {
                        try { t.Undo(); } catch { }
                    }
                }
            }

            foreach (var tweakId in targetSystemApplied)
            {
                if (!currentSystemApplied.Contains(tweakId))
                {
                    var t = Tweaks.All.Find(x => x.Id == tweakId);
                    if (t != null)
                    {
                        try { t.Apply(); } catch { }
                    }
                }
            }

            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\NoveNTweaks", true))
            {
                if (key != null)
                {
                    foreach (var kvp in newValues)
                    {
                        string name = kvp.Key;
                        var (type, val) = kvp.Value;
                        if (type == "DWORD")
                        {
                            if (uint.TryParse(val, out uint ui))
                                key.SetValue(name, ui, RegistryValueKind.DWord);
                        }
                        else if (type == "SZ")
                        {
                            key.SetValue(name, val, RegistryValueKind.String);
                        }
                    }
                }
            }

            lock (_appliedTweaks)
            {
                _appliedTweaks.Clear();
                foreach (var id in targetSystemApplied) _appliedTweaks.Add(id);
            }

            Dispatcher.Invoke(() => {
                LoadSettingsToggles();
                RefreshActiveCards();
                UpdateStatus();
                InvalidateDetectCache();
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to restore local registry backup: {ex.Message}");
            return false;
        }
    }

    private void PopulateLocalBackupsList()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(PopulateLocalBackupsList);
            return;
        }

        if (LocalBackupsContainer == null || LocalBackupsHeader == null) return;
        LocalBackupsContainer.Children.Clear();

        try
        {
            string backupsDir = GetBackupsDir();
            if (!System.IO.Directory.Exists(backupsDir))
            {
                LocalBackupsHeader.Visibility = Visibility.Collapsed;
                return;
            }

            var files = System.IO.Directory.GetFiles(backupsDir, "*.nvnbak")
                .Select(f => new System.IO.FileInfo(f))
                .OrderByDescending(fi => fi.CreationTime)
                .Take(10)
                .ToList();

            if (files.Count == 0)
            {
                LocalBackupsHeader.Visibility = Visibility.Collapsed;
                return;
            }

            LocalBackupsHeader.Visibility = Visibility.Visible;
            var s = I18n.Get(_lang);

            foreach (var fi in files)
            {
                string desc = "NoveN Backup";
                string timestampStr = fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                bool isEmergency = fi.Name.StartsWith("Emergency_Base") || fi.Name.Contains("Emergency");

                try
                {
                    using (var reader = new System.IO.StreamReader(fi.FullName))
                    {
                        string header = reader.ReadLine();
                        if (header == "NoveNTweaks Backup v1" || header == "NoveNTweaks Backup v2")
                        {
                            string ts = reader.ReadLine();
                            if (!string.IsNullOrEmpty(ts)) timestampStr = ts;
                            string d = reader.ReadLine();
                            if (!string.IsNullOrEmpty(d))
                            {
                                desc = d;
                                if (d.Contains("Emergency")) isEmergency = true;
                            }
                        }
                    }
                }
                catch { }

                if (isEmergency)
                {
                    desc = (_lang == I18n.Language.PL ? "🚨 KOPIA AWARYJNA (Baza)" : "🚨 EMERGENCY BACKUP (Base)") + $" - {desc}";
                }

                var rowBorder = new System.Windows.Controls.Border
                {
                    CornerRadius = new System.Windows.CornerRadius(10),
                    Padding = new System.Windows.Thickness(12, 8, 12, 8),
                    Margin = new System.Windows.Thickness(0, 0, 0, 8),
                    BorderThickness = new System.Windows.Thickness(1)
                };
                rowBorder.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, isEmergency ? "AccentSoft" : "SurfaceLow");
                rowBorder.SetResourceReference(System.Windows.Controls.Border.BorderBrushProperty, isEmergency ? "Accent" : "BorderSoft");

                var grid = new System.Windows.Controls.Grid();
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });

                var iconTxt = new System.Windows.Controls.TextBlock
                {
                    Text = isEmergency ? "\xE7BA" : "\xE8A5",
                    FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                    FontSize = 14,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Margin = new System.Windows.Thickness(0, 0, 12, 0)
                };
                iconTxt.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, isEmergency ? "Accent" : "FgPrimary");
                System.Windows.Controls.Grid.SetColumn(iconTxt, 0);
                grid.Children.Add(iconTxt);

                var infoStack = new System.Windows.Controls.StackPanel { VerticalAlignment = System.Windows.VerticalAlignment.Center };
                var dateTxt = new System.Windows.Controls.TextBlock
                {
                    Text = timestampStr,
                    FontSize = 11,
                    FontWeight = System.Windows.FontWeights.SemiBold
                };
                dateTxt.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "FgPrimary");

                var descTxt = new System.Windows.Controls.TextBlock
                {
                    Text = desc,
                    FontSize = 9
                };
                descTxt.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "FgMuted");

                infoStack.Children.Add(dateTxt);
                infoStack.Children.Add(descTxt);
                System.Windows.Controls.Grid.SetColumn(infoStack, 1);
                grid.Children.Add(infoStack);

                var btnView = new System.Windows.Controls.Button
                {
                    Content = _lang == I18n.Language.PL ? "Zobacz" : "View",
                    Style = (System.Windows.Style)FindResource("BtnSec"),
                    Height = 24,
                    FontSize = 9,
                    Padding = new System.Windows.Thickness(8, 0, 8, 0),
                    Margin = new System.Windows.Thickness(0, 0, 8, 0),
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                string filePath = fi.FullName;
                btnView.Click += (o, e) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{filePath}\"") { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MsgBox(s.ErrorTitle, $"Failed to open file: {ex.Message}", DialogType.Error);
                    }
                };
                System.Windows.Controls.Grid.SetColumn(btnView, 2);
                grid.Children.Add(btnView);

                var btnDelete = new System.Windows.Controls.Button
                {
                    Content = _lang == I18n.Language.PL ? "Usuń" : "Delete",
                    Style = (System.Windows.Style)FindResource("BtnSec"),
                    Height = 24,
                    FontSize = 9,
                    Padding = new System.Windows.Thickness(8, 0, 8, 0),
                    Margin = new System.Windows.Thickness(0, 0, 8, 0),
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Cursor = isEmergency ? System.Windows.Input.Cursors.No : System.Windows.Input.Cursors.Hand,
                    IsEnabled = !isEmergency
                };
                if (!isEmergency)
                {
                    btnDelete.Click += (o, e) =>
                    {
                        string confirmMsg = _lang == I18n.Language.PL
                            ? "Czy na pewno chcesz trwale usunąć ten punkt przywracania?"
                            : "Are you sure you want to permanently delete this restore point?";
                        string confirmTitle = _lang == I18n.Language.PL ? "Potwierdź usunięcie" : "Confirm Delete";

                        if (MsgBox(confirmTitle, confirmMsg, DialogType.Warning, yesNo: true))
                        {
                            try
                            {
                                System.IO.File.Delete(filePath);
                                PlaySoundEffect();
                                PopulateLocalBackupsList();
                            }
                            catch (Exception ex)
                            {
                                MsgBox(s.ErrorTitle, $"Failed to delete file: {ex.Message}", DialogType.Error);
                            }
                        }
                    };
                }
                System.Windows.Controls.Grid.SetColumn(btnDelete, 3);
                grid.Children.Add(btnDelete);

                var btnRestore = new System.Windows.Controls.Button
                {
                    Content = _lang == I18n.Language.PL ? "Przywróć" : "Restore",
                    Style = (System.Windows.Style)FindResource("BtnAccent"),
                    Height = 24,
                    FontSize = 9,
                    Padding = new System.Windows.Thickness(10, 0, 10, 0),
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btnRestore.Click += (o, e) =>
                {
                    if (MsgBox(s.ConfirmTitle, s.ConfirmApply, DialogType.Question, yesNo: true))
                    {
                        bool restored = RestoreLocalRegistryBackup(filePath);
                        if (restored)
                        {
                            MsgBox(s.FlashbackLocalRestoreSuccessTitle, s.FlashbackLocalRestoreSuccess, DialogType.Success);
                            PopulateLocalBackupsList();
                        }
                        else
                        {
                            MsgBox(s.ErrorTitle, "Failed to restore the selected backup.", DialogType.Error);
                        }
                    }
                };
                System.Windows.Controls.Grid.SetColumn(btnRestore, 4);
                grid.Children.Add(btnRestore);

                rowBorder.Child = grid;
                LocalBackupsContainer.Children.Add(rowBorder);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to populate backups list: {ex.Message}");
        }
    }

    void CheckAndCreateEmergencyFlashback()
    {
        var created = Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "FirstLaunchFlashback") ?? 0;
        if (created == 1) return;

        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                bool success = false;
                if (IsSystemRestoreSupported())
                {
                    success = await CreateRestorePointAsync("NoveN Tweaks Emergency Base");
                }
                else
                {
                    success = CreateLocalRegistryBackup("NoveN Tweaks Emergency Base");
                }

                if (success)
                {
                    Reg.SetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "FirstLaunchFlashback", 1);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create emergency base restore point: {ex.Message}");
            }
        });
    }

    // ============================================================
    // v2.6: POST-WARNING FLASHBACK CREATION DIALOG
    // Shows after the disclaimer is accepted on every launch.
    // ============================================================
    void ShowPostWarningFlashbackDialog()
    {
        if (FlashbackPromptOverlay == null) return;

        var ls = I18n.Get(_lang);

        // Apply I18n texts
        if (TxtFlashbackPromptTitle != null) TxtFlashbackPromptTitle.Text = ls.PostFlashbackTitle;
        if (TxtFlashbackPromptDesc != null) TxtFlashbackPromptDesc.Text = ls.PostFlashbackDesc;
        if (BtnFlashbackPromptCreate != null) BtnFlashbackPromptCreate.Content = ls.PostFlashbackCreate;
        if (BtnFlashbackPromptSkip != null) BtnFlashbackPromptSkip.Content = ls.PostFlashbackSkip;

        // Show overlay with slide+fade animation
        AnimateShowOverlay(FlashbackPromptOverlay, FlashbackPromptBox);

        // Wire button handlers (one-shot — remove after use)
        RoutedEventHandler createHandler = null, skipHandler = null;

        void closeOverlay()
        {
            if (BtnFlashbackPromptCreate != null && createHandler != null) BtnFlashbackPromptCreate.Click -= createHandler;
            if (BtnFlashbackPromptSkip != null && skipHandler != null) BtnFlashbackPromptSkip.Click -= skipHandler;
            AnimateHideOverlay(FlashbackPromptOverlay, FlashbackPromptBox);
        }

        createHandler = (_, _) =>
        {
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string description = $"NoveN Tweaks Flashback ({dateStr})";
            closeOverlay();

            // Create flashback in background
            System.Threading.Tasks.Task.Run(async () =>
            {
                bool success = false;
                if (IsSystemRestoreSupported())
                {
                    success = await CreateRestorePointAsync(description);
                }
                else
                {
                    success = CreateLocalRegistryBackup(description);
                }

                Dispatcher.Invoke(() =>
                {
                    if (success)
                    {
                        MsgBox(ls.FlashbackCreateSuccessTitle ?? "Flashback Created",
                               ls.FlashbackCreateSuccess ?? "Flashback point created successfully!",
                               DialogType.Success);
                    }
                    else
                    {
                        string errMsg = !string.IsNullOrEmpty(_lastRestorePointError)
                            ? _lastRestorePointError
                            : (ls.FlashbackCreateFailed ?? "Failed to create flashback point.");
                        MsgBox(ls.FlashbackCreateFailedTitle ?? "Flashback Failed", errMsg, DialogType.Error);
                    }
                });
            });
        };

        skipHandler = (_, _) =>
        {
            // Show "Are you sure?" confirmation
            bool confirmSkip = MsgBox(ls.PostFlashbackSkipConfirmTitle, ls.PostFlashbackSkipConfirmBody, DialogType.Warning, yesNo: true);
            if (confirmSkip)
            {
                closeOverlay();
            }
            // If they click No, the flashback dialog stays open
        };

        if (BtnFlashbackPromptCreate != null) BtnFlashbackPromptCreate.Click += createHandler;
        if (BtnFlashbackPromptSkip != null) BtnFlashbackPromptSkip.Click += skipHandler;
    }

    private static string _lastRestorePointError = "";

    System.Threading.Tasks.Task<bool> CreateRestorePointAsync(string description)
    {
        return System.Threading.Tasks.Task.Run(() =>
        {
            _lastRestorePointError = "";
            try
            {
                if (!IsSystemRestoreSupported())
                {
                    _lastRestorePointError = "System Restore is not supported on this Windows installation.";
                    return false;
                }

                bool isFlashback = (description == "NoveN Tweaks Flashback");
                var cmd = "try { " +
                          "  Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\VSS' -Name 'Start' -Value 3 -Type DWord -Force -ErrorAction SilentlyContinue; " +
                          "  Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\swprv' -Name 'Start' -Value 3 -Type DWord -Force -ErrorAction SilentlyContinue; " +
                          "  Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\srservice' -Name 'Start' -Value 3 -Type DWord -Force -ErrorAction SilentlyContinue; " +
                          "  Start-Service -Name VSS -ErrorAction SilentlyContinue; " +
                          "  Start-Service -Name swprv -ErrorAction SilentlyContinue; " +
                          "  Start-Service -Name srservice -ErrorAction SilentlyContinue; " +
                          "  $polPath = 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore'; " +
                          "  if (Test-Path $polPath) { " +
                          "    Set-ItemProperty -Path $polPath -Name 'DisableSR' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue; " +
                          "    Set-ItemProperty -Path $polPath -Name 'DisableConfig' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue; " +
                          "  } " +
                          "  $rpKey = 'HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\SystemRestore'; " +
                          "  if (Test-Path $rpKey) { " +
                          "    Set-ItemProperty -Path $rpKey -Name 'DisableSR' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue; " +
                          "    Set-ItemProperty -Path $rpKey -Name 'DisableConfig' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue; " +
                          "    $origFreq = (Get-ItemProperty -Path $rpKey -Name 'SystemRestorePointCreationFrequency' -ErrorAction SilentlyContinue).SystemRestorePointCreationFrequency; " +
                          "    Set-ItemProperty -Path $rpKey -Name 'SystemRestorePointCreationFrequency' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue; " +
                          "  } " +
                          "  Enable-ComputerRestore -Drive ($env:SystemDrive + '\\') -ErrorAction Stop; " +
                          "  Checkpoint-Computer -Description '" + description + "' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop; " +
                          "  if (Test-Path $rpKey) { " +
                          "    if ($origFreq -ne $null) { " +
                          "      Set-ItemProperty -Path $rpKey -Name 'SystemRestorePointCreationFrequency' -Value $origFreq -Type DWord -Force -ErrorAction SilentlyContinue; " +
                          "    } else { " +
                          "      Remove-ItemProperty -Path $rpKey -Name 'SystemRestorePointCreationFrequency' -ErrorAction SilentlyContinue; " +
                          "    } " +
                          "  } " +
                          "  if (" + (isFlashback ? "$true" : "$false") + ") { " +
                          "    $rps = Get-ComputerRestorePoint | Where-Object { $_.Description -eq 'NoveN Tweaks Flashback' } | Sort-Object SequenceNumber; " +
                          "    if ($rps.Count -gt 5) { " +
                          "      $toDeleteCount = $rps.Count - 5; " +
                          "      for ($i = 0; $i -lt $toDeleteCount; $i++) { " +
                          "        $seq = $rps[$i].SequenceNumber; " +
                          "        ([wmiclass]'\\\\.\\root\\default:SystemRestore').SRRemoveRestorePoint($seq); " +
                          "      } " +
                          "    } " +
                          "  } " +
                          "  exit 0; " +
                          "} catch { " +
                          "  [Console]::Error.WriteLine($_.Exception.Message); " +
                          "  exit 1; " +
                          "}";

                var bytes = System.Text.Encoding.Unicode.GetBytes(cmd);
                var encoded = Convert.ToBase64String(bytes);
                var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                var proc = Process.Start(psi);
                if (proc != null)
                {
                    string errOut = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(errOut) && errOut.Contains("#< CLIXML"))
                        {
                            errOut = System.Text.RegularExpressions.Regex.Replace(errOut, @"<[^>]+>", " ");
                            errOut = System.Text.RegularExpressions.Regex.Replace(errOut, @"\s+", " ").Trim();
                        }
                        _lastRestorePointError = string.IsNullOrEmpty(errOut)
                            ? $"PowerShell exited with code {proc.ExitCode}"
                            : errOut.Trim();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _lastRestorePointError = ex.Message;
            }
            return false;
        });
    }

    async void DoCreateRestorePoint()
    {
        var s = I18n.Get(_lang);
        SetStatus(s.StatusApply);

        if (BtnCreateRestorePoint != null) BtnCreateRestorePoint.IsEnabled = false;
        if (BtnRestoreSystem != null) BtnRestoreSystem.IsEnabled = false;

        bool success = false;
        bool isLocalBackup = false;

        int mode = Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "FlashbackMode") ?? 0;
        string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        if (mode == 0 && IsSystemRestoreSupported())
        {
            success = await CreateRestorePointAsync($"NoveN Tweaks Flashback ({dateStr})");
        }

        if (!success)
        {
            // System Restore is either unsupported, failed, or user requested manual local backup mode
            isLocalBackup = true;
            success = await System.Threading.Tasks.Task.Run(() => CreateLocalRegistryBackup($"NoveN Tweaks Flashback ({dateStr}) - Manual"));
        }

        if (BtnCreateRestorePoint != null) BtnCreateRestorePoint.IsEnabled = true;
        if (BtnRestoreSystem != null) BtnRestoreSystem.IsEnabled = true;
        SetStatus(s.StatusReady);

        if (success)
        {
            PlaySoundEffect();
            if (isLocalBackup)
            {
                PopulateLocalBackupsList();
                MsgBox(s.FlashbackLocalBackupSuccessTitle, s.FlashbackLocalBackupSuccess, DialogType.Success);
            }
            else
            {
                MsgBox(s.FlashbackCreateSuccessTitle, s.FlashbackCreateSuccess, DialogType.Success);
            }
        }
        else
        {
            if (isLocalBackup)
            {
                MsgBox(s.ErrorTitle, "Failed to create local registry backup.", DialogType.Error);
            }
            else
            {
                string msg = s.FlashbackCreateFailed;
                if (!string.IsNullOrEmpty(_lastRestorePointError))
                {
                    msg += "\n\nDetails:\n" + _lastRestorePointError;
                }
                MsgBox(s.FlashbackCreateFailedTitle, msg, DialogType.Error);
            }
        }
    }

    void DoRestoreSystem()
    {
        var s = I18n.Get(_lang);
        if (IsSystemRestoreSupported())
        {
            try
            {
                Process.Start("rstrui.exe");
            }
            catch (Exception ex)
            {
                MsgBox(s.FlashbackRestoreFailedTitle, $"{s.FlashbackRestoreFailedTitle}: {ex.Message}", DialogType.Error);
            }
        }
        else
        {
            try
            {
                string backupsDir = GetBackupsDir();
                if (!System.IO.Directory.Exists(backupsDir) || System.IO.Directory.GetFiles(backupsDir, "*.nvnbak").Length == 0)
                {
                    MsgBox(s.FlashbackLocalRestoreSuccessTitle, s.FlashbackLocalRestoreNoBackups, DialogType.Warning);
                    return;
                }

                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    InitialDirectory = backupsDir,
                    Filter = "NoveN Backup files (*.nvnbak)|*.nvnbak",
                    Title = s.FlashbackLocalRestoreTitle
                };

                if (dlg.ShowDialog() == true)
                {
                    bool restored = RestoreLocalRegistryBackup(dlg.FileName);
                    if (restored)
                    {
                        PlaySoundEffect();
                        PopulateLocalBackupsList();
                        MsgBox(s.FlashbackLocalRestoreSuccessTitle, s.FlashbackLocalRestoreSuccess, DialogType.Success);
                    }
                    else
                    {
                        MsgBox(s.ErrorTitle, "Failed to restore the selected backup.", DialogType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MsgBox(s.ErrorTitle, $"Error during registry rollback: {ex.Message}", DialogType.Error);
            }
        }
    }

    void WireToggle(CheckBox cb, string name, int defaultVal = 0)
    {
        if (cb == null) return;
        cb.Checked += (_, _) =>
        {
            if (_isProgrammaticChange) return;
            SaveSetting(name, 1);
            PlaySoundEffect();
            if (name == "AnimationsEnabled") AreAnimationsEnabled = true;
        };
        cb.Unchecked += (_, _) =>
        {
            if (_isProgrammaticChange) return;
            SaveSetting(name, 0);
            PlaySoundEffect();
            if (name == "AnimationsEnabled") AreAnimationsEnabled = false;
        };
    }

    void LoadSettingsToggles()
    {
        _isProgrammaticChange = true;
        try
        {
            if (TglAutoStart != null) TglAutoStart.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AutoStart") ?? 0) == 1;
            if (TglAskClose != null) TglAskClose.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "ShowConfirm") ?? 1) == 1;
            if (TglRestorePoint != null) TglRestorePoint.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "CreateRestorePoint") ?? 1) == 1;
            if (TglAutoRestart != null) TglAutoRestart.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AutoRestart") ?? 0) == 1;
            if (TglSounds != null) TglSounds.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SoundsEnabled") ?? 0) == 1; // v2.8: OFF by default
            if (TglAnimations != null)
            {
                bool anims = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
                TglAnimations.IsChecked = anims;
                AreAnimationsEnabled = anims;
            }
            if (TglHwAccel != null) TglHwAccel.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "HwAccelEnabled") ?? 1) == 1;
            if (TglAutoUpdate != null) TglAutoUpdate.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AutoUpdateCheck") ?? 1) == 1;
            if (TglCompact != null) TglCompact.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "CompactMode") ?? 0) == 1;
            if (TglFlashbackMode != null) TglFlashbackMode.IsChecked = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "FlashbackMode") ?? 0) == 1;
        }
        finally
        {
            _isProgrammaticChange = false;
        }
        PopulateLocalBackupsList();
    }

    void SaveSetting(string name, int value) { try { Reg.SetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", name, value); } catch { } }

    void ApplyHwAccel(bool enabled)
    {
        try
        {
            System.Windows.Media.RenderOptions.ProcessRenderMode = enabled
                ? System.Windows.Interop.RenderMode.Default
                : System.Windows.Interop.RenderMode.SoftwareOnly;
        }
        catch { }
    }

    // ============================================================
    // SEARCH (fixed: animate Height, not just Visibility)
    // ============================================================
    void WireSearch()
    {
        if (BtnSearchToggle == null || SearchPanel == null) return;
        BtnSearchToggle.Click += (_, _) => ToggleSearch();

        if (SearchBox != null)
        {
            var debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            debounce.Tick += (_, _) => { debounce.Stop(); DoSearch(SearchBox.Text); };
            SearchBox.TextChanged += (_, _) => { debounce.Stop(); debounce.Start(); };
        }
        if (BtnSearchClear != null) BtnSearchClear.Click += (_, _) => { if (SearchBox != null) SearchBox.Text = ""; };

        // Ctrl+F and Escape
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; ToggleSearch(); }
            if (e.Key == Key.Escape && _searchOpen) { e.Handled = true; ToggleSearch(); }
        };
    }

    void ToggleSearch()
    {
        _searchOpen = !_searchOpen;
        bool anims = TglAnimations != null ? TglAnimations.IsChecked == true : (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "AnimationsEnabled") ?? 1) == 1;
        if (_searchOpen)
        {
            SearchPanel.Visibility = Visibility.Visible;
            if (!anims)
            {
                SearchPanel.BeginAnimation(HeightProperty, null);
                SearchPanel.Height = 300;
            }
            else
            {
                var anim = new DoubleAnimation(0, 300, TimeSpan.FromMilliseconds(200))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                SearchPanel.BeginAnimation(HeightProperty, anim);
            }
            SearchBox?.Focus();
        }
        else
        {
            if (!anims)
            {
                SearchPanel.BeginAnimation(HeightProperty, null);
                SearchPanel.Height = 0;
                SearchPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                var anim = new DoubleAnimation(SearchPanel.ActualHeight, 0, TimeSpan.FromMilliseconds(150))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
                anim.Completed += (_, _) => SearchPanel.Visibility = Visibility.Collapsed;
                SearchPanel.BeginAnimation(HeightProperty, anim);
            }
        }
    }

    void DoSearch(string q)
    {
        if (SearchResults == null) return;
        SearchResults.Items.Clear();
        q = q.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q)) return;

        int count = 0;
        foreach (var t in Tweaks.All)
        {
            string name = Tweaks.Name(t, (int)_lang).ToLowerInvariant();
            string desc = Tweaks.Desc(t, (int)_lang).ToLowerInvariant();
            if (name.Contains(q) || desc.Contains(q) || t.Id.ToLowerInvariant().Contains(q))
            {
                SearchResults.Items.Add(BuildSearchRow(t));
                count++;
                if (count >= 15) break; // Limit to top 15 results
            }
        }
    }

    Border BuildSearchRow(Tweak t)
    {
        var s = I18n.Get(_lang);
        bool active = false;
        lock (_tweakActiveState)
        {
            _tweakActiveState.TryGetValue(t.Id, out var val);
            active = val == true;
        }

        var row = new Border
        {
            Padding = new Thickness(12, 8, 12, 8),
            Cursor = Cursors.Hand,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 4)
        };

        if (active)
        {
            row.BorderBrush = BorderOn;
            row.Background = BgOn;
        }
        else
        {
            row.BorderBrush = BorderOff;
            row.Background = BgOff;
        }

        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock
        {
            FontFamily = Mdl2,
            FontSize = 11,
            Foreground = FgIcon,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 7, 0),
            Text = GetTweakIcon(t)
        });
        nameRow.Children.Add(new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = FgTitle,
            VerticalAlignment = VerticalAlignment.Center,
            Text = Tweaks.Name(t, (int)_lang)
        });
        sp.Children.Add(nameRow);

        var subRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        subRow.Children.Add(new TextBlock
        {
            Text = t.Category.ToUpper(),
            FontSize = 9,
            Foreground = FgDesc,
            VerticalAlignment = VerticalAlignment.Center
        });
        subRow.Children.Add(new TextBlock
        {
            Text = "  •  " + Tweaks.Desc(t, (int)_lang),
            FontSize = 9,
            Foreground = FgDesc,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 360
        });
        sp.Children.Add(subRow);

        Grid.SetColumn(sp, 0);
        g.Children.Add(sp);

        var btn = new Button
        {
            Style = (Style)Resources["BtnSec"],
            VerticalAlignment = VerticalAlignment.Center,
            Height = 24,
            Padding = new Thickness(10, 4, 10, 4),
            Cursor = Cursors.Hand
        };
        SetActionButtonContent(btn, active, s);
        btn.Click += (_, _) =>
        {
            RunSingleTweak(t, !active);
        };
        Grid.SetColumn(btn, 1);
        g.Children.Add(btn);

        row.MouseEnter += (_, _) => { if (row.Background != BgOn) row.Background = BgHover; };
        row.MouseLeave += (_, _) => { if (row.Background == BgHover) row.Background = BgOff; };
        row.Child = g;
        return row;
    }

    // ============================================================
    // ============================================================
    // INSTALLER PANEL  (rebuilt: categories, search, select-all, per-app status, winget check)
    // ============================================================

    // Category key constants
    static readonly string[] InstallerCatOrder = new[]
    {
        "Browsers", "Communication", "Media", "Games", "Archivers", "DevTools",
        "Utilities", "HardwareInfo", "CloudSync", "Remote", "Virtualization",
        "Security", "Notes", "Torrents", "Other"
    };

    static string InstallerCatLabel(string key, I18n.Strings s) => key switch
    {
        "Browsers"        => s.InstallerCatBrowsers,
        "Communication"   => s.InstallerCatCommunication,
        "Media"           => s.InstallerCatMedia,
        "Games"           => s.InstallerCatGames,
        "Archivers"       => s.InstallerCatArchivers,
        "DevTools"        => s.InstallerCatDevTools,
        "Utilities"       => s.InstallerCatUtilities,
        "HardwareInfo"    => s.InstallerCatHardwareInfo,
        "CloudSync"       => s.InstallerCatCloudSync,
        "Remote"          => s.InstallerCatRemote,
        "Virtualization"  => s.InstallerCatVirtualization,
        "Security"        => s.InstallerCatSecurity,
        "Notes"           => s.InstallerCatNotes,
        "Torrents"        => s.InstallerCatTorrents,
        _                 => s.InstallerCatOther,
    };

    void WireInstaller()
    {
        BuildInstallerPanel();

        if (BtnInstall != null) BtnInstall.Click += (_, _) => InstallSelectedApps(install: true);
        if (BtnDownload != null) BtnDownload.Click += (_, _) => InstallSelectedApps(install: false);

        // Select-all / Deselect-all footer buttons act on the installer when on the Installer tab
        if (BtnSelectAll != null) BtnSelectAll.Click += (_, _) => { if (_activeCategory == "Installer") InstallerSelectAll(true); };
        if (BtnDeselectAll != null) BtnDeselectAll.Click += (_, _) => { if (_activeCategory == "Installer") InstallerSelectAll(false); };
    }

    static string GetInstallerIcon(string id) => id switch
    {
        "Brave.Brave" or "Mozilla.Firefox" or "Google.Chrome" or "Opera.Opera" or "Opera.OperaGX" => "\xE774",
        "Discord.Discord" or "WhatsApp.WhatsApp" or "Telegram.TelegramDesktop" or
        "OpenWhisperSystems.Signal" or "Mozilla.Thunderbird" or "ProtonMail.ProtonMailBridge" => "\xE8BD",
        "VideoLAN.VLC" or "Spotify.Spotify" or "clsid2.mpc-hc" or "foobar2000.foobar2000" or
        "Winamp.Winamp" or "PexipAS.Plex" => "\xE7FC",
        "Valve.Steam" or "EpicGames.EpicGamesLauncher" => "\xE7FC",
        "7zip.7zip" or "RARLab.WinRAR" => "\xE7B8",
        "Notepad++.Notepad++" or "Microsoft.VisualStudioCode" or "Anysphere.Cursor" or
        "Codeium.Windsurf" or "JetBrains.Toolbox" => "\xE8A5",
        "Microsoft.PowerToys" => "\xE9D9",
        "Git.Git" or "GitHub.GitHubDesktop" => "\xE8FD",
        "Python.Python.3.11" or "OpenJS.NodeJS" => "\xE943",
        "Docker.DockerDesktop" => "\xEBC5",
        "GIMP.GIMP" or "KDE.Krita" or "BlenderFoundation.Blender" or "Inkscape.Inkscape" or
        "Blackmagic.DaVinciResolve" => "\xE8B9",
        "OBSProject.OBSStudio" or "HandBrake.HandBrake" => "\xE7FC",
        "Audacity.Audacity" => "\xE767",
        "Figma.Figma" => "\xE7F4",
        "PuTTY.PuTTY" or "WinSCP.WinSCP" or "Mobatek.MobaXterm" => "\xE8FD",
        "MSI.Afterburner" or "CPUID.HWMonitor" or "Sysinternals.ProcessHacker" => "\xE9D9",
        "DominikReichl.KeePass" or "ProtonTechnologies.ProtonVPN" or
        "NordSecurity.NordVPN" or "OpenVPNTechnologies.OpenVPN" => "\xE8B3",
        "AutoHotkey.AutoHotkey" or "Genymobile.scrcpy" => "\xE713",
        "Zen-Team.Zen-Browser" or "LibreWolf.LibreWolf" or "Waterfox.Waterfox" => "\xE774",
        "Element.Element" or "Viber.Viber" or "Mattermost.Mattermost" => "\xE8BD",
        "Kodi.Kodi" or "XnSoft.XnView.Classic" or "stax76.mpv.net" => "\xE7FC",
        "Playnite.Playnite" or "Itch.Itch" => "\xE7FC",
        "M2Team.NanaZip" or "peazip.PeaZip" => "\xE7B8",
        "Microsoft.WindowsTerminal" or "GoLang.Go" or "dbeaver.dbeaver" or "Insomnia.Insomnia" or "GitHub.cli" => "\xE8A5",
        "dotPDN.PaintDotNet" or "Greenshot.Greenshot" or "Flameshot.Flameshot" => "\xE8B9",
        "TranslucentTB.TranslucentTB" or "File-New-Project.EarTrumpet" or "Ditto.Ditto" or "Duplicati.Duplicati" or "Ventoy.Ventoy" => "\xE9D9",
        "FinalWire.AIDA64.Extreme" or "Piriform.Speccy" => "\xE9D9",
        "Google.GoogleDrive" or "MEGA.MEGADesktop" => "\xEBC5",
        "RustDesk.RustDesk" or "Parsec.Parsec" => "\xE8FD",
        "Sandboxie.Plus" => "\xEBC5",
        "Malwarebytes.Malwarebytes" or "Windscribe.Windscribe" => "\xE8B3",
        "Joplin.Joplin" or "Logseq.Logseq" => "\xE8A5",
        "Deluge.Deluge" => "\xE713",
        _ => "\xE713"
    };

    static string InstallerCatOf(string id) => id switch
    {
        "Brave.Brave" or "Mozilla.Firefox" or "Google.Chrome" or "Opera.Opera" or "Opera.OperaGX" =>
            "Browsers",
        "Discord.Discord" or "WhatsApp.WhatsApp" or "Telegram.TelegramDesktop" or "OpenWhisperSystems.Signal" or
        "Microsoft.Skype" or "Microsoft.Teams" or "SlackTechnologies.Slack" or "Zoom.Zoom" or
        "Mozilla.Thunderbird" or "ProtonMail.ProtonMailBridge" =>
            "Communication",
        "VideoLAN.VLC" or "Spotify.Spotify" or "OBSProject.OBSStudio" or "Audacity.Audacity" or
        "Apple.iTunes" or "Twitch.Twitch" or "IrfanSkiljan.IrfanView" or "clsid2.mpc-hc" or
        "PexipAS.Plex" or "foobar2000.foobar2000" or "Winamp.Winamp" or "Blackmagic.DaVinciResolve" or
        "HandBrake.HandBrake" =>
            "Media",
        "Valve.Steam" or "EpicGames.EpicGamesLauncher" or "ElectronicArts.EADesktop" or
        "Ubisoft.Connect" or "GOG.Galaxy" or "RiotGames.RiotClient" =>
            "Games",
        "7zip.7zip" or "RARLab.WinRAR" =>
            "Archivers",
        "Notepad++.Notepad++" or "Microsoft.VisualStudioCode" or "Git.Git" or "GitHub.GitHubDesktop" or
        "Python.Python.3.11" or "OpenJS.NodeJS" or "Postman.Postman" or "Anysphere.Cursor" or
        "Codeium.Windsurf" or "JetBrains.Toolbox" or "PuTTY.PuTTY" or "WinSCP.WinSCP" or
        "Mobatek.MobaXterm" or "AutoHotkey.AutoHotkey" =>
            "DevTools",
        "Microsoft.PowerToys" or "Flow-Launcher.Flow-Launcher" or "Everything.Everything" or
        "ShareX.ShareX" or "WinDirStat.WinDirStat" or "Rufo.Rufus" or "Balena.Etcher" or
        "FileZilla.FileZilla" or "GIMP.GIMP" or "KDE.Krita" or "BlenderFoundation.Blender" or
        "Figma.Figma" or "Inkscape.Inkscape" or "Genymobile.scrcpy" =>
            "Utilities",
        "AntibodySoftware.WizTree" or "CrystalDewWorld.CrystalDiskInfo" or "CPUID.CPU-Z" or
        "TechPowerUp.GPU-Z" or "HWiNFO.HWiNFO" or "Piriform.CCleaner" or
        "MSI.Afterburner" or "CPUID.HWMonitor" or "Sysinternals.ProcessHacker" =>
            "HardwareInfo",
        "Microsoft.OneDrive" or "Dropbox.Dropbox" =>
            "CloudSync",
        "TeamViewer.TeamViewer" or "AnyDeskSoftwareGmbH.AnyDesk" =>
            "Remote",
        "Oracle.VirtualBox" or "VMware.WorkstationPlayer" or "Docker.DockerDesktop" =>
            "Virtualization",
        "Bitwarden.Bitwarden" or "AgileBits.1Password" or "DominikReichl.KeePass" or
        "ProtonTechnologies.ProtonVPN" or "NordSecurity.NordVPN" or "OpenVPNTechnologies.OpenVPN" =>
            "Security",
        "Notion.Notion" or "Obsidian.Obsidian" or "Amazon.Kindle" =>
            "Notes",
        "qBittorrent.qBittorrent" or "Transmission.Transmission" or "Deluge.Deluge" =>
            "Torrents",
        "Zen-Team.Zen-Browser" or "LibreWolf.LibreWolf" or "Waterfox.Waterfox" =>
            "Browsers",
        "Element.Element" or "Viber.Viber" or "Mattermost.Mattermost" =>
            "Communication",
        "Kodi.Kodi" or "XnSoft.XnView.Classic" or "stax76.mpv.net" =>
            "Media",
        "Playnite.Playnite" or "Itch.Itch" =>
            "Games",
        "M2Team.NanaZip" or "peazip.PeaZip" =>
            "Archivers",
        "Microsoft.WindowsTerminal" or "GoLang.Go" or "dbeaver.dbeaver" or "Insomnia.Insomnia" or "GitHub.cli" =>
            "DevTools",
        "dotPDN.PaintDotNet" or "Greenshot.Greenshot" or "Flameshot.Flameshot" or
        "TranslucentTB.TranslucentTB" or "File-New-Project.EarTrumpet" or "Ditto.Ditto" or "Duplicati.Duplicati" or "Ventoy.Ventoy" =>
            "Utilities",
        "FinalWire.AIDA64.Extreme" or "Piriform.Speccy" =>
            "HardwareInfo",
        "Google.GoogleDrive" or "MEGA.MEGADesktop" =>
            "CloudSync",
        "RustDesk.RustDesk" or "Parsec.Parsec" =>
            "Remote",
        "Sandboxie.Plus" =>
            "Virtualization",
        "Malwarebytes.Malwarebytes" or "Windscribe.Windscribe" =>
            "Security",
        "Joplin.Joplin" or "Logseq.Logseq" =>
            "Notes",
        _ =>
            "Other"
    };

    // Installer app definitions (matches .ps1 InstallerDefs)
    // v2.5: Removed "CTT" (Chris Titus Tech Tool) at user request.
    static readonly (string id, string name, string desc)[] InstallerApps = new[]
    {
        ("Brave.Brave", "Brave Browser", "Privacy-focused web browser."),
        ("Mozilla.Firefox", "Mozilla Firefox", "Free and open-source web browser."),
        ("Google.Chrome", "Google Chrome", "Fast, secure, and free web browser."),
        ("Discord.Discord", "Discord", "Chat for communities and friends."),
        ("WhatsApp.WhatsApp", "WhatsApp", "Simple. Reliable. Secure."),
        ("Telegram.TelegramDesktop", "Telegram", "Fast and secure messaging app."),
        ("OpenWhisperSystems.Signal", "Signal", "Privacy-focused messaging app."),
        ("VideoLAN.VLC", "VLC Media Player", "Free and open source cross-platform multimedia player."),
        ("Spotify.Spotify", "Spotify", "Digital music, podcast, and video service."),
        ("Valve.Steam", "Steam", "Video game digital distribution service by Valve."),
        ("EpicGames.EpicGamesLauncher", "Epic Games Launcher", "Storefront and game launcher by Epic Games."),
        ("7zip.7zip", "7-Zip", "A free and open-source file archiver."),
        ("RARLab.WinRAR", "WinRAR", "A powerful archive manager."),
        ("Notepad++.Notepad++", "Notepad++", "Free source code editor and Notepad replacement."),
        ("Microsoft.PowerToys", "PowerToys", "Set of utilities for power users to tune and streamline their Windows experience."),
        ("Microsoft.VisualStudioCode", "Visual Studio Code", "Code editor redefined and optimized for building and debugging modern web and cloud applications."),
        ("Git.Git", "Git", "Free and open source distributed version control system."),
        ("GitHub.GitHubDesktop", "GitHub Desktop", "Focus on what matters instead of fighting with Git."),
        ("Python.Python.3.11", "Python 3.11", "Programming language that lets you work quickly and integrate systems more effectively."),
        ("OpenJS.NodeJS", "Node.js", "JavaScript runtime built on Chrome's V8 JavaScript engine."),
        ("Docker.DockerDesktop", "Docker Desktop", "Collaborative and secure platform for developers to build, share, and run applications."),
        ("GIMP.GIMP", "GIMP", "GNU Image Manipulation Program."),
        ("KDE.Krita", "Krita", "Professional FREE and open source painting program."),
        ("BlenderFoundation.Blender", "Blender", "Free and open source 3D creation suite."),
        ("OBSProject.OBSStudio", "OBS Studio", "Free and open source software for video recording and live streaming."),
        ("Audacity.Audacity", "Audacity", "Free, open source, cross-platform audio software."),
        ("Figma.Figma", "Figma", "Collaborative interface design tool."),
        ("AntibodySoftware.WizTree", "WizTree", "The fastest disk space analyzer."),
        ("CrystalDewWorld.CrystalDiskInfo", "CrystalDiskInfo", "HDD/SSD health monitoring utility."),
        ("CPUID.CPU-Z", "CPU-Z", "Freeware that gathers information on some of the main devices of your system."),
        ("TechPowerUp.GPU-Z", "GPU-Z", "A lightweight system utility designed to provide vital information about your video card and graphics processor."),
        ("HWiNFO.HWiNFO", "HWiNFO", "Comprehensive hardware analysis, monitoring and reporting for Windows and DOS."),
        ("Piriform.CCleaner", "CCleaner", "Utility used to clean potentially unwanted files and invalid Windows Registry entries from a computer."),
        ("Microsoft.OneDrive", "OneDrive", "Save your files and photos to OneDrive and access them from any device, anywhere."),
        ("Dropbox.Dropbox", "Dropbox", "Bring your photos, docs, and videos anywhere and keep your files safe."),
        ("Zoom.Zoom", "Zoom", "Video conferencing, web conferencing, webinars, screen sharing."),
        ("Microsoft.Teams", "Microsoft Teams", "Meet, chat, call, and collaborate in just one place."),
        ("SlackTechnologies.Slack", "Slack", "Slack brings the team together, wherever you are."),
        ("TeamViewer.TeamViewer", "TeamViewer", "Remote control, desktop sharing, online meetings, web conferencing and file transfer between computers."),
        ("AnyDeskSoftwareGmbH.AnyDesk", "AnyDesk", "The fast remote desktop application."),
        ("Oracle.VirtualBox", "VirtualBox", "A powerful x86 and AMD64/Intel64 virtualization product for enterprise as well as home use."),
        ("VMware.WorkstationPlayer", "VMware Workstation Player", "A streamlined desktop virtualization application."),
        ("Postman.Postman", "Postman", "An API platform for building and using APIs."),
        ("Bitwarden.Bitwarden", "Bitwarden", "The easiest and safest way for individuals, teams, and business organizations to store, share, and sync sensitive data."),
        ("AgileBits.1Password", "1Password", "A password manager, digital vault, form filler and secure digital wallet."),
        ("WinDirStat.WinDirStat", "WinDirStat", "A disk usage statistics viewer and cleanup tool for various versions of Microsoft Windows."),
        ("Rufo.Rufus", "Rufus", "Create bootable USB drives the easy way."),
        ("Balena.Etcher", "balenaEtcher", "Flash OS images to SD cards & USB drives, safely and easily."),
        ("qBittorrent.qBittorrent", "qBittorrent", "A free and reliable P2P BitTorrent client."),
        ("Transmission.Transmission", "Transmission", "A fast, easy, and free BitTorrent client."),
        ("Flow-Launcher.Flow-Launcher", "Flow Launcher", "Quick file search & app launcher for Windows with community-made plugins."),
        ("Everything.Everything", "Everything", "Locate files and folders by name instantly."),
        ("ShareX.ShareX", "ShareX", "Screen capture, file sharing and productivity tool."),
        ("Notion.Notion", "Notion", "The all-in-one workspace for your notes, tasks, wikis, and databases."),
        ("Obsidian.Obsidian", "Obsidian", "A powerful knowledge base that works on top of a local folder of plain text Markdown files."),
        ("Microsoft.Skype", "Skype", "Communication tool for free calls and chat."),
        ("ElectronicArts.EADesktop", "EA app", "The all-new PC platform for EA games."),
        ("Ubisoft.Connect", "Ubisoft Connect", "The ecosystem of players services for Ubisoft games across all platforms."),
        ("GOG.Galaxy", "GOG GALAXY", "GOG GALAXY 2.0 brings all your games and friends to one place."),
        ("RiotGames.RiotClient", "Riot Client", "A launcher for Riot Games' PC titles."),
        ("IrfanSkiljan.IrfanView", "IrfanView", "One of the most popular viewers worldwide."),
        ("Amazon.Kindle", "Kindle", "Read eBooks on your PC."),
        ("Twitch.Twitch", "Twitch", "Watch livestreaming gaming videos, Esports and any IRL broadcast on your Windows device."),
        ("FileZilla.FileZilla", "FileZilla", "Fast and reliable cross-platform FTP, FTPS and SFTP client."),
        ("Apple.iTunes", "iTunes", "Everything you need to be entertained."),
        // ── v2.9: New apps ──
        ("Opera.Opera", "Opera Browser", "Fast, safe and smart browser."),
        ("Opera.OperaGX", "Opera GX", "The gaming browser — Twitch, Discord & CPU/RAM limiter built-in."),
        ("Anysphere.Cursor", "Cursor", "The AI-first code editor. Built on VSCode."),
        ("Codeium.Windsurf", "Windsurf", "AI-powered IDE by Codeium."),
        ("JetBrains.Toolbox", "JetBrains Toolbox", "Manage all your JetBrains IDEs in one place."),
        ("AutoHotkey.AutoHotkey", "AutoHotkey", "The ultimate automation scripting language for Windows."),
        ("PuTTY.PuTTY", "PuTTY", "Free SSH and Telnet client."),
        ("WinSCP.WinSCP", "WinSCP", "Free SFTP, SCP, S3 and FTP client for Windows."),
        ("Mobatek.MobaXterm", "MobaXterm", "Enhanced terminal for Windows with SSH, X11 and more."),
        ("Genymobile.scrcpy", "scrcpy", "Display and control your Android device from your PC."),
        ("Inkscape.Inkscape", "Inkscape", "Professional vector graphics editor. Free and open source."),
        ("Blackmagic.DaVinciResolve", "DaVinci Resolve", "Professional video editing, color, VFX and audio."),
        ("HandBrake.HandBrake", "HandBrake", "Free and open source video transcoder."),
        ("clsid2.mpc-hc", "MPC-HC", "Lightweight video player for Windows."),
        ("foobar2000.foobar2000", "foobar2000", "Advanced freeware audio player."),
        ("PexipAS.Plex", "Plex", "Organize and stream all your media."),
        ("MSI.Afterburner", "MSI Afterburner", "GPU overclocking and monitoring utility."),
        ("CPUID.HWMonitor", "HWMonitor", "Hardware monitoring program for CPU, GPU, and disk temperatures."),
        ("Sysinternals.ProcessHacker", "Process Hacker", "Powerful multi-purpose tool for process, service and network monitoring."),
        ("DominikReichl.KeePass", "KeePass", "Free, open source, light-weight and easy-to-use password manager."),
        ("ProtonTechnologies.ProtonVPN", "ProtonVPN", "High-speed Swiss VPN that safeguards your privacy."),
        ("NordSecurity.NordVPN", "NordVPN", "Fast, secure, and unlimited VPN."),
        ("OpenVPNTechnologies.OpenVPN", "OpenVPN", "Open source VPN protocol and GUI client."),
        ("Mozilla.Thunderbird", "Thunderbird", "Free email application. Easy setup."),
        ("Winamp.Winamp", "Winamp", "Classic Windows media player reborn."),
        // v3.0: Additional popular apps
        ("Zen-Team.Zen-Browser", "Zen Browser", "Privacy-focused Firefox-based browser."),
        ("LibreWolf.LibreWolf", "LibreWolf", "A custom Firefox fork focused on privacy."),
        ("Element.Element", "Element", "Secure, decentralized messaging via Matrix."),
        ("Viber.Viber", "Viber", "Free calls and messages to anyone, anywhere."),
        ("Kodi.Kodi", "Kodi", "Free and open source home theater software."),
        ("XnSoft.XnView.Classic", "XnView Classic", "Graphic viewer, browser and converter."),
        ("stax76.mpv.net", "mpv.net", "Powerful media player based on mpv."),
        ("Playnite.Playnite", "Playnite", "Open source video game library manager."),
        ("Itch.Itch", "itch.io", "The indie game store desktop app."),
        ("M2Team.NanaZip", "NanaZip", "Modern file archiver for Windows 11."),
        ("peazip.PeaZip", "PeaZip", "Free file archiver - supports RAR, TAR, ZIP."),
        ("Microsoft.WindowsTerminal", "Windows Terminal", "Modern terminal with tabs and themes."),
        ("GoLang.Go", "Go", "Open source programming language by Google."),
        ("dbeaver.dbeaver", "DBeaver", "Universal database tool and SQL client."),
        ("Insomnia.Insomnia", "Insomnia", "API design platform and REST client."),
        ("GitHub.cli", "GitHub CLI", "GitHub on the command line."),
        ("dotPDN.PaintDotNet", "paint.net", "Free image and photo editing software."),
        ("Greenshot.Greenshot", "Greenshot", "A light-weight screenshot tool with annotation."),
        ("Flameshot.Flameshot", "Flameshot", "Powerful yet simple screenshot software."),
        ("TranslucentTB.TranslucentTB", "TranslucentTB", "Makes Windows taskbar translucent."),
        ("File-New-Project.EarTrumpet", "EarTrumpet", "Better volume control for Windows."),
        ("Ditto.Ditto", "Ditto", "An extension to the standard Windows clipboard."),
        ("Duplicati.Duplicati", "Duplicati", "Free backup software with cloud storage."),
        ("Ventoy.Ventoy", "Ventoy", "Create bootable USB drives with multiple ISOs."),
        ("FinalWire.AIDA64.Extreme", "AIDA64 Extreme", "System info, diagnostics and benchmarking."),
        ("Piriform.Speccy", "Speccy", "Fast, lightweight advanced system information."),
        ("Google.GoogleDrive", "Google Drive", "Easy and secure access to all your files."),
        ("MEGA.MEGADesktop", "MEGA", "Secure cloud storage and communication."),
        ("RustDesk.RustDesk", "RustDesk", "Open source remote desktop software."),
        ("Parsec.Parsec", "Parsec", "Remote desktop you will actually love."),
        ("Sandboxie.Plus", "Sandboxie Plus", "Sandbox-based isolation software for Windows."),
        ("Malwarebytes.Malwarebytes", "Malwarebytes", "Advanced anti-malware and anti-spyware."),
        ("Windscribe.Windscribe", "Windscribe", "Free and pro VPN with ad blocker."),
        ("Joplin.Joplin", "Joplin", "Open source note-taking and to-do application."),
        ("Logseq.Logseq", "Logseq", "Privacy-first, open-source knowledge base."),
        ("Deluge.Deluge", "Deluge", "Lightweight, open source BitTorrent client.")    };


    readonly Dictionary<string, CheckBox> _installerCheckboxes = new();
    readonly Dictionary<string, TextBlock> _installerStatusTbs = new();
    readonly Dictionary<string, Border> _installerCards = new();
    readonly Dictionary<string, string> _installerCats = new();
    StackPanel _installerRootStack;

    private static string _resolvedWingetPath = null;

    private static string GetWingetPath()
    {
        if (_resolvedWingetPath != null) return _resolvedWingetPath;

        // 1. Try standard winget.exe (system PATH)
        if (IsWingetRunnable("winget.exe"))
        {
            _resolvedWingetPath = "winget.exe";
            return _resolvedWingetPath;
        }

        // 2. Try the local appdata copy
        string localDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NoveNTweaks", "winget");
        string localPath = System.IO.Path.Combine(localDir, "winget.exe");
        if (IsWingetRunnable(localPath))
        {
            _resolvedWingetPath = localPath;
            return _resolvedWingetPath;
        }

        // 3. Try to copy it from WindowsApps to local appdata
        try
        {
            string winAppsDir = System.IO.Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "WindowsApps");
            if (System.IO.Directory.Exists(winAppsDir))
            {
                var match = System.IO.Directory.GetDirectories(winAppsDir, "Microsoft.DesktopAppInstaller_*_x64__8wekyb3d8bbwe")
                                     .OrderByDescending(d => d)
                                     .FirstOrDefault();
                if (match != null)
                {
                    if (!System.IO.Directory.Exists(localDir)) System.IO.Directory.CreateDirectory(localDir);
                    foreach (var file in System.IO.Directory.GetFiles(match, "*.*"))
                    {
                        try
                        {
                            string dest = System.IO.Path.Combine(localDir, System.IO.Path.GetFileName(file));
                            System.IO.File.Copy(file, dest, true);
                        }
                        catch { }
                    }
                    if (IsWingetRunnable(localPath))
                    {
                        _resolvedWingetPath = localPath;
                        return _resolvedWingetPath;
                    }
                }
            }
        }
        catch { }

        // Fallback to "winget.exe" (so it fails normally if all else fails)
        return "winget.exe";
    }

    private static bool IsWingetRunnable(string path)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(path, "--version")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            if (p == null) return false;
            p.WaitForExit(3000);
            return p.ExitCode == 0 || p.ExitCode == 1;
        }
        catch { return false; }
    }

    // True if winget.exe is available on PATH.
    static bool WingetAvailable()
    {
        return IsWingetRunnable(GetWingetPath());
    }

    void BuildInstallerPanel()
    {
        var panel = FindName("PanelInstaller") as ScrollViewer;
        if (panel == null) return;
        var s = I18n.Get(_lang);

        // Clear stale state (e.g. when rebuilding after a language switch)
        _installerCheckboxes.Clear();
        _installerStatusTbs.Clear();
        _installerCards.Clear();
        _installerCats.Clear();

        _installerRootStack = new StackPanel { Margin = new Thickness(4, 2, 4, 2) };

        // ---- Category sections ----
        foreach (var catKey in InstallerCatOrder)
        {
            var appsInCat = System.Array.FindAll(InstallerApps, a => InstallerCatOf(a.id) == catKey);
            if (appsInCat.Length == 0) continue;

            var sectionWrap = new StackPanel { Margin = new Thickness(0, 4, 0, 8) };
            // Category header
            var headerGrid = new Grid { Margin = new Thickness(2, 0, 2, 6) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var hdr = new TextBlock
            {
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = FgTitle,
                Text = InstallerCatLabel(catKey, s).ToUpperInvariant(),
            };
            Grid.SetColumn(hdr, 0); headerGrid.Children.Add(hdr);
            var selectCat = new TextBlock
            {
                FontSize = 10,
                Foreground = (Brush)Application.Current.Resources["AccentFg"],
                Text = s.InstallerSelectAllCat,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
            };
            selectCat.MouseLeftButtonUp += (_, _) => ToggleInstallerCategory(catKey, true);
            Grid.SetColumn(selectCat, 1); headerGrid.Children.Add(selectCat);
            sectionWrap.Children.Add(headerGrid);

            var grid = new UniformGrid { Columns = 2 };
            grid.BeginInit();
            foreach (var app in appsInCat)
                grid.Children.Add(BuildInstallerCard(app));
            grid.EndInit();
            sectionWrap.Children.Add(grid);
            _installerRootStack.Children.Add(sectionWrap);
        }

        panel.Content = _installerRootStack;
    }

    // Try to load a PNG icon from embedded resources for installer cards.
    // Returns null if the resource does not exist or cannot be decoded.
    static System.Windows.Media.Imaging.BitmapImage TryLoadAppIcon(string appId)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/Assets/AppIcons/{appId}.png");
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.DecodePixelHeight = 24;
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    Border BuildInstallerCard((string id, string name, string desc) app)
    {
        var card = new Border
        {
            Style = (Style)Resources["CardStyle"],
            Margin = new Thickness(4),
            Background = BgOff,
            BorderBrush = BorderOff,
        };
        var inner = new Grid();
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 0) };
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };

        // Try to use a real PNG logo; fall back to the MDL2 glyph icon
        var appIcon = TryLoadAppIcon(app.id);
        if (appIcon != null)
        {
            var img = new Image
            {
                Source = appIcon,
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 1, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true,
            };
            RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.HighQuality);
            nameRow.Children.Add(img);
        }
        else
        {
            nameRow.Children.Add(new TextBlock { FontFamily = Mdl2, FontSize = 12, Foreground = FgIcon, Text = GetInstallerIcon(app.id), Margin = new Thickness(0, 1, 7, 0), VerticalAlignment = VerticalAlignment.Center });
        }

        nameRow.Children.Add(new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = FgTitle, Text = app.name, VerticalAlignment = VerticalAlignment.Center });
        // per-app status badge (hidden until install runs)
        var statusTb = new TextBlock { FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = (Brush)Application.Current.Resources["AccentFg"], Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };
        nameRow.Children.Add(statusTb);
        left.Children.Add(nameRow);
        // Description indent: 20px when icon present, 19px when using glyph (consistent alignment)
        left.Children.Add(new TextBlock { FontSize = 10, Foreground = FgDesc, TextWrapping = TextWrapping.Wrap, Text = app.desc, Margin = new Thickness(28, 0, 0, 0) });
        Grid.SetColumn(left, 0);
        inner.Children.Add(left);
        var cb = new CheckBox { Style = (Style)Resources["ToggleStyle"], VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(cb, 1);
        inner.Children.Add(cb);
        card.MouseEnter += (_, _) => { if (cb.IsChecked != true) { card.Background = BgHover; card.BorderBrush = BgHoverBorder; } };
        card.MouseLeave += (_, _) => { if (cb.IsChecked != true) { card.Background = BgOff; card.BorderBrush = BorderOff; } };
        cb.Checked += (_, _) => { card.BorderBrush = BorderOn; card.Background = BgOn; };
        cb.Unchecked += (_, _) => { card.BorderBrush = BorderOff; card.Background = BgOff; };
        // Click anywhere on the card toggles the checkbox — but skip when the click lands on the
        // checkbox itself (it toggles on its own) to avoid a double-toggle.
        card.MouseLeftButtonUp += (sender, e) =>
        {
            if (e.OriginalSource == cb) return;
            if (e.OriginalSource is DependencyObject d && cb.IsAncestorOf(d)) return;
            cb.IsChecked = !(cb.IsChecked == true);
        };
        card.Child = inner;
        _installerCheckboxes[app.id] = cb;
        _installerStatusTbs[app.id] = statusTb;
        _installerCards[app.id] = card;
        _installerCats[app.id] = InstallerCatOf(app.id);
        return card;
    }

    void ToggleInstallerCategory(string catKey, bool on)
    {
        // v2.5: Fixed — previously skipped cards that were Collapsed (which broke
        // when search filter was active). Now we toggle ALL cards in the category
        // regardless of visibility, since the user explicitly clicked "Select all".
        foreach (var (id, _) in _installerCats)
        {
            if (_installerCats[id] != catKey) continue;
            if (_installerCheckboxes.TryGetValue(id, out var cb)) cb.IsChecked = on;
        }
    }

    void InstallerSelectAll(bool on)
    {
        foreach (var (id, cb) in _installerCheckboxes)
        {
            if (_installerCards.TryGetValue(id, out var card) && card.Visibility != Visibility.Visible) continue;
            cb.IsChecked = on;
        }
    }

    void ApplyInstallerFilter()
    {
        // Search box removed - all apps shown by default
        foreach (var (id, card) in _installerCards)
            card.Visibility = Visibility.Visible;
    }

    void SetInstallerAppStatus(string id, string text, bool visible)
    {
        if (!_installerStatusTbs.TryGetValue(id, out var tb)) return;
        Dispatcher.Invoke(() =>
        {
            tb.Text = text ?? "";
            tb.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    void InstallSelectedApps(bool install)
    {
        var s = I18n.Get(_lang);
        var selected = InstallerApps.Where(a => _installerCheckboxes.TryGetValue(a.id, out var cb) && cb.IsChecked == true).ToList();
        if (selected.Count == 0)
        {
            ShowCustomDialog(s.NavInstaller, s.InstallerNoSelection, DialogType.Info, false);
            return;
        }

        // Pre-flight: verify winget is present
        if (!WingetAvailable())
        {
            ShowCustomDialog(s.InstallerWingetMissingTitle, s.InstallerWingetMissing, DialogType.Warning, false);
            return;
        }

        var action = install ? s.InstallerReadyInstall : s.InstallerReadyDownload;
        var appList = string.Join("\n", selected.Select(a => a.name));
        if (!ShowCustomDialog(s.ConfirmCaption, string.Format(s.InstallerConfirm, action, selected.Count, appList),
            DialogType.Question, true)) return;

        if (BtnInstall != null) BtnInstall.IsEnabled = false;
        if (BtnDownload != null) BtnDownload.IsEnabled = false;

        // Mark all selected as queued
        foreach (var app in selected) SetInstallerAppStatus(app.id, s.InstallerAppQueued, true);

        // v2.5: Show top progress bar during installation
        int totalApps = selected.Count;
        int completedApps = 0;
        var actionVerb = install ? s.StatusInstallApp : s.StatusDownloadApp;
        ShowTopProgress(install ? s.StatusApply : s.StatusApply, 0);

        System.Threading.Tasks.Task.Run(() =>
        {
            int ok = 0, fail = 0;
            var failedApps = new List<string>();
            foreach (var app in selected)
            {
                Dispatcher.Invoke(() =>
                {
                    SetInstallerAppStatus(app.id, s.InstallerAppInstalling, true);
                    string statusText = string.Format(actionVerb, app.name);
                    SetStatus(statusText);
                    UpdateTopProgress(statusText, totalApps > 0 ? completedApps * 100 / totalApps : 0);
                });

                try
                {
                    // v2.5: Improved winget args — added -h (silent), --silent, removed --disable-interactivity
                    // which was causing failures for packages that need user context.
                    // Also added longer timeout (Run.Hidden already has 30s but winget install may take longer).
                    // Use --accept-source-agreements + --accept-package-agreements + -h (silent) + --silent
                    // Retry once if first attempt fails.
                    var args = install
                        ? $"install --id {app.id} -e -h --silent --accept-source-agreements --accept-package-agreements"
                        : $"download --id {app.id} -e --accept-source-agreements --accept-package-agreements";
                    var result = Run.HiddenLong($"\"{GetWingetPath()}\" {args}", 600000); // 10 min timeout

                    // If first attempt failed, retry without --silent (some packages don't support it)
                    if (result != 0 && install)
                    {
                        System.Threading.Thread.Sleep(2000);
                        var argsRetry = $"install --id {app.id} -e -h --accept-source-agreements --accept-package-agreements";
                        result = Run.HiddenLong($"\"{GetWingetPath()}\" {argsRetry}", 600000);
                    }

                    // winget exit codes: 0 = success, -1978335189 (0x8A150011) = already installed
                    if (result == 0 || result == -1978335189)
                    {
                        ok++;
                        SetInstallerAppStatus(app.id, s.InstallerAppDone, true);
                    }
                    else
                    {
                        fail++;
                        failedApps.Add(app.name);
                        SetInstallerAppStatus(app.id, s.InstallerAppFailed, true);
                    }
                }
                catch
                {
                    fail++;
                    failedApps.Add(app.name);
                    SetInstallerAppStatus(app.id, s.InstallerAppFailed, true);
                }
                completedApps++;
                Dispatcher.Invoke(() =>
                {
                    UpdateTopProgress(totalApps > 0 ? completedApps * 100 / totalApps : 100);
                });
            }

            Dispatcher.Invoke(() =>
            {
                HideTopProgress();
                if (BtnInstall != null) BtnInstall.IsEnabled = true;
                if (BtnDownload != null) BtnDownload.IsEnabled = true;
                SetStatus(s.StatusDone);

                // v2.5: Custom dialog with details — Success type if all OK, Error if any failed.
                string summary = string.Format(s.InstallerDone, ok, fail);
                if (fail > 0 && failedApps.Count > 0)
                {
                    summary += "\n\n" + (s.InstallerFailedList ?? "Failed apps:") + "\n  • " + string.Join("\n  • ", failedApps);
                    if (failedApps.Count > 10)
                        summary = summary.Substring(0, summary.Length - (failedApps.Count - 10) * 30) + "\n  ...";
                    ShowCustomDialog(s.NavInstaller, summary, DialogType.Error, false);
                }
                else
                {
                    ShowCustomDialog(s.NavInstaller, summary, DialogType.Success, false);
                }
            });
        });
    }

    // ============================================================
    // DYNAMIC SETTINGS & SYSTEM TRAY INTEGRATION
    // ============================================================
    private System.Windows.Forms.NotifyIcon _notifyIcon;

    [DllImport("psapi.dll")]
    static extern bool EmptyWorkingSet(IntPtr hProcess);

    private void InitNotifyIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        try
        {
            var streamInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
            if (streamInfo != null)
            {
                using var stream = streamInfo.Stream;
                _notifyIcon.Icon = new System.Drawing.Icon(stream);
            }
        }
        catch 
        {
            try
            {
                if (System.IO.File.Exists("Assets/app.ico"))
                {
                    _notifyIcon.Icon = new System.Drawing.Icon("Assets/app.ico");
                }
            }
            catch { }
        }

        _notifyIcon.Text = "NoveN Tweaks";
        _notifyIcon.DoubleClick += (s, e) => ShowAndRestoreWindow();

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Renderer = new System.Windows.Forms.ToolStripProfessionalRenderer(new DarkContextMenuColorTable());
        contextMenu.BackColor = System.Drawing.Color.FromArgb(30, 30, 36);
        contextMenu.ForeColor = System.Drawing.Color.White;
        contextMenu.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Regular);
        
        var openItem = new System.Windows.Forms.ToolStripMenuItem(_lang == I18n.Language.PL ? "Otwórz program" : "Open Program");
        openItem.ForeColor = System.Drawing.Color.White;
        openItem.Click += (s, e) => ShowAndRestoreWindow();

        var cleanRamItem = new System.Windows.Forms.ToolStripMenuItem(_lang == I18n.Language.PL ? "Wyczyść RAM" : "Clean RAM");
        cleanRamItem.ForeColor = System.Drawing.Color.White;
        cleanRamItem.Click += (s, e) =>
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                EmptyWorkingSet(Process.GetCurrentProcess().Handle);

                string title = _lang == I18n.Language.PL ? "Optymalizacja pamięci" : "Memory Optimization";
                string msg = _lang == I18n.Language.PL 
                    ? "Pamięć RAM została pomyślnie zoptymalizowana!" 
                    : "RAM memory optimized successfully!";
                _notifyIcon.ShowBalloonTip(3000, title, msg, System.Windows.Forms.ToolTipIcon.Info);
            }
            catch { }
        };

        var cleanTempItem = new System.Windows.Forms.ToolStripMenuItem(_lang == I18n.Language.PL ? "Wyczyść pliki tymczasowe" : "Clean Temp Files");
        cleanTempItem.ForeColor = System.Drawing.Color.White;
        cleanTempItem.Click += async (s, e) =>
        {
            try
            {
                int deletedFiles = 0;
                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var tempDir = System.IO.Path.GetTempPath();
                        if (System.IO.Directory.Exists(tempDir))
                        {
                            foreach (var file in System.IO.Directory.GetFiles(tempDir))
                            {
                                try { System.IO.File.Delete(file); deletedFiles++; } catch { }
                            }
                            foreach (var dir in System.IO.Directory.GetDirectories(tempDir))
                            {
                                try { System.IO.Directory.Delete(dir, true); } catch { }
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        var windirTemp = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
                        if (System.IO.Directory.Exists(windirTemp))
                        {
                            foreach (var file in System.IO.Directory.GetFiles(windirTemp))
                            {
                                try { System.IO.File.Delete(file); deletedFiles++; } catch { }
                            }
                            foreach (var dir in System.IO.Directory.GetDirectories(windirTemp))
                            {
                                try { System.IO.Directory.Delete(dir, true); } catch { }
                            }
                        }
                    }
                    catch { }
                });

                string title = _lang == I18n.Language.PL ? "Czyszczenie plików tymczasowych" : "Temp Files Cleanup";
                string msg = _lang == I18n.Language.PL 
                    ? $"Pomyślnie usunięto {deletedFiles} plików tymczasowych!" 
                    : $"Successfully deleted {deletedFiles} temporary files!";
                _notifyIcon.ShowBalloonTip(3000, title, msg, System.Windows.Forms.ToolTipIcon.Info);
            }
            catch { }
        };

        var resetNetItem = new System.Windows.Forms.ToolStripMenuItem(_lang == I18n.Language.PL ? "Zresetuj połączenie sieciowe" : "Reset Network Connection");
        resetNetItem.ForeColor = System.Drawing.Color.White;
        resetNetItem.Click += async (s, e) =>
        {
            try
            {
                string title = _lang == I18n.Language.PL ? "Resetowanie sieci" : "Network Reset";
                string startMsg = _lang == I18n.Language.PL 
                    ? "Rozpoczęto resetowanie karty sieciowej i pamięci DNS..." 
                    : "Resetting network adapter and DNS cache...";
                _notifyIcon.ShowBalloonTip(3000, title, startMsg, System.Windows.Forms.ToolTipIcon.Info);

                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c ipconfig /flushdns & ipconfig /release & ipconfig /renew",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                        };
                        using var proc = System.Diagnostics.Process.Start(psi);
                        proc?.WaitForExit(10000); // wait up to 10 seconds
                    }
                    catch { }
                });

                string endMsg = _lang == I18n.Language.PL 
                    ? "Karta sieciowa i pamięć DNS zostały pomyślnie zresetowane!" 
                    : "Network adapter and DNS cache reset successfully!";
                _notifyIcon.ShowBalloonTip(3000, title, endMsg, System.Windows.Forms.ToolTipIcon.Info);
            }
            catch { }
        };

        var exitItem = new System.Windows.Forms.ToolStripMenuItem(_lang == I18n.Language.PL ? "Zakończ" : "Exit");
        exitItem.ForeColor = System.Drawing.Color.White;
        exitItem.Click += (s, e) =>
        {
            _forceExit = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Current.Shutdown();
        };

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add(cleanRamItem);
        contextMenu.Items.Add(cleanTempItem);
        contextMenu.Items.Add(resetNetItem);
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    public class DarkContextMenuColorTable : System.Windows.Forms.ProfessionalColorTable
    {
        public override System.Drawing.Color ToolStripDropDownBackground => System.Drawing.Color.FromArgb(30, 30, 36);
        public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.FromArgb(30, 30, 36);
        public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.FromArgb(30, 30, 36);
        public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.FromArgb(30, 30, 36);
        public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(60, 60, 70);
        public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(30, 30, 36);
        public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(50, 50, 60);
        public override System.Drawing.Color MenuItemSelectedGradientBegin => System.Drawing.Color.FromArgb(50, 50, 60);
        public override System.Drawing.Color MenuItemSelectedGradientEnd => System.Drawing.Color.FromArgb(50, 50, 60);
        public override System.Drawing.Color MenuItemPressedGradientBegin => System.Drawing.Color.FromArgb(40, 40, 48);
        public override System.Drawing.Color MenuItemPressedGradientEnd => System.Drawing.Color.FromArgb(40, 40, 48);
        public override System.Drawing.Color SeparatorDark => System.Drawing.Color.FromArgb(60, 60, 70);
        public override System.Drawing.Color SeparatorLight => System.Drawing.Color.FromArgb(30, 30, 36);
    }

    private void ShowAndRestoreWindow()
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
        this.ShowInTaskbar = true;
    }

    // ── Click sound (v2.7 — completely rebuilt) ──
    // PREVIOUS APPROACH (winmm PlaySound + SND_MEMORY + SND_ASYNC) was unreliable:
    //   - The byte array had to stay pinned for the entire async playback.
    //   - On many systems winmm's PlaySound silently fails for in-memory WAVs
    //     when called rapidly (clicks get dropped), and there's no error feedback.
    //   - SND_PURGE/SND_ASYNC interplay caused the sound to be cut off or not
    //     play at all on some audio stacks.
    //
    // NEW APPROACH: Use System.Media.SoundPlayer (a thin managed wrapper over
    // the waveOut API). It is purpose-built for playing WAV streams from memory,
    // handles its own buffering, and is thread-safe when called with PlaySync()
    // on a background thread. We keep a single reusable SoundPlayer instance
    // pointed at a non-GC'd MemoryStream so rapid clicks don't reallocate.
    private static System.Media.SoundPlayer _clickPlayer;
    private static System.IO.MemoryStream _clickStream;
    private static readonly object _soundLock = new object();

    private static void InitClickSound()
    {
        try
        {
            int sampleRate = 44100;
            int ms = 35;
            int numSamples = (sampleRate * ms) / 1000;
            short[] samples = new short[numSamples];
            double frequency = 880.0; // A5 — bright click
            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double envelope = Math.Exp(-180.0 * t);
                samples[i] = (short)(Math.Sin(2.0 * Math.PI * frequency * t) * envelope * 12000.0);
            }
            byte[] wavBytes;
            using (var msStream = new System.IO.MemoryStream())
            {
                using (var writer = new System.IO.BinaryWriter(msStream, System.Text.Encoding.UTF8, true))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + numSamples * 2);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                    writer.Write(16);
                    writer.Write((short)1);  // PCM
                    writer.Write((short)1);  // Mono
                    writer.Write(sampleRate);
                    writer.Write(sampleRate * 2); // byte rate
                    writer.Write((short)2);  // block align
                    writer.Write((short)16); // bits per sample
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    writer.Write(numSamples * 2);
                    for (int i = 0; i < numSamples; i++) writer.Write(samples[i]);
                }
                wavBytes = msStream.ToArray();
            }
            // Keep a persistent MemoryStream + SoundPlayer so rapid clicks reuse
            // the same player (SoundPlayer.Play() is non-blocking and re-entrant).
            _clickStream?.Dispose();
            _clickStream = new System.IO.MemoryStream(wavBytes, writable: false);
            _clickPlayer?.Dispose();
            _clickPlayer = new System.Media.SoundPlayer(_clickStream);
        }
        catch (Exception ex) { Debug.WriteLine($"InitClickSound failed: {ex.Message}"); }
    }

    private static void PlaySoundEffect()
    {
        // v2.7: Removed the _isProgrammaticChange guard here — it was suppressing
        // sounds during legitimate user interactions that happened to be inside a
        // programmatic-change block. Each call site already gates PlaySoundEffect
        // appropriately; the global guard caused more harm than good for sounds.
        try
        {
            bool sounds = (Reg.GetDWord("HKCU:\\SOFTWARE\\NoveNTweaks", "SoundsEnabled") ?? 0) == 1; // v2.8: OFF by default
            if (!sounds) return;
            lock (_soundLock)
            {
                if (_clickPlayer == null) InitClickSound();
                if (_clickPlayer != null)
                {
                    // Play() is async (non-blocking) and re-entrant: it restarts
                    // the WAV from the beginning on each call, which is exactly
                    // the "click" behavior we want for rapid UI interactions.
                    _clickPlayer.Play();
                }
            }
        }
        catch (Exception ex) { Debug.WriteLine($"PlaySoundEffect failed: {ex.Message}"); }
    }

    private void ApplyAutoStartSetting(bool enable)
    {
        try
        {
            string keyPath = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Run";
            if (enable)
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                Reg.SetString(keyPath, "NoveNTweaks", $"\"{exePath}\" --minimized");
            }
            else
            {
                Reg.RemoveValue(keyPath, "NoveNTweaks");
            }
        }
        catch { }
    }



    private void RebuildActivePanel()
    {
        if (string.IsNullOrEmpty(_activeCategory)) return;
        if (_activeCategory != "Home" && _activeCategory != "Installer" && _activeCategory != "Settings" && _activeCategory != "SpeedTest")
        {
            BuildPanel(_activeCategory);
            _builtPanels.Add(_activeCategory);
        }
    }

    // ============================================================
    // CUSTOM BORDERLESS WINDOW RESIZING
    // ============================================================
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            var source = System.Windows.Interop.HwndSource.FromHwnd(helper.Handle);
            source.AddHook(WndProc);
        }
        catch { }
    }

    private const int WM_NCHITTEST = 0x0084;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            try
            {
                int x = (int)(lParam.ToInt32() & 0xFFFF);
                int y = (int)((lParam.ToInt32() >> 16) & 0xFFFF);
                
                var gridPoint = this.PointFromScreen(new Point(x, y));
                
                double width = this.ActualWidth;
                double height = this.ActualHeight;
                double borderSize = 7; // resize border thickness

                bool isLeft = gridPoint.X <= borderSize;
                bool isRight = gridPoint.X >= width - borderSize;
                bool isTop = gridPoint.Y <= borderSize;
                bool isBottom = gridPoint.Y >= height - borderSize;

                if (isTop && isLeft) { handled = true; return (IntPtr)HTTOPLEFT; }
                if (isTop && isRight) { handled = true; return (IntPtr)HTTOPRIGHT; }
                if (isBottom && isLeft) { handled = true; return (IntPtr)HTBOTTOMLEFT; }
                if (isBottom && isRight) { handled = true; return (IntPtr)HTBOTTOMRIGHT; }
                if (isLeft) { handled = true; return (IntPtr)HTLEFT; }
                if (isRight) { handled = true; return (IntPtr)HTRIGHT; }
                if (isTop) { handled = true; return (IntPtr)HTTOP; }
                if (isBottom) { handled = true; return (IntPtr)HTBOTTOM; }
            }
            catch { }
        }
        return IntPtr.Zero;
    }
}

// DXGI GPU Detection helpers
public struct GpuInfo
{
    public string Name;
    public string Vram;
    public uint LuidLow;
    public int LuidHigh;
}

[StructLayout(LayoutKind.Sequential)]
public struct LUID
{
    public uint LowPart;
    public int HighPart;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct DXGI_ADAPTER_DESC
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Description;
    public uint VendorId;
    public uint DeviceId;
    public uint SubSysId;
    public uint Revision;
    public UIntPtr DedicatedVideoMemory;
    public UIntPtr DedicatedSystemMemory;
    public UIntPtr SharedSystemMemory;
    public LUID AdapterLuid;
}

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public delegate int EnumAdaptersDelegate(IntPtr thisPtr, uint adapterIndex, out IntPtr ppAdapter);

[UnmanagedFunctionPointer(CallingConvention.StdCall)]
public delegate int GetDescDelegate(IntPtr thisPtr, out DXGI_ADAPTER_DESC pDesc);


// Converter: empty string -> Visible, non-empty -> Collapsed. Used by installer search placeholder.
public sealed class EmptyToVisibleConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        bool empty = value == null || string.IsNullOrEmpty(value.ToString());
        return empty ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }
    public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => System.Windows.DependencyProperty.UnsetValue;
}
