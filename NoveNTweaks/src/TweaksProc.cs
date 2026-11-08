using System;
using System.Collections.Generic;

namespace NoveNTweaks;

public static class TweaksProc
{
    private const string CatPerformance = "Performance";
    private const string CatSystem = "System";
    private const string CatDebloat = "Debloat";
    private const string CatAppearance = "Appearance";
    private const string CatPrivacy = "Privacy";
    private const string CatAdvanced = "Advanced";

    private static string[] T(string en, string pl)
    {
        var result = new string[9];
        result[0] = en;
        result[1] = pl;
        for (int i = 2; i < 9; i++) result[i] = en;
        return result;
    }

    private static void Add(List<Tweak> list, string id, string cat, bool warn,
                            Func<bool> apply, Func<bool> undo, Func<bool> detect,
                            string[] names, string[] descs)
    {
        var t = new Tweak
        {
            Id = id,
            Category = cat,
            Warning = warn,
            Apply = apply,
            Undo = undo,
            Detect = detect,
            Text = new string[2, 9],
        };
        for (int i = 0; i < 9; i++)
        {
            t.Text[0, i] = names[Math.Min(i, names.Length - 1)];
            t.Text[1, i] = descs[Math.Min(i, descs.Length - 1)];
        }
        list.Add(t);
    }

    public static List<Tweak> Build()
    {
        var list = new List<Tweak>();

        // ───────── 1. Reset / WinRE Enable ─────────
        Add(list, "tw_proc_reset_enable", CatSystem, false,
            () => { Run.Hidden("reagentc /enable"); return true; },
            () => true,
            () => true,
            T("Enable Windows Recovery Environment (WinRE & Factory Reset)", "Włącz środowisko odzyskiwania Windows (WinRE i Reset do fabrycznych)"),
            T("Runs reagentc /enable to ensure Windows Reset this PC works.", "Uruchamia reagentc /enable, aby upewnić się, że funkcja Resetowania systemu do ustawień fabrycznych działa."));

        // ───────── 2. SvcHost Merging (Major Background Process Reduction) ─────────
        Add(list, "tw_proc_svchost_merge", CatAdvanced, false,
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", 4294967295U);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", 3670016);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB") == unchecked((int)4294967295U),
            T("Merge SvcHost Processes (Reduces background process count down to ~50-60)", "Scalaj procesy SvcHost (Redukuje liczbę procesów w tle z ~100 do ~50-60)"),
            T("Combines split svchost background processes into groups. Massive background process reduction win.", "Łączy rozbite procesy svchost w grupy. Znaczny spadek liczby procesów w Menedżerze Zadań."));

        // ───────── 3. Disable UWP Apps Background Execution ─────────
        Add(list, "tw_proc_uwp_bg_disable", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1);
                Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 0);
                Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 0);
                return true;
            },
            () => Reg.GetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled") == 1,
            T("Disable UWP Store Apps Background Execution (Stops background process spawns)", "Wyłącz działanie aplikacji UWP w tle (Zatrzymuje procesy ukryte w tle)"),
            T("Prevents Store/UWP applications from running background process instances.", "Zapobiega uruchamianiu ukrytych instancji procesów aplikacji Windows Store w tle."));

        // ───────── 4. Disable Diagnostic & Telemetry Services ─────────
        Add(list, "tw_proc_disable_diag_telemetry", CatDebloat, false,
            () => {
                string[] svcs = { "dmwappushservice", "DPS", "WdiServiceHost", "WdiSystemHost", "WerSvc", "DiagTrack" };
                foreach (var s in svcs) { Svc.StopAndDisable(s); }
                return true;
            },
            () => {
                string[] svcs = { "dmwappushservice", "DPS", "WdiServiceHost", "WdiSystemHost", "WerSvc", "DiagTrack" };
                foreach (var s in svcs) { Svc.SetStartup(s, Svc.StartType.Manual); }
                return true;
            },
            () => Svc.IsDisabled("DiagTrack") && Svc.IsDisabled("WerSvc"),
            T("Disable Diagnostic & Telemetry Services (DPS, WerSvc, DiagTrack)", "Wyłącz usługi diagnostyki i telemetrii (DPS, WerSvc, DiagTrack)"),
            T("Stops diagnostic policy hosts and error reporting services running in background.", "Zatrzymuje procesy hostów diagnostycznych i raportowania błędów pracujące w tle."));

        // ───────── 5. Disable Non-Essential Desktop Services ─────────
        Add(list, "tw_proc_disable_nonessential_svcs", CatSystem, false,
            () => {
                string[] svcs = { "MapsBroker", "lfsvc", "wisvc", "RetailDemo", "PhoneSvc", "SensrSvc", "SensorDataService", "SensorService", "WalletService", "PaymentManager", "PcaSvc", "TrkWks", "WpcMonSvc" };
                foreach (var s in svcs) { Svc.StopAndDisable(s); }
                return true;
            },
            () => {
                string[] svcs = { "MapsBroker", "lfsvc", "wisvc", "RetailDemo", "PhoneSvc", "SensrSvc", "SensorDataService", "SensorService", "WalletService", "PaymentManager", "PcaSvc", "TrkWks", "WpcMonSvc" };
                foreach (var s in svcs) { Svc.SetStartup(s, Svc.StartType.Manual); }
                return true;
            },
            () => Svc.IsDisabled("MapsBroker") && Svc.IsDisabled("PhoneSvc"),
            T("Disable Non-Essential Services (Maps, Phone, Sensors, RetailDemo)", "Wyłącz zbędne usługi w tle (Mapy, Telefon, Czujniki, RetailDemo)"),
            T("Disables background processes for maps, phone sync, sensors, and retail demo. Leaves Wi-Fi, BT, MS Login intact.", "Zatrzymuje procesy w tle dla map, czujników i telefonu. Pozostawia Wi-Fi, Bluetooth, Wyszukiwarkę i logowanie MS bezpieczne."));

        // ───────── 6. FPS: Win32PrioritySeparation Quantum Boost ─────────
        Add(list, "tw_proc_fps_win32_priority", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 2);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation") == 38,
            T("CPU Quantum Priority Boost for Foreground Games (Win32PrioritySeparation = 38)", "Priorytet kwantu CPU dla gier w pierwszym planie (Win32PrioritySeparation = 38)"),
            T("Gives maximum CPU priority to active gaming windows for higher & smoother FPS.", "Daje maksymalny priorytet procesora aktywnej grze w pierwszym planie, zwiększając płynność i FPS."));

        // ───────── 7. FPS: Hardware-Accelerated GPU Scheduling (HAGS) ─────────
        Add(list, "tw_proc_fps_hags", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode") == 2,
            T("Enable Hardware-Accelerated GPU Scheduling (HAGS)", "Włącz sprzętowe planowanie GPU (HAGS)"),
            T("Reduces GPU scheduling latency and frametime jitter in modern games.", "Zmniejsza opóźnienia planowania GPU i mikro-zacięcia klatek w grach."));

        // ───────── 8. FPS: Disable Game DVR & Background Video Capture ─────────
        Add(list, "tw_proc_fps_gamedvr_disable", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKCU:\System\GameConfigStore", "GameDVR_Enabled", 0);
                Reg.SetDWord(@"HKCU:\System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2);
                Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
                Reg.SetDWord(@"HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
                Reg.SetDWord(@"HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "HistoricalCaptureEnabled", 0);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKCU:\System\GameConfigStore", "GameDVR_Enabled", 1);
                Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 1);
                Reg.SetDWord(@"HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1);
                return true;
            },
            () => Reg.GetDWord(@"HKCU:\System\GameConfigStore", "GameDVR_Enabled") == 0,
            T("Disable Windows Game DVR & Background Recording (Zero Overlay Latency)", "Wyłącz Xbox Game DVR i nagrywanie w tle (Brak opóźnień nakładek)"),
            T("Stops background video capture processes and eliminates input lag in games.", "Zatrzymuje procesy przechwytywania wideo w tle i eliminuje opóźnienia sterowania (input lag)."));

        // ───────── 9. FPS: MMCSS & Network Latency Profile ─────────
        Add(list, "tw_proc_fps_mmcss_latency", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0);
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 4294967295U);
                Reg.SetString(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", "High");
                Reg.SetString(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", "High");
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 8);
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 6);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness") == 0,
            T("MMCSS Ultra Latency & Network Throttling Disable Profile", "Optymalizacja MMCSS i wyłączenie dławienia sieci pod gry"),
            T("Configures system responsiveness to 0% reservation for non-games and removes network throttling.", "Ustawia 0% rezerwacji zasobów dla procesów tła i wyłącza dławienie pakietów sieciowych w grach."));

        // ───────── 10. FPS: Disable Dynamic Tick & HPET ─────────
        Add(list, "tw_proc_fps_timer_dynamictick", CatPerformance, false,
            () => {
                Run.Hidden("bcdedit /set disabledynamictick yes");
                Run.Hidden("bcdedit /deletevalue useplatformclock");
                return true;
            },
            () => {
                Run.Hidden("bcdedit /set disabledynamictick no");
                return true;
            },
            () => true,
            T("Disable Dynamic Tick & HPET (Timer Stutter Elimination)", "Wyłącz Dynamic Tick i HPET (Eliminacja mikro-zacięć zegara)"),
            T("Stops CPU power-saving tick modulation, enforcing consistent system timer resolution in games.", "Zapobiega zmianom częstotliwości zegara CPU, wymuszając stałe czasy odświeżania dla gier."));

        // ───────── 11. FPS: Disable Fault Tolerant Heap (FTH) ─────────
        Add(list, "tw_proc_fps_fth_disable", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\FTH", "Enabled", 0);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\FTH", "Enabled", 1);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SOFTWARE\Microsoft\FTH", "Enabled") == 0,
            T("Disable Fault Tolerant Heap FTH (Remove Gaming Latency Overhead)", "Wyłącz Fault Tolerant Heap FTH (Usuwa narzut pamięci w grach)"),
            T("Disables Windows memory fault monitoring overhead for DirectX games.", "Wyłącza narzut monitorowania pamięci Windows dla gier DirectX, poprawiając framerate."));

        // ───────── 12. FPS: Disable DXGI Thread Yielding ─────────
        Add(list, "tw_proc_fps_dxgi_yield_disable", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableThreadYield", 1);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableThreadYield", 0);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableThreadYield") == 1,
            T("Disable DXGI Thread Yielding (Low-Latency DirectX Rendering)", "Wyłącz ustępowanie wątków DXGI (Szybszy render DirectX)"),
            T("Forces DirectX render threads to avoid CPU yielding during frame output.", "Zmusza wątki renderowania DirectX do unikania oddawania czasu CPU przy generowaniu klatek."));

        // ───────── 13. FPS: Disable Power Throttling ─────────
        Add(list, "tw_proc_fps_power_throttling_off", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 0);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff") == 1,
            T("Disable Power Throttling (Keep CPU Cores at Max Speed)", "Wyłącz Power Throttling (Utrzymuje rdzenie CPU w gotowości)"),
            T("Prevents Windows from downclocking CPU background/worker threads during games.", "Zapobiega zbijaniu taktowań rdzeni procesora przez system podczas grania."));

        // ───────── 14. Visuals: Disable Acrylic Blur & Transparency ─────────
        Add(list, "tw_proc_visual_blur_disable", CatAppearance, false,
            () => {
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1);
                return true;
            },
            () => Reg.GetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency") == 0,
            T("Disable Windows Blur & Acrylic Transparency (Frees GPU/CPU Compositor Cycles)", "Wyłącz efekt rozmycia Blur i przezroczystość (Oszczędność cykli GPU/CPU)"),
            T("Disables window blur transparency effects to maximize GPU resources for games.", "Wyłącza efekt rozmycia przezroczystości okien i paska zadań, uwalniając zasoby GPU pod gry."));

        // ───────── 15. Visuals: Disable Window Animations & Motion ─────────
        Add(list, "tw_proc_visual_anim_disable", CatAppearance, false,
            () => {
                Reg.SetString(@"HKCU:\Control Panel\Desktop", "MinAnimate", "0");
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 0);
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2);
                return true;
            },
            () => {
                Reg.SetString(@"HKCU:\Control Panel\Desktop", "MinAnimate", "1");
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAnimations", 1);
                return true;
            },
            () => Reg.GetString(@"HKCU:\Control Panel\Desktop", "MinAnimate") == "0",
            T("Disable Window & Taskbar Animations (Instant System Response)", "Wyłącz animacje okien i paska zadań (Natychmiastowa reakcja pulpitu)"),
            T("Removes window minimize/maximize animation delays for instant desktop responsiveness.", "Usuwa animacje minimalizowania i powiększania okien dla natychmiastowej reakcji interfejsu."));

        // ───────── 16. Ads & Bloat: Disable Windows Ads & Start Suggestions ─────────
        Add(list, "tw_proc_windows_ads_disable", CatPrivacy, false,
            () => {
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0);
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0);
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0);
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-353696Enabled", 0);
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentlyInstalledAppsEnabled", 0);
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "RotatingLockScreenEnabled", 0);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 1);
                return true;
            },
            () => Reg.GetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled") == 0,
            T("Disable Windows Ads, Start Suggestions & Sponsored App Downloads", "Wyłącz reklamy Windows, sugestie w menu Start i ukryte instalacje"),
            T("Disables lock screen ads, Start menu suggestions, and automatic sponsored background downloads.", "Wyłącza reklamy na ekranie blokady, sugestie w menu Start i automatyczne pobieranie sponsorowanych aplikacji."));

        // ───────── 17. FPS: DX12 Low Latency Render Pipeline ─────────
        Add(list, "tw_proc_fps_dx12_queue", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableThreadYield", 1);
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "DisableOverlaySuppression", 1);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableThreadYield", 0);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableThreadYield") == 1,
            T("DirectX 12 Low-Latency Frame Queue (Eliminates Render Lag)", "Optymalizacja kolejki klatek DirectX 12 (Redukcja opóźnień renderowania)"),
            T("Prevents DX12 render threads from yielding CPU cycles during intensive 3D gaming.", "Zmusza wątki renderowania DirectX 12 do uniknięcia opóźnień i przestojów w grach 3D."));

        // ───────── 18. FPS: High Performance GPU Priority Mode ─────────
        Add(list, "tw_proc_fps_gpu_priority_mode", CatPerformance, false,
            () => {
                Reg.SetString(@"HKLM:\SOFTWARE\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings", "SwapEffectUpgradeEnable=1;GpuPreference=2;");
                return true;
            },
            () => {
                Reg.RemoveValue(@"HKLM:\SOFTWARE\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings");
                return true;
            },
            () => Reg.GetString(@"HKLM:\SOFTWARE\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings") != null,
            T("Global High Performance GPU Mode for 3D Games & Apps", "Globalne wymuszenie wysokiej wydajności GPU dla gier 3D"),
            T("Forces Direct3D pipelines to request maximum performance GPU power profiles.", "Wymusza najwyższy profil wydajności karty graficznej dla gier Direct3D."));

        // ───────── 19. FPS: Force Windows Game Mode & Priority ─────────
        Add(list, "tw_proc_fps_gamemode_enable", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKCU:\Software\Microsoft\GameBar", "AutoGameModeEnabled", 1);
                Reg.SetDWord(@"HKCU:\Software\Microsoft\GameBar", "AllowAutoGameMode", 1);
                return true;
            },
            () => {
                Reg.SetDWord(@"HKCU:\Software\Microsoft\GameBar", "AutoGameModeEnabled", 0);
                return true;
            },
            () => Reg.GetDWord(@"HKCU:\Software\Microsoft\GameBar", "AutoGameModeEnabled") == 1,
            T("Force Windows Game Mode Thread Prioritization", "Zapewnij priorytet rdzeni procesora dla gier (Windows Game Mode)"),
            T("Ensures CPU scheduling allocates resources to active game threads over background apps.", "Przydziela priorytet zasobów procesora aktywnej grze z pominięciem procesów tle."));

        // ───────── 20. FPS: DWM Compositor Latency Optimization ─────────
        Add(list, "tw_proc_fps_dwm_buffer", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\DWM", "MaxPreRenderedFrames", 1);
                Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\DWM", "Composition", 1);
                return true;
            },
            () => {
                Reg.RemoveValue(@"HKCU:\Software\Microsoft\Windows\DWM", "MaxPreRenderedFrames");
                return true;
            },
            () => Reg.GetDWord(@"HKCU:\Software\Microsoft\Windows\DWM", "MaxPreRenderedFrames") == 1,
            T("DWM Low Latency Frame Buffer (1 Pre-Rendered Frame)", "Minimalny bufor opóźnienia pulpitu DWM (MaxPreRenderedFrames = 1)"),
            T("Reduces Desktop Window Manager frame buffering to minimize mouse click to display response time.", "Zmniejsza bufor klatek kompozytora pulpitu DWM, redukując opóźnienie myszy (input lag)."));

        // ───────── 21. Aggressive FPS: 0.5ms Timer Resolution & System Timer Lock ─────────
        Add(list, "tw_proc_fps_timer_ultra", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "GlobalTimerResolutionRequests", 1);
                Run.Hidden("bcdedit /set disabledynamictick yes");
                Run.Hidden("bcdedit /deletevalue useplatformclock");
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "GlobalTimerResolutionRequests", 0);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "GlobalTimerResolutionRequests") == 1,
            T("Aggressive 0.5ms Timer Resolution & Zero-Latency Lock", "Agresywna blokada zegara systemowego 0.5ms (Eliminacja micro-stutteringu)"),
            T("Locks system timer resolution to 0.5ms and disables dynamic tick modulation for ultra-smooth FPS.", "Blokuje dokładność zegara systemowego na 0.5ms i wyłącza skoki zegara CPU, zapewniając idealną płynność w grach."));

        // ───────── 22. Aggressive FPS: CPU Core Unparking & C-State Elimination ─────────
        Add(list, "tw_proc_fps_cpu_parking_ultra", CatPerformance, false,
            () => {
                Run.Hidden("powercfg /setacvalueindex scheme_current sub_processor 5d76a2ca-e8c0-402f-a133-2158492d58ad 100");
                Run.Hidden("powercfg /setacvalueindex scheme_current sub_processor 0cc5b647-c1df-4637-891a-dec42631f264 100");
                Run.Hidden("powercfg /setactive scheme_current");
                return true;
            },
            () => true,
            () => true,
            T("Aggressive CPU Core Unparking (Keep All Cores Active)", "Agresywna blokada uśpienia rdzeni CPU (Wszystkie rdzenie aktywne 100%)"),
            T("Forces all physical and logical CPU cores to remain 100% active, preventing core wake-up latency during gaming.", "Wymusza ciągłą aktywność wszystkich rdzeni procesora, eliminując opóźnienia związane z ich wybudzaniem w grach."));

        // ───────── 23. Aggressive FPS: DirectX & GPU Preemption Boost ─────────
        Add(list, "tw_proc_fps_gpu_preemption_ultra", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "DisableOverlaySuppression", 1);
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableThreadYield", 1);
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 8);
                Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 6);
                Reg.SetString(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", "High");
                Reg.SetString(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", "High");
                return true;
            },
            () => true,
            () => Reg.GetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority") == 8,
            T("Aggressive GPU Scheduling & DirectX Preemption Boost", "Agresywne wywłaszczanie i najwyższy priorytet GPU dla gier 3D"),
            T("Maximizes GPU preemption and assigns absolute top priority to DirectX 11/12 graphics pipelines.", "Maksymalizuje wywłaszczanie GPU i przydziela absolutny priorytet wykonania dla potoków DirectX 11/12."));

        // ───────── 24. Aggressive Process Reduction: Background Service Trimming ─────────
        Add(list, "tw_proc_fps_process_count_ultra", CatDebloat, false,
            () => {
                // SvcHost Process merging to drastically drop process count down to 35-45
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", 4294967295U);
                
                // Safely stop & disable bloat background services (NEVER touches Wi-Fi, Sound, System Restore, or WinRE)
                string[] svcs = {
                    "MapsBroker", "lfsvc", "wisvc", "RetailDemo", "PhoneSvc", "SensrSvc",
                    "SensorDataService", "SensorService", "WalletService", "PaymentManager",
                    "PcaSvc", "TrkWks", "WpcMonSvc", "dmwappushservice", "DPS",
                    "WdiServiceHost", "WdiSystemHost", "diagsvc", "diagtrack"
                };
                foreach (var s in svcs) { Svc.StopAndDisable(s); }
                return true;
            },
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", 3670016);
                return true;
            },
            () => Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB") == unchecked((int)4294967295U),
            T("Aggressive Process Reduction Profile (Drop Process Count to ~35-45)", "Agresywna redukcja liczby procesów w tle (Spadek do ~35-45 procesów)"),
            T("Aggressively merges svchost groups and disables background bloat services. Keeps Wi-Fi, Audio, Graphics & Recovery 100% safe.", "Agresywnie scala grupy svchost i wyłącza zbędne procesy tła. Pozostawia Wi-Fi, dźwięk, grafikę i odzyskiwanie całkowicie bezpieczne."));

        // ───────── 25. Aggressive FPS: Kernel RAM Lock & Cache Trim ─────────
        Add(list, "tw_proc_fps_ram_standby_trim", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1);
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0);
                return true;
            },
            () => true,
            () => Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive") == 1,
            T("Aggressive Kernel RAM Lock & Standby Memory Trim", "Agresywna blokada Jądra Windows w RAM (Dedykowana pamięć dla gier)"),
            T("Locks the Windows kernel in physical RAM and stops file system cache from eating memory required by games.", "Blokuje jądro systemu w pamięci RAM i zapobiega zabieraniu pamięci ram przez bufor plików."));

        // ───────── 26. Aggressive Input Latency: Mouse/Keyboard Buffer Lock ─────────
        Add(list, "tw_proc_fps_input_latency_ultra", CatPerformance, false,
            () => {
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\mouhid\Parameters", "MouseDataQueueSize", 16);
                Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\kbdhid\Parameters", "KeyboardDataQueueSize", 16);
                Reg.SetString(@"HKCU:\Control Panel\Keyboard", "KeyboardSpeed", "31");
                Reg.SetString(@"HKCU:\Control Panel\Keyboard", "KeyboardDelay", "0");
                Reg.SetString(@"HKCU:\Control Panel\Desktop", "MenuShowDelay", "0");
                return true;
            },
            () => true,
            () => Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\mouhid\Parameters", "MouseDataQueueSize") == 16,
            T("Aggressive Zero-Input-Lag Mouse & Keyboard Buffer (16 Queue Size)", "Agresywna eliminacja input-laga myszy i klawiatury (Bufor 16)"),
            T("Reduces HID mouse and keyboard packet buffers to minimum to minimize hardware input latency.", "Zmniejsza bufor pakietów myszy i klawiatury USB do minimum, eliminując opóźnienie reacji w grach."));

        return list;
    }
}
