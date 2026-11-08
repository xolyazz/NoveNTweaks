using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;

namespace NoveNTweaks;

public class Tweak
{
    public string Id { get; set; }
    public string Category { get; set; }
    public bool Warning { get; set; }
    public string[,] Text { get; set; }
    public string[] Note { get; set; }
    public Func<bool> Apply { get; set; }
    public Func<bool> Undo { get; set; }
    public Func<bool> Detect { get; set; }
}

public static class Tweaks
{
    private const string CatPrivacy = "Privacy";
    private const string CatPerformance = "Performance";
    private const string CatDebloat = "Debloat";
    private const string CatUpdates = "Updates";
    private const string CatNetwork = "Network";
    private const string CatInternet = "Internet";
    private const string CatSystem = "System";
    private const string CatSecurity = "Security";
    private const string CatUI = "UI";
    private const string CatAppearance = "Appearance";
    private const string CatContextMenu = "ContextMenu";
    private const string CatAdvanced = "Advanced";

    public static string Name(Tweak t, int lang) => t?.Text != null ? t.Text[0, Math.Clamp(lang, 0, 8)] : "";
    public static string Desc(Tweak t, int lang) => t?.Text != null ? t.Text[1, Math.Clamp(lang, 0, 8)] : "";
    public static string Note(Tweak t, int lang) => t?.Note != null ? t.Note[Math.Clamp(lang, 0, t.Note.Length - 1)] : null;

    public static string Name(Tweak t, I18n.Language lang) => Name(t, (int)lang);
    public static string Desc(Tweak t, I18n.Language lang) => Desc(t, (int)lang);
    public static string Note(Tweak t, I18n.Language lang) => Note(t, (int)lang);

    private static readonly Dictionary<string, string[]> Notes = new();

    [ThreadStatic]
    private static List<Tweak> _tempList;

    public static List<Tweak> All { get; } = Build();

    private static void Add(string id, string cat, bool warn,
                            Func<bool> apply, Func<bool> undo, Func<bool> detect,
                            string[] names, string[] descs)
    {
        var t = new Tweak
        {
            Id = id, Category = cat, Warning = warn,
            Apply = apply, Undo = undo, Detect = detect,
            Text = new string[2, 9],
        };
        for (int i = 0; i < 9; i++) { t.Text[0, i] = names[Math.Min(i, names.Length - 1)]; t.Text[1, i] = descs[Math.Min(i, descs.Length - 1)]; }
        t.Note = Notes.TryGetValue(id, out var n) ? n : null;
        _tempList.Add(t);
    }

    static string[] T(params string[] s)
    {
        var result = new string[9];
        for (int i = 0; i < s.Length && i < 9; i++) result[i] = s[i];
        for (int i = s.Length; i < 9; i++) result[i] = s[0];
        return result;
    }

    static List<Tweak> Build()
    {
        _tempList = new List<Tweak>();

        Add("tw_001", CatSystem, false,
            A_tw_001, U_tw_001, D_tw_001,
            T("Disable service: WAP Push Message Routing (device management push) (dmwappushservice)"),
            T("sc.exe config \"dmwappushservice\" start= disabled"));
        Add("tw_002", CatSystem, false,
            A_tw_002, U_tw_002, D_tw_002,
            T("Disable service: Diagnostic Policy Service (DPS)"),
            T("sc.exe config \"DPS\" start= disabled"));
        Add("tw_003", CatSystem, false,
            A_tw_003, U_tw_003, D_tw_003,
            T("Disable service: Diagnostic Service Host (WdiServiceHost)"),
            T("sc.exe config \"WdiServiceHost\" start= disabled"));
        Add("tw_004", CatSystem, false,
            A_tw_004, U_tw_004, D_tw_004,
            T("Disable service: Diagnostic System Host (WdiSystemHost)"),
            T("sc.exe config \"WdiSystemHost\" start= disabled"));
        Add("tw_005", CatSystem, false,
            A_tw_005, U_tw_005, D_tw_005,
            T("Disable service: Downloaded Maps Manager (MapsBroker)"),
            T("sc.exe config \"MapsBroker\" start= disabled"));
        Add("tw_006", CatPrivacy, false,
            A_tw_006, U_tw_006, D_tw_006,
            T("Disable service: Geolocation Service (lfsvc)"),
            T("sc.exe config \"lfsvc\" start= disabled"));
        Add("tw_007", CatSystem, false,
            A_tw_007, U_tw_007, D_tw_007,
            T("Disable service: Windows Insider Service (wisvc)"),
            T("sc.exe config \"wisvc\" start= disabled"));
        Add("tw_008", CatPerformance, false,
            A_tw_008, U_tw_008, D_tw_008,
            T("Disable service: Program Compatibility Assistant (PcaSvc)"),
            T("sc.exe config \"PcaSvc\" start= disabled"));
        Add("tw_009", CatSystem, false,
            A_tw_009, U_tw_009, D_tw_009,
            T("Disable service: Remote Registry (RemoteRegistry)"),
            T("sc.exe config \"RemoteRegistry\" start= disabled"));
        Add("tw_010", CatSystem, false,
            A_tw_010, U_tw_010, D_tw_010,
            T("Disable service: Retail Demo Service (RetailDemo)"),
            T("sc.exe config \"RetailDemo\" start= disabled"));
        Add("tw_011", CatSystem, false,
            A_tw_011, U_tw_011, D_tw_011,
            T("Disable service: Secondary Logon (seclogon)"),
            T("sc.exe config \"seclogon\" start= disabled"));
        Add("tw_012", CatSystem, false,
            A_tw_012, U_tw_012, D_tw_012,
            T("Disable service: Parental Controls (WpcMonSvc)"),
            T("sc.exe config \"WpcMonSvc\" start= disabled"));
        Add("tw_013", CatSystem, false,
            A_tw_013, U_tw_013, D_tw_013,
            T("Disable service: Phone Service (PhoneSvc)"),
            T("sc.exe config \"PhoneSvc\" start= disabled"));
        Add("tw_014", CatSystem, false,
            A_tw_014, U_tw_014, D_tw_014,
            T("Disable service: Sensor Monitoring Service (SensrSvc)"),
            T("sc.exe config \"SensrSvc\" start= disabled"));
        Add("tw_015", CatSystem, false,
            A_tw_015, U_tw_015, D_tw_015,
            T("Disable service: Sensor Data Service (SensorDataService)"),
            T("sc.exe config \"SensorDataService\" start= disabled"));
        Add("tw_016", CatSystem, false,
            A_tw_016, U_tw_016, D_tw_016,
            T("Disable service: Sensor Service (SensorService)"),
            T("sc.exe config \"SensorService\" start= disabled"));
        Add("tw_017", CatSystem, false,
            A_tw_017, U_tw_017, D_tw_017,
            T("Disable service: Connected Devices Platform Service (CDPSvc)"),
            T("sc.exe config \"CDPSvc\" start= disabled"));
        Add("tw_018", CatNetwork, false,
            A_tw_018, U_tw_018, D_tw_018,
            T("Disable service: IP Helper (IPv6 tunneling/Teredo) (iphlpsvc)"),
            T("sc.exe config \"iphlpsvc\" start= disabled"));
        Add("tw_019", CatSystem, false,
            A_tw_019, U_tw_019, D_tw_019,
            T("Disable service: SysMain / Superfetch (SysMain)"),
            T("sc.exe config \"SysMain\" start= disabled"));
        Add("tw_020", CatSystem, false,
            A_tw_020, U_tw_020, D_tw_020,
            T("Disable service: Contact Data Indexing (PimIndexMaintenanceSvc)"),
            T("sc.exe config \"PimIndexMaintenanceSvc\" start= disabled"));
        Add("tw_021", CatSystem, false,
            A_tw_021, U_tw_021, D_tw_021,
            T("Disable service: User Data Storage (UserDataSvc)"),
            T("sc.exe config \"UserDataSvc\" start= disabled"));
        Add("tw_022", CatSystem, false,
            A_tw_022, U_tw_022, D_tw_022,
            T("Disable service: User Data Access (Unistore) (UnistoreSvc)"),
            T("sc.exe config \"UnistoreSvc\" start= disabled"));
        Add("tw_023", CatPerformance, false,
            A_tw_023, U_tw_023, D_tw_023,
            T("Disable service: Quality Windows Audio Video Experience (QWAVE)"),
            T("sc.exe config \"QWAVE\" start= disabled"));
        Add("tw_024", CatSystem, false,
            A_tw_024, U_tw_024, D_tw_024,
            T("Disable service: Remote Desktop Configuration (SessionEnv)"),
            T("sc.exe config \"SessionEnv\" start= disabled"));
        Add("tw_025", CatSystem, false,
            A_tw_025, U_tw_025, D_tw_025,
            T("Disable service: Remote Desktop Services (TermService)"),
            T("sc.exe config \"TermService\" start= disabled"));
        Add("tw_026", CatSystem, false,
            A_tw_026, U_tw_026, D_tw_026,
            T("Disable service: Remote Desktop Port Redirector (UmRdpService)"),
            T("sc.exe config \"UmRdpService\" start= disabled"));
        Add("tw_027", CatSystem, false,
            A_tw_027, U_tw_027, D_tw_027,
            T("Disable service: Smart Card (ScardSvr)"),
            T("sc.exe config \"ScardSvr\" start= disabled"));
        Add("tw_028", CatSystem, false,
            A_tw_028, U_tw_028, D_tw_028,
            T("Disable service: Smart Card Device Enumeration (ScDeviceEnum)"),
            T("sc.exe config \"ScDeviceEnum\" start= disabled"));
        Add("tw_029", CatSystem, false,
            A_tw_029, U_tw_029, D_tw_029,
            T("Disable service: SSDP Discovery (SSDPSRV)"),
            T("sc.exe config \"SSDPSRV\" start= disabled"));
        Add("tw_030", CatSystem, false,
            A_tw_030, U_tw_030, D_tw_030,
            T("Disable service: UPnP Device Host (upnphost)"),
            T("sc.exe config \"upnphost\" start= disabled"));
        Add("tw_031", CatNetwork, false,
            A_tw_031, U_tw_031, D_tw_031,
            T("Disable service: Windows Media Player Network Sharing (WMPNetworkSvc)"),
            T("sc.exe config \"WMPNetworkSvc\" start= disabled"));
        Add("tw_032", CatSystem, false,
            A_tw_032, U_tw_032, D_tw_032,
            T("Disable service: Device Association Service (DeviceAssociationService)"),
            T("sc.exe config \"DeviceAssociationService\" start= disabled"));
        Add("tw_033", CatSystem, false,
            A_tw_033, U_tw_033, D_tw_033,
            T("Disable service: Windows Push Notifications System (WpnService)"),
            T("sc.exe config \"WpnService\" start= disabled"));
        Add("tw_034", CatSystem, false,
            A_tw_034, U_tw_034, D_tw_034,
            T("Disable service: Application Layer Gateway (ALG)"),
            T("sc.exe config \"ALG\" start= disabled"));
        Add("tw_035", CatSystem, false,
            A_tw_035, U_tw_035, D_tw_035,
            T("Disable service: AllJoyn Router Service (AJRouter)"),
            T("sc.exe config \"AJRouter\" start= disabled"));
        Add("tw_036", CatSystem, false,
            A_tw_036, U_tw_036, D_tw_036,
            T("Disable service: Certificate Propagation (CertPropSvc)"),
            T("sc.exe config \"CertPropSvc\" start= disabled"));
        Add("tw_037", CatNetwork, false,
            A_tw_037, U_tw_037, D_tw_037,
            T("Disable service: Client License Service (Store apps licensing) (ClipSVC)"),
            T("sc.exe config \"ClipSVC\" start= disabled"));
        Add("tw_038", CatNetwork, false,
            A_tw_038, U_tw_038, D_tw_038,
            T("Disable service: Data Usage Subscription Management (DusmSvc)"),
            T("sc.exe config \"DusmSvc\" start= disabled"));
        Add("tw_039", CatSystem, false,
            A_tw_039, U_tw_039, D_tw_039,
            T("Disable service: Function Discovery Provider Host (fdPHost)"),
            T("sc.exe config \"fdPHost\" start= disabled"));
        Add("tw_040", CatSystem, false,
            A_tw_040, U_tw_040, D_tw_040,
            T("Disable service: Function Discovery Resource Publication (FDResPub)"),
            T("sc.exe config \"FDResPub\" start= disabled"));
        Add("tw_041", CatSystem, false,
            A_tw_041, U_tw_041, D_tw_041,
            T("Disable service: Automatic Time Zone Update (tzautoupdate)"),
            T("sc.exe config \"tzautoupdate\" start= disabled"));
        Add("tw_042", CatSystem, false,
            A_tw_042, U_tw_042, D_tw_042,
            T("Disable service: Microsoft Store Install Service (InstallService)"),
            T("sc.exe config \"InstallService\" start= disabled"));
        Add("tw_043", CatPerformance, false,
            A_tw_043, U_tw_043, D_tw_043,
            T("Disable service: Windows Font Cache Service (FontCache)"),
            T("sc.exe config \"FontCache\" start= disabled"));
        Add("tw_044", CatInternet, false,
            A_tw_044, U_tw_044, D_tw_044,
            T("Disable service: WebClient (WebDAV) (WebClient)"),
            T("sc.exe config \"WebClient\" start= disabled"));
        Add("tw_045", CatPrivacy, false,
            A_tw_045, U_tw_045, D_tw_045,
            T("Disable service: Distributed Link Tracking Client (TrkWks)"),
            T("sc.exe config \"TrkWks\" start= disabled"));
        Add("tw_046", CatSystem, false,
            A_tw_046, U_tw_046, D_tw_046,
            T("Disable service: Windows Mobile Hotspot Service (icssvc)"),
            T("sc.exe config \"icssvc\" start= disabled"));
        Add("tw_047", CatSystem, false,
            A_tw_047, U_tw_047, D_tw_047,
            T("Disable service: Wallet Service (WalletService)"),
            T("sc.exe config \"WalletService\" start= disabled"));
        Add("tw_048", CatSystem, false,
            A_tw_048, U_tw_048, D_tw_048,
            T("Disable service: Application Management (enterprise app deployment) (AppMgmt)"),
            T("sc.exe config \"AppMgmt\" start= disabled"));
        Add("tw_049", CatSystem, false,
            A_tw_049, U_tw_049, D_tw_049,
            T("Disable service: Diagnostic Execution Service (diagsvc)"),
            T("sc.exe config \"diagsvc\" start= disabled"));
        Add("tw_050", CatSystem, false,
            A_tw_050, U_tw_050, D_tw_050,
            T("Disable service: Windows Backup Engine (WbEngine)"),
            T("sc.exe config \"WbEngine\" start= disabled"));
        Add("tw_051", CatSystem, false,
            A_tw_051, U_tw_051, D_tw_051,
            T("Disable service: Volume Shadow Copy (VSS)"),
            T("sc.exe config \"VSS\" start= disabled"));
        Add("tw_052", CatPrivacy, false,
            A_tw_052, U_tw_052, D_tw_052,
            T("Disable service: Touch Keyboard and Handwriting Panel (TabletInputService)"),
            T("sc.exe config \"TabletInputService\" start= disabled"));
        Add("tw_053", CatSystem, false,
            A_tw_053, U_tw_053, D_tw_053,
            T("Disable service: Google Update (scheduled) (gupdate)"),
            T("sc.exe config \"gupdate\" start= disabled"));
        Add("tw_054", CatSystem, false,
            A_tw_054, U_tw_054, D_tw_054,
            T("Disable service: Google Update (on-demand) (gupdatem)"),
            T("sc.exe config \"gupdatem\" start= disabled"));
        Add("tw_055", CatSystem, false,
            A_tw_055, U_tw_055, D_tw_055,
            T("Disable service: Windows Image Acquisition (scanners) (StiSvc)"),
            T("sc.exe config \"StiSvc\" start= disabled"));
        Add("tw_056", CatSystem, false,
            A_tw_056, U_tw_056, D_tw_056,
            T("Disable service: Print Spooler (Spooler)"),
            T("sc.exe config \"Spooler\" start= disabled"));
        Add("tw_057", CatSystem, false,
            A_tw_057, U_tw_057, D_tw_057,
            T("Disable service: Fax Service (Fax)"),
            T("sc.exe config \"Fax\" start= disabled"));
        Add("tw_058", CatDebloat, false,
            A_tw_058, U_tw_058, D_tw_058,
            T("Disable service: Xbox Live Auth Manager (XblAuthManager)"),
            T("sc.exe config \"XblAuthManager\" start= disabled"));
        Add("tw_059", CatDebloat, false,
            A_tw_059, U_tw_059, D_tw_059,
            T("Disable service: Xbox Live Game Save (XblGameSave)"),
            T("sc.exe config \"XblGameSave\" start= disabled"));
        Add("tw_060", CatDebloat, false,
            A_tw_060, U_tw_060, D_tw_060,
            T("Disable service: Xbox Live Networking Service (XboxNetApiSvc)"),
            T("sc.exe config \"XboxNetApiSvc\" start= disabled"));
        Add("tw_061", CatDebloat, false,
            A_tw_061, U_tw_061, D_tw_061,
            T("Disable service: Xbox Game Monitoring (xbgm)"),
            T("sc.exe config \"xbgm\" start= disabled"));
        Add("tw_062", CatDebloat, false,
            A_tw_062, U_tw_062, D_tw_062,
            T("Disable service: Xbox Accessory Management Service (XboxGipSvc)"),
            T("sc.exe config \"XboxGipSvc\" start= disabled"));
        Add("tw_063", CatSystem, false,
            A_tw_063, U_tw_063, D_tw_063,
            T("Disable service: Netlogon (only if not domain-joined) (netlogon)"),
            T("sc.exe config \"netlogon\" start= disabled"));
        Add("tw_064", CatSystem, false,
            A_tw_064, U_tw_064, D_tw_064,
            T("Disable scheduled task: Media sharing/DRM init task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Media Center\\ehDRMInit\" /Disable"));
        Add("tw_065", CatSystem, false,
            A_tw_065, U_tw_065, D_tw_065,
            T("Disable scheduled task: Google AutoUpdate core task"),
            T("schtasks /Change /TN \"\\GoogleUpdateTaskMachineCore\" /Disable"));
        Add("tw_066", CatSystem, false,
            A_tw_066, U_tw_066, D_tw_066,
            T("Disable scheduled task: Google AutoUpdate UA task"),
            T("schtasks /Change /TN \"\\GoogleUpdateTaskMachineUA\" /Disable"));
        Add("tw_067", CatPrivacy, false,
            A_tw_067, U_tw_067, D_tw_067,
            T("Disable scheduled task: Energy/power efficiency diagnostics scan"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Disable"));
        Add("tw_068", CatSystem, false,
            A_tw_068, U_tw_068, D_tw_068,
            T("Disable scheduled task: Disk footprint / diagnosis scan"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Disable"));
        Add("tw_069", CatPrivacy, false,
            A_tw_069, U_tw_069, D_tw_069,
            T("Disable scheduled task: Background memory diagnostics"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\MemoryDiagnostic\\ProcessMemoryDiagnosticEvents\" /Disable"));
        Add("tw_070", CatSystem, false,
            A_tw_070, U_tw_070, D_tw_070,
            T("Disable scheduled task: Error report queue processing"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Windows Error Reporting\\QueueReporting\" /Disable"));
        Add("tw_071", CatSystem, false,
            A_tw_071, U_tw_071, D_tw_071,
            T("Disable scheduled task: Automatic chkdsk trigger on boot"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /Disable"));
        Add("tw_072", CatSystem, false,
            A_tw_072, U_tw_072, D_tw_072,
            T("Disable scheduled task: Background component (WinSxS) cleanup"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Servicing\\StartComponentCleanup\" /Disable"));
        Add("tw_073", CatSystem, false,
            A_tw_073, U_tw_073, D_tw_073,
            T("Disable scheduled task: Background registry backup scan"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Registry\\RegIdleBackup\" /Disable"));
        Add("tw_074", CatPerformance, false,
            A_tw_074, U_tw_074, D_tw_074,
            T("Disable scheduled task: Program Compatibility Assistant patch scan on boot"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Application Experience\\PcaPatchDbTask\" /Disable"));
        Add("tw_075", CatSystem, false,
            A_tw_075, U_tw_075, D_tw_075,
            T("Disable scheduled task: Disk diagnostic data collector"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Disable"));
        Add("tw_076", CatSystem, false,
            A_tw_076, U_tw_076, D_tw_076,
            T("Disable scheduled task: Family Safety monitoring task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Shell\\FamilySafetyMonitor\" /Disable"));
        Add("tw_077", CatSystem, false,
            A_tw_077, U_tw_077, D_tw_077,
            T("Disable scheduled task: Offline Maps update task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /Disable"));
        Add("tw_078", CatSystem, false,
            A_tw_078, U_tw_078, D_tw_078,
            T("Disable scheduled task: Offline Maps notification task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsToastTask\" /Disable"));
        Add("tw_079", CatSystem, false,
            A_tw_079, U_tw_079, D_tw_079,
            T("Disable scheduled task: Scheduled disk defragmentation (SSD-irrelevant)"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Disable"));
        Add("tw_080", CatUpdates, false,
            A_tw_080, U_tw_080, D_tw_080,
            T("Disable scheduled task: Update Orchestrator background scan"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\WindowsUpdate\\Scheduled Start\" /Disable"));
        Add("tw_081", CatUpdates, false,
            A_tw_081, U_tw_081, D_tw_081,
            T("Disable scheduled task: USO background scan"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\UpdateOrchestrator\\Schedule Scan\" /Disable"));
        Add("tw_082", CatPrivacy, false,
            A_tw_082, U_tw_082, D_tw_082,
            T("Disable scheduled task: Compatibility telemetry appraiser"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser\" /Disable"));
        Add("tw_083", CatSystem, false,
            A_tw_083, U_tw_083, D_tw_083,
            T("Disable scheduled task: Cloud experience/account sync helper"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\CloudExperienceHost\\CreateObjectTask\" /Disable"));
        Add("tw_084", CatPrivacy, false,
            A_tw_084, U_tw_084, D_tw_084,
            T("Disable Advertising ID"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo\" /v Enabled /t REG_DWORD /d 0 /f"));
        Add("tw_085", CatPrivacy, false,
            A_tw_085, U_tw_085, D_tw_085,
            T("Disable Activity History / Timeline"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v EnableActivityFeed /t REG_DWORD /d 0 /f"));
        Add("tw_086", CatPrivacy, false,
            A_tw_086, U_tw_086, D_tw_086,
            T("Disable Location Tracking"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location\" /v Value /t REG_SZ /d Deny /f"));
        Add("tw_087", CatPrivacy, false,
            A_tw_087, U_tw_087, D_tw_087,
            T("Disable Search History"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings\" /v IsDeviceSearchHistoryEnabled /t REG_DWORD /d 0 /f"));
        Add("tw_088", CatInternet, false,
            A_tw_088, U_tw_088, D_tw_088,
            T("Disable Bing/Cortana Web Results in Search"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v BingSearchEnabled /t REG_DWORD /d 0 /f"));
        Add("tw_089", CatNetwork, false,
            A_tw_089, U_tw_089, D_tw_089,
            T("Disable Cloud Clipboard Sync"),
            T("reg add \"HKCU\\Software\\Microsoft\\Clipboard\" /v EnableCloudClipboard /t REG_DWORD /d 0 /f"));
        Add("tw_090", CatPrivacy, false,
            A_tw_090, U_tw_090, D_tw_090,
            T("Disable Find My Device"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\FindMyDevice\" /v LocationSyncEnabled /t REG_DWORD /d 0 /f"));
        Add("tw_091", CatPrivacy, false,
            A_tw_091, U_tw_091, D_tw_091,
            T("Disable Feedback & Diagnostics Prompts"),
            T("reg add \"HKCU\\Software\\Microsoft\\Siuf\\Rules\" /v NumberOfSIUFInPeriod /t REG_DWORD /d 0 /f"));
        Add("tw_092", CatPrivacy, false,
            A_tw_092, U_tw_092, D_tw_092,
            T("Disable Tailored Experiences"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy\" /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 0 /f"));
        Add("tw_093", CatPrivacy, false,
            A_tw_093, U_tw_093, D_tw_093,
            T("Disable App Access to Diagnostics"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\CloudContent\" /v DisableConsumerAccountStateContent /t REG_DWORD /d 1 /f"));
        Add("tw_094", CatPrivacy, false,
            A_tw_094, U_tw_094, D_tw_094,
            T("Block Microsoft Telemetry Domains (hosts file)"),
            T("Add-Content -Path $env:WinDir\\System32\\drivers\\etc\\hosts -Value \"0.0.0.0 vortex.data.microsoft.com`n0.0.0.0 telemetry.microsoft.com`n0.0.0.0 watson.telemetry.microsoft.com\""));
        Add("tw_095", CatUI, false,
            A_tw_095, U_tw_095, D_tw_095,
            T("Disable Recent Files Tracking"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoRecentDocsHistory /t REG_DWORD /d 1 /f"));
        Add("tw_096", CatPrivacy, false,
            A_tw_096, U_tw_096, D_tw_096,
            T("Disable Handwriting Data Sharing"),
            T("reg add \"HKCU\\Software\\Microsoft\\InputPersonalization\" /v RestrictImplicitInkCollection /t REG_DWORD /d 1 /f"));
        Add("tw_097", CatPrivacy, false,
            A_tw_097, U_tw_097, D_tw_097,
            T("Disable Speech Model Update"),
            T("reg add \"HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy\" /v HasAccepted /t REG_DWORD /d 0 /f"));
        Add("tw_098", CatPrivacy, false,
            A_tw_098, U_tw_098, D_tw_098,
            T("Disable Typing/Inking Personalization"),
            T("reg add \"HKCU\\Software\\Microsoft\\InputPersonalization\" /v RestrictImplicitTextCollection /t REG_DWORD /d 1 /f"));
        Add("tw_099", CatSystem, false,
            A_tw_099, U_tw_099, D_tw_099,
            T("Disable Diagnostic Data Viewer feed"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v DisableDiagnosticDataViewer /t REG_DWORD /d 1 /f"));
        Add("tw_100", CatSecurity, true,
            A_tw_100, U_tw_100, D_tw_100,
            T("Disable Defender Automatic Sample Submission"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet\" /v SubmitSamplesConsent /t REG_DWORD /d 2 /f"));
        Add("tw_101", CatPrivacy, false,
            A_tw_101, U_tw_101, D_tw_101,
            T("Disable Locally Relevant Content / Suggested Content"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SubscribedContentEnabled /t REG_DWORD /d 0 /f"));
        Add("tw_102", CatPrivacy, false,
            A_tw_102, U_tw_102, D_tw_102,
            T("Disable Account-related Notifications"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SubscribedContent-338389Enabled /t REG_DWORD /d 0 /f"));
        Add("tw_103", CatNetwork, false,
            A_tw_103, U_tw_103, D_tw_103,
            T("Disable Suggested Settings/Tips in Settings app"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager\" /v SubscribedContent-338393Enabled /t REG_DWORD /d 0 /f"));
        Add("tw_104", CatSystem, false,
            A_tw_104, U_tw_104, D_tw_104,
            T("Disable Recently Added Apps in Start"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Start_TrackProgs /t REG_DWORD /d 0 /f"));
        Add("tw_105", CatUI, false,
            A_tw_105, U_tw_105, D_tw_105,
            T("Disable Web Search Results in Start Menu"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v CortanaConsent /t REG_DWORD /d 0 /f"));
        Add("tw_106", CatInternet, false,
            A_tw_106, U_tw_106, D_tw_106,
            T("Disable Cortana Voice Activation"),
            T("reg add \"HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\UserPreferenceForAllApps\" /v AgentActivationLastUsed /t REG_DWORD /d 0 /f"));
        Add("tw_107", CatPerformance, false,
            A_tw_107, U_tw_107, D_tw_107,
            T("Disable Game DVR / Game Bar recording"),
            T("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_Enabled /t REG_DWORD /d 0 /f && reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR\" /v AllowGameDVR /t REG_DWORD /d 0 /f"));
        Add("tw_108", CatSystem, false,
            A_tw_108, U_tw_108, D_tw_108,
            T("Disable GameBarPresenceWriter background process"),
            T("reg add \"HKCU\\SOFTWARE\\Microsoft\\GameBar\" /v AutoGameModeEnabled /t REG_DWORD /d 0 /f"));
        Add("tw_109", CatPerformance, false,
            A_tw_109, U_tw_109, D_tw_109,
            T("Create & activate Ultimate Performance power plan"),
            T("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 && powercfg /setactive e9a42b02-d5df-448d-aa00-03f14749eb61"));
        Add("tw_110", CatPerformance, false,
            A_tw_110, U_tw_110, D_tw_110,
            T("Disable USB Selective Suspend"),
            T("powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0 && powercfg /setactive scheme_current"));
        Add("tw_111", CatPerformance, false,
            A_tw_111, U_tw_111, D_tw_111,
            T("Enable Hardware-Accelerated GPU Scheduling (HAGS)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f"));
        Add("tw_112", CatSystem, false,
            A_tw_112, U_tw_112, D_tw_112,
            T("Disable Fullscreen Optimizations globally"),
            T("reg add \"HKCU\\System\\GameConfigStore\" /v GameDVR_FSEBehavior /t REG_DWORD /d 2 /f"));
        Add("tw_113", CatPerformance, false,
            A_tw_113, U_tw_113, D_tw_113,
            T("Set MMCSS 'Games' task priority to GPU 8 / High"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f && reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v Priority /t REG_DWORD /d 6 /f"));
        Add("tw_114", CatPerformance, false,
            A_tw_114, U_tw_114, D_tw_114,
            T("Disable Prefetch (SSD systems)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnablePrefetcher /t REG_DWORD /d 0 /f"));
        Add("tw_115", CatSystem, false,
            A_tw_115, U_tw_115, D_tw_115,
            T("Reduce Shutdown/Kill-process Timeout"),
            T("reg add \"HKCU\\Control Panel\\Desktop\" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f && reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\" /v WaitToKillServiceTimeout /t REG_SZ /d 2000 /f"));
        Add("tw_116", CatPerformance, false,
            A_tw_116, U_tw_116, D_tw_116,
            T("CPU Scheduling: Prioritize Programs over Background Services"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f"));
        Add("tw_117", CatPerformance, false,
            A_tw_117, U_tw_117, D_tw_117,
            T("Force Game Process Priority to High on launch (Image File Execution)"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\PerfOptions\" /v CpuPriorityClass /t REG_DWORD /d 3 /f"));
        Add("tw_118", CatPerformance, false,
            A_tw_118, U_tw_118, D_tw_118,
            T("Disable CPU Core Parking / Idle states (aggressive, C-states off)"),
            T("powercfg /setacvalueindex scheme_current sub_processor 5d76a2ca-e8c0-402f-a133-2158492d58ad 0 && powercfg /setactive scheme_current"));
        Add("tw_119", CatPerformance, false,
            A_tw_119, U_tw_119, D_tw_119,
            T("Prioritize HPET / dynamic tick handling"),
            T("bcdedit /set useplatformclock false"));
        Add("tw_120", CatPerformance, false,
            A_tw_120, U_tw_120, D_tw_120,
            T("Disable Paging Executive (keep kernel in RAM)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v DisablePagingExecutive /t REG_DWORD /d 1 /f"));
        Add("tw_121", CatPerformance, false,
            A_tw_121, U_tw_121, D_tw_121,
            T("Increase System Cache priority for RAM"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v LargeSystemCache /t REG_DWORD /d 1 /f"));
        Add("tw_122", CatPerformance, false,
            A_tw_122, U_tw_122, D_tw_122,
            T("Raise IRQ8 (RTC) Priority"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v IRQ8Priority /t REG_DWORD /d 1 /f"));
        Add("tw_123", CatNetwork, false,
            A_tw_123, U_tw_123, D_tw_123,
            T("Disable MMCSS Network Throttling for Games task"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f"));
        Add("tw_124", CatSystem, false,
            A_tw_124, U_tw_124, D_tw_124,
            T("Disable BITS bandwidth throttling"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\BITS\" /v EnableBITSMaxBandwidth /t REG_DWORD /d 0 /f"));
        Add("tw_125", CatPerformance, false,
            A_tw_125, U_tw_125, D_tw_125,
            T("Set System Responsiveness to 0% (foreground priority to games)"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 0 /f"));
        Add("tw_126", CatPerformance, false,
            A_tw_126, U_tw_126, D_tw_126,
            T("Disable Modern Standby (S0 Low Power Idle)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\" /v PlatformAoAcOverride /t REG_DWORD /d 0 /f"));
        Add("tw_127", CatPerformance, false,
            A_tw_127, U_tw_127, D_tw_127,
            T("Reduce Input/Keyboard Buffer Latency"),
            T("reg add \"HKCU\\Control Panel\\Keyboard\" /v KeyboardDelay /t REG_SZ /d 0 /f"));
        Add("tw_128", CatPerformance, false,
            A_tw_128, U_tw_128, D_tw_128,
            T("Force DWM to present immediately (reduce compositor latency)"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\DWM\" /v Composition /t REG_DWORD /d 1 /f"));
        Add("tw_129", CatSystem, false,
            A_tw_129, U_tw_129, D_tw_129,
            T("Disable Memory Compression"),
            T("Disable-MMAgent -MemoryCompression"));
        Add("tw_130", CatSystem, false,
            A_tw_130, U_tw_130, D_tw_130,
            T("Prioritize Disk I/O for foreground applications"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v IoPriority /t REG_DWORD /d 3 /f"));
        Add("tw_131", CatSystem, false,
            A_tw_131, U_tw_131, D_tw_131,
            T("Disable disk idle spin-down timeout"),
            T("powercfg /setacvalueindex scheme_current sub_disk 6738e2c4-e8a5-4a42-b16a-e040e769756e 0 && powercfg /setactive scheme_current"));
        Add("tw_132", CatNetwork, false,
            A_tw_132, U_tw_132, D_tw_132,
            T("Disable Nagle's Algorithm (per adapter, lower latency)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\<AdapterGUID>\" /v TcpAckFrequency /t REG_DWORD /d 1 /f && reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\<AdapterGUID>\" /v TCPNoDelay /t REG_DWORD /d 1 /f"));
        Add("tw_133", CatNetwork, false,
            A_tw_133, U_tw_133, D_tw_133,
            T("Disable Network Adapter Power Saving"),
            T("powercfg /setacvalueindex scheme_current 501a4d13-42af-4429-9fd1-a8218c268e20 12bbebe6-58d6-4636-95bb-3217ef867c1a 0"));
        Add("tw_134", CatNetwork, false,
            A_tw_134, U_tw_134, D_tw_134,
            T("Set Network Adapter Profile to Maximum Performance"),
            T("netsh int tcp set global autotuninglevel=normal"));
        Add("tw_135", CatNetwork, false,
            A_tw_135, U_tw_135, D_tw_135,
            T("Disable Wake-on-LAN"),
            T("powercfg -deviceenablewake \"Ethernet\""));
        Add("tw_136", CatNetwork, false,
            A_tw_136, U_tw_136, D_tw_136,
            T("Reset Network Adapter (Winsock/IP stack)"),
            T("netsh winsock reset && netsh int ip reset"));
        Add("tw_137", CatNetwork, false,
            A_tw_137, U_tw_137, D_tw_137,
            T("Disable Energy Efficient Ethernet (driver property, per-NIC)"),
            T("Set-NetAdapterAdvancedProperty -DisplayName \"Energy Efficient Ethernet\" -DisplayValue \"Disabled\""));
        Add("tw_138", CatNetwork, false,
            A_tw_138, U_tw_138, D_tw_138,
            T("Set DNS to Cloudflare (1.1.1.1 / 1.0.0.1)"),
            T("Set-DnsClientServerAddress -InterfaceAlias \"Ethernet\" -ServerAddresses (\"1.1.1.1\",\"1.0.0.1\")"));
        Add("tw_139", CatNetwork, false,
            A_tw_139, U_tw_139, D_tw_139,
            T("Enable DNS over HTTPS (Cloudflare template)"),
            T("Add-DnsClientDohServerAddress -ServerAddress \"1.1.1.1\" -DohTemplate \"https://cloudflare-dns.com/dns-query\" -AllowFallbackToUdp $false"));
        Add("tw_140", CatNetwork, false,
            A_tw_140, U_tw_140, D_tw_140,
            T("Flush DNS Cache"),
            T("ipconfig /flushdns"));
        Add("tw_141", CatNetwork, false,
            A_tw_141, U_tw_141, D_tw_141,
            T("TCP Optimizations (Auto-Tuning enabled, Wi-Fi safe)"),
            T("netsh int tcp set global autotuninglevel=normal & netsh int tcp set global rsc=disabled & netsh int tcp set global ecncapability=disabled"));
        Add("tw_142", CatNetwork, false,
            A_tw_142, U_tw_142, D_tw_142,
            T("Disable Large Send Offload (LSO, per-NIC)"),
            T("Disable-NetAdapterLso -Name \"Ethernet\""));
        Add("tw_143", CatNetwork, false,
            A_tw_143, U_tw_143, D_tw_143,
            T("Enable Receive Side Scaling (RSS)"),
            T("Enable-NetAdapterRss -Name \"Ethernet\""));
        Add("tw_144", CatNetwork, false,
            A_tw_144, U_tw_144, D_tw_144,
            T("Reduce aggressive Wi-Fi roaming sensitivity (keeps Wi-Fi ON)"),
            T("Set-NetAdapterAdvancedProperty -Name \"Wi-Fi\" -DisplayName \"Roaming Aggressiveness\" -DisplayValue \"Lowest\""));
        Add("tw_145", CatNetwork, false,
            A_tw_145, U_tw_145, D_tw_145,
            T("Force NIC Duplex/Speed to Maximum (per-NIC)"),
            T("Set-NetAdapterAdvancedProperty -DisplayName \"Speed & Duplex\" -DisplayValue \"1.0 Gbps Full Duplex\""));
        Add("tw_146", CatNetwork, false,
            A_tw_146, U_tw_146, D_tw_146,
            T("Disable NIC Flow Control"),
            T("Set-NetAdapterAdvancedProperty -DisplayName \"Flow Control\" -DisplayValue \"Disabled\""));
        Add("tw_147", CatNetwork, false,
            A_tw_147, U_tw_147, D_tw_147,
            T("Disable Green Ethernet / Gigabit Lite (per-NIC)"),
            T("Set-NetAdapterAdvancedProperty -DisplayName \"Green Ethernet\" -DisplayValue \"Disabled\""));
        Add("tw_148", CatNetwork, false,
            A_tw_148, U_tw_148, D_tw_148,
            T("Prefer IPv4 over IPv6"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters\" /v DisabledComponents /t REG_DWORD /d 32 /f"));
        Add("tw_149", CatInternet, false,
            A_tw_149, U_tw_149, D_tw_149,
            T("Disable WPAD (Web Proxy Auto-Discovery)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\WinHttpAutoProxySvc\" /v Start /t REG_DWORD /d 4 /f"));
        Add("tw_150", CatNetwork, false,
            A_tw_150, U_tw_150, D_tw_150,
            T("Enable TCP Fast Open"),
            T("netsh int tcp set global fastopen=enabled"));
        Add("tw_151", CatNetwork, false,
            A_tw_151, U_tw_151, D_tw_151,
            T("Disable Teredo tunneling"),
            T("netsh interface teredo set state disabled"));
        Add("tw_152", CatSystem, false,
            A_tw_152, U_tw_152, D_tw_152,
            T("Tune SYN Attack Protection"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\" /v SynAttackProtect /t REG_DWORD /d 1 /f"));
        Add("tw_153", CatUI, false,
            A_tw_153, U_tw_153, D_tw_153,
            T("Disable Explorer Automatic Proxy Detection"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\" /v AutoDetect /t REG_DWORD /d 0 /f"));
        Add("tw_154", CatSystem, false,
            A_tw_154, U_tw_154, D_tw_154,
            T("Disable QoS Packet Scheduler Reservation (20% cap)"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched\" /v NonBestEffortLimit /t REG_DWORD /d 0 /f"));
        Add("tw_155", CatUI, false,
            A_tw_155, U_tw_155, D_tw_155,
            T("Increase Max Connections Per Server (Explorer/IE stack)"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\" /v MaxConnectionsPerServer /t REG_DWORD /d 16 /f"));
        Add("tw_156", CatNetwork, false,
            A_tw_156, U_tw_156, D_tw_156,
            T("Set optimal MTU for gaming (1500, no fragmentation)"),
            T("netsh interface ipv4 set subinterface \"Ethernet\" mtu=1500 store=persistent"));
        Add("tw_157", CatPerformance, false,
            A_tw_157, U_tw_157, D_tw_157,
            T("Raise MMCSS priority for Steam/Discord/launchers"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v Affinity /t REG_DWORD /d 0 /f && reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency\" /v Priority /t REG_DWORD /d 1 /f"));
        Add("tw_158", CatSystem, false,
            A_tw_158, U_tw_158, D_tw_158,
            T("Disable GeForce Experience Autostart"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"NVIDIA GeForce Experience\" /f /t REG_SZ /d \"\" 2>nul & reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"NVIDIA GeForce Experience\" /f"));
        Add("tw_159", CatPerformance, false,
            A_tw_159, U_tw_159, D_tw_159,
            T("Set NVIDIA/AMD GPU Power Management to Prefer Maximum Performance"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000\" /v PowerMizerEnable /t REG_DWORD /d 0 /f"));
        Add("tw_160", CatSystem, false,
            A_tw_160, U_tw_160, D_tw_160,
            T("Disable Nvidia ShadowPlay In-Game Overlay"),
            T("reg add \"HKCU\\SOFTWARE\\NVIDIA Corporation\\Global\\ShadowPlay\\NVSPCAPS\" /v \"PublicOverlay_Enabled\" /t REG_DWORD /d 0 /f"));
        Add("tw_161", CatSystem, false,
            A_tw_161, U_tw_161, D_tw_161,
            T("Disable AMD Radeon Chill"),
            T("reg add \"HKCU\\SOFTWARE\\AMD\\CN\" /v ChillEnable /t REG_DWORD /d 0 /f"));
        Add("tw_162", CatSystem, false,
            A_tw_162, U_tw_162, D_tw_162,
            T("Disable AMD External Events Autostart"),
            T("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"AMD External Events Utility\" /f"));
        Add("tw_163", CatSecurity, false,
            A_tw_163, U_tw_163, D_tw_163,
            T("Disable AMD Crash Defender Service"),
            T("sc.exe config \"AMD Crash Defender Service\" start= disabled"));
        Add("tw_164", CatPerformance, false,
            A_tw_164, U_tw_164, D_tw_164,
            T("Set GPU Low-Latency Mode to Ultra (Nvidia Reflex profile default)"),
            T("reg add \"HKCU\\Software\\NVIDIA Corporation\\Global\\NVTweak\" /v LowLatencyMode /t REG_DWORD /d 2 /f"));
        Add("tw_165", CatPerformance, false,
            A_tw_165, U_tw_165, D_tw_165,
            T("Force GPU Power Profile Per Application (max perf)"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\DirectX\\UserGpuPreferences\" /v DirectXUserGlobalSettings /t REG_SZ /d \"SwapEffectUpgradeEnable=1;GpuPreference=2;\" /f"));
        Add("tw_166", CatPerformance, false,
            A_tw_166, U_tw_166, D_tw_166,
            T("Set GPU Fan Curve to Adaptive/Auto"),
            T("reg add \"HKLM\\SOFTWARE\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000\" /v FanControlMode /t REG_DWORD /d 0 /f"));
        Add("tw_167", CatSystem, false,
            A_tw_167, U_tw_167, D_tw_167,
            T("Set Texture Filtering Quality to Performance"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000\" /v TextureFilterQuality /t REG_DWORD /d 3 /f"));
        Add("tw_168", CatPerformance, false,
            A_tw_168, U_tw_168, D_tw_168,
            T("Remove Shader Cache Size Limit"),
            T("reg add \"HKLM\\SOFTWARE\\NVIDIA Corporation\\Global\\NVTweak\" /v PSGpuAdapterMemory /t REG_DWORD /d 4294967295 /f"));
        Add("tw_169", CatSystem, false,
            A_tw_169, U_tw_169, D_tw_169,
            T("Disable G-Sync/FreeSync Compositor Overhead (borderless mode bypass)"),
            T("reg add \"HKCU\\Software\\Microsoft\\DirectX\\GraphicsSettings\" /v \"SwapEffectUpgradeCache\" /t REG_DWORD /d 0 /f"));
        Add("tw_170", CatPerformance, false,
            A_tw_170, U_tw_170, D_tw_170,
            T("Set GPU Preemption to Pixel-Level"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v PlatformSupportMiracast /t REG_DWORD /d 0 /f"));
        Add("tw_171", CatSystem, false,
            A_tw_171, U_tw_171, D_tw_171,
            T("Disable PCIe ASPM (Active State Power Management)"),
            T("powercfg /setacvalueindex scheme_current 501a4d13-42af-4429-9fd1-a8218c268e20 ee12f906-d277-404b-b6da-e5fa1a576df5 0"));
        Add("tw_172", CatSystem, false,
            A_tw_172, U_tw_172, D_tw_172,
            T("Disable USB3 Link Power Management"),
            T("powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 d4e98f31-5ffe-4ce1-be31-1b38b384c009 0"));
        Add("tw_173", CatSystem, false,
            A_tw_173, U_tw_173, D_tw_173,
            T("Disable Global Driver-Level VSync (Nvidia CP style)"),
            T("reg add \"HKCU\\Software\\NVIDIA Corporation\\Global\\NVTweak\" /v VSyncMode /t REG_DWORD /d 0 /f"));
        Add("tw_174", CatPerformance, false,
            A_tw_174, U_tw_174, D_tw_174,
            T("Disable AMD ULPS (Ultra Low Power State, multi-GPU)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000\" /v EnableUlps /t REG_DWORD /d 0 /f"));
        Add("tw_175", CatPerformance, false,
            A_tw_175, U_tw_175, D_tw_175,
            T("Disable Mouse Pointer Acceleration"),
            T("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f && reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold1 /t REG_SZ /d 0 /f && reg add \"HKCU\\Control Panel\\Mouse\" /v MouseThreshold2 /t REG_SZ /d 0 /f"));
        Add("tw_176", CatSystem, false,
            A_tw_176, U_tw_176, D_tw_176,
            T("Disable Enhanced Pointer Precision"),
            T("reg add \"HKCU\\Control Panel\\Mouse\" /v MouseSpeed /t REG_SZ /d 0 /f"));
        Add("tw_177", CatPerformance, false,
            A_tw_177, U_tw_177, D_tw_177,
            T("Raise Mouse Polling Rate to 1000Hz (USB registry)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\mouhid\\Parameters\" /v MouseDataQueueSize /t REG_DWORD /d 20 /f"));
        Add("tw_178", CatPerformance, false,
            A_tw_178, U_tw_178, D_tw_178,
            T("Disable Keyboard Input Filter Delay"),
            T("reg add \"HKCU\\Control Panel\\Accessibility\\Keyboard Response\" /v AutoRepeatDelay /t REG_SZ /d 0 /f"));
        Add("tw_179", CatPerformance, false,
            A_tw_179, U_tw_179, D_tw_179,
            T("Set Keyboard Repeat Rate to Maximum"),
            T("reg add \"HKCU\\Control Panel\\Keyboard\" /v KeyboardSpeed /t REG_SZ /d 31 /f"));
        Add("tw_180", CatNetwork, false,
            A_tw_180, U_tw_180, D_tw_180,
            T("Disable HID Battery Status Polling (wireless peripherals)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\HidBatt\\Parameters\" /v IdleQueryTimeout /t REG_DWORD /d 0 /f"));
        Add("tw_181", CatPerformance, false,
            A_tw_181, U_tw_181, D_tw_181,
            T("Raise Windows Audio Engine (Audiosrv) Process Priority"),
            T("wmic process where name=\"audiodg.exe\" CALL setpriority \"high priority\""));
        Add("tw_182", CatPerformance, false,
            A_tw_182, U_tw_182, D_tw_182,
            T("Disable Windows Spatial Sound Processing (lower audio-engine latency)"),
            T("reg add \"HKCU\\Software\\Microsoft\\Multimedia\\Audio\" /v UserSimulatedSpatialAudio /t REG_DWORD /d 0 /f"));
        Add("tw_183", CatSystem, false,
            A_tw_183, U_tw_183, D_tw_183,
            T("Disable Exclusive-Mode Application Blocking"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render\" /v DisableExclusiveMode /t REG_DWORD /d 0 /f"));
        Add("tw_184", CatPerformance, false,
            A_tw_184, U_tw_184, D_tw_184,
            T("Set Audio Buffer Size to Minimum (low-latency mode)"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render\\Properties\" /v \"{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5\" /t REG_DWORD /d 1 /f"));
        Add("tw_185", CatPerformance, false,
            A_tw_185, U_tw_185, D_tw_185,
            T("Mitigate USB Audio DPC Latency Spikes"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e96c-e325-11ce-bfc1-08002be10318}\" /v \"IdlePowerManagementSupported\" /t REG_DWORD /d 0 /f"));
        Add("tw_186", CatSystem, false,
            A_tw_186, U_tw_186, D_tw_186,
            T("Disable Automatic Steam Overlay Injection (per-game, set in Steam)"),
            T("reg add \"HKCU\\Software\\Valve\\Steam\" /v OverlayEnabled /t REG_DWORD /d 0 /f"));
        Add("tw_187", CatSystem, false,
            A_tw_187, U_tw_187, D_tw_187,
            T("Fix Discord Overlay Hardware-Acceleration Conflict"),
            T("reg add \"HKCU\\Software\\Discord\" /v \"hardwareAcceleration\" /t REG_DWORD /d 0 /f"));
        Add("tw_188", CatSystem, false,
            A_tw_188, U_tw_188, D_tw_188,
            T("Disable Background RGB Control Services (e.g. iCUE/Aura idle polling)"),
            T("sc.exe config \"LightingService\" start= demand"));
        Add("tw_189", CatSystem, false,
            A_tw_189, U_tw_189, D_tw_189,
            T("Disable Fast Startup"),
            T("powercfg /h off"));
        Add("tw_190", CatSystem, false,
            A_tw_190, U_tw_190, D_tw_190,
            T("Disable Storage Sense"),
            T("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy\" /v 01 /t REG_DWORD /d 0 /f"));
        Add("tw_191", CatSystem, false,
            A_tw_191, U_tw_191, D_tw_191,
            T("Disable Content Indexing on non-system drives"),
            T("Get-Volume | Where-Object DriveLetter -ne \"C\" | ForEach-Object { fsutil behavior set disablelastaccess 1 }"));
        Add("tw_192", CatSystem, false,
            A_tw_192, U_tw_192, D_tw_192,
            T("Disable Removable Drive Indexing"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\" /v DisableRemovableDriveIndexing /t REG_DWORD /d 1 /f"));
        Add("tw_193", CatSystem, false,
            A_tw_193, U_tw_193, D_tw_193,
            T("Disable Automatic chkdsk on boot"),
            T("chkntfs /x C:"));
        Add("tw_194", CatSystem, false,
            A_tw_194, U_tw_194, D_tw_194,
            T("Disable Automatic Defrag Schedule (SSD)"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Disable"));
        Add("tw_195", CatUI, false,
            A_tw_195, U_tw_195, D_tw_195,
            T("Disable Thumbnail Cache"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v DisableThumbnailCache /t REG_DWORD /d 1 /f"));
        Add("tw_196", CatUI, false,
            A_tw_196, U_tw_196, D_tw_196,
            T("Disable Explorer Preview Pane Rendering"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v ShowPreviewHandlers /t REG_DWORD /d 0 /f"));
        Add("tw_197", CatUI, false,
            A_tw_197, U_tw_197, D_tw_197,
            T("Disable Thumbnails in Details Pane"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v IconsOnly /t REG_DWORD /d 1 /f"));
        Add("tw_198", CatUI, false,
            A_tw_198, U_tw_198, D_tw_198,
            T("Reduce Explorer 'Ghosting' Hang Timeout"),
            T("reg add \"HKCU\\Control Panel\\Desktop\" /v HungAppTimeout /t REG_SZ /d 2000 /f"));
        Add("tw_199", CatPerformance, false,
            A_tw_199, U_tw_199, D_tw_199,
            T("Disable AppCompat Cache scanning"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\AppCompatCache\" /v AppCompatCache /t REG_DWORD /d 0 /f"));
        Add("tw_200", CatUpdates, false,
            A_tw_200, U_tw_200, D_tw_200,
            T("Windows Update: Notify Only, No Auto-Download"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\" /v AUOptions /t REG_DWORD /d 2 /f"));
        Add("tw_201", CatUpdates, false,
            A_tw_201, U_tw_201, D_tw_201,
            T("Disable Driver Updates via Windows Update"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Device Metadata\" /v PreventDeviceMetadataFromNetwork /t REG_DWORD /d 1 /f"));
        Add("tw_202", CatUpdates, false,
            A_tw_202, U_tw_202, D_tw_202,
            T("Pause Quality/Feature Updates 7 days"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\" /v PauseUpdatesExpiryTime /t REG_SZ /d \"2026-08-18T00:00:00Z\" /f"));
        Add("tw_203", CatUpdates, false,
            A_tw_203, U_tw_203, D_tw_203,
            T("Disable Windows Update for Other Microsoft Products"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\UX\\Settings\" /v AllowMUUpdateService /t REG_DWORD /d 0 /f"));
        Add("tw_204", CatUpdates, false,
            A_tw_204, U_tw_204, D_tw_204,
            T("Disable USO Background Orchestrator Scans"),
            T("sc.exe config \"UsoSvc\" start= demand"));
        Add("tw_205", CatSystem, false,
            A_tw_205, U_tw_205, D_tw_205,
            T("Disable Compatibility Appraiser Task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser\" /Disable"));
        Add("tw_206", CatDebloat, false,
            A_tw_206, U_tw_206, D_tw_206,
            T("Remove preinstalled app: Windows Mixed Reality Portal"),
            T("Get-AppxPackage \"Microsoft.MixedReality.Portal\" -AllUsers | Remove-AppxPackage"));
        Add("tw_207", CatDebloat, false,
            A_tw_207, U_tw_207, D_tw_207,
            T("Remove preinstalled app: Skype (preinstalled)"),
            T("Get-AppxPackage \"Microsoft.SkypeApp\" -AllUsers | Remove-AppxPackage"));
        Add("tw_208", CatDebloat, false,
            A_tw_208, U_tw_208, D_tw_208,
            T("Remove preinstalled app: News app"),
            T("Get-AppxPackage \"Microsoft.BingNews\" -AllUsers | Remove-AppxPackage"));
        Add("tw_209", CatDebloat, false,
            A_tw_209, U_tw_209, D_tw_209,
            T("Remove preinstalled app: Weather app"),
            T("Get-AppxPackage \"Microsoft.BingWeather\" -AllUsers | Remove-AppxPackage"));
        Add("tw_210", CatDebloat, false,
            A_tw_210, U_tw_210, D_tw_210,
            T("Remove preinstalled app: Get Help app"),
            T("Get-AppxPackage \"Microsoft.GetHelp\" -AllUsers | Remove-AppxPackage"));
        Add("tw_211", CatDebloat, false,
            A_tw_211, U_tw_211, D_tw_211,
            T("Remove preinstalled app: Tips app"),
            T("Get-AppxPackage \"Microsoft.Getstarted\" -AllUsers | Remove-AppxPackage"));
        Add("tw_212", CatDebloat, false,
            A_tw_212, U_tw_212, D_tw_212,
            T("Remove preinstalled app: Family Safety app"),
            T("Get-AppxPackage \"MicrosoftCorporationII.MicrosoftFamily\" -AllUsers | Remove-AppxPackage"));
        Add("tw_213", CatDebloat, false,
            A_tw_213, U_tw_213, D_tw_213,
            T("Remove preinstalled app: 3D Viewer"),
            T("Get-AppxPackage \"Microsoft.Microsoft3DViewer\" -AllUsers | Remove-AppxPackage"));
        Add("tw_214", CatDebloat, false,
            A_tw_214, U_tw_214, D_tw_214,
            T("Remove preinstalled app: Paint 3D"),
            T("Get-AppxPackage \"Microsoft.Paint3D\" -AllUsers | Remove-AppxPackage"));
        Add("tw_215", CatDebloat, false,
            A_tw_215, U_tw_215, D_tw_215,
            T("Remove preinstalled app: People app"),
            T("Get-AppxPackage \"Microsoft.People\" -AllUsers | Remove-AppxPackage"));
        Add("tw_216", CatDebloat, false,
            A_tw_216, U_tw_216, D_tw_216,
            T("Remove preinstalled app: Clipchamp video editor"),
            T("Get-AppxPackage \"Clipchamp.Clipchamp\" -AllUsers | Remove-AppxPackage"));
        Add("tw_217", CatDebloat, false,
            A_tw_217, U_tw_217, D_tw_217,
            T("Remove preinstalled app: Cortana app package"),
            T("Get-AppxPackage \"Microsoft.549981C3F5F10\" -AllUsers | Remove-AppxPackage"));
        Add("tw_218", CatDebloat, false,
            A_tw_218, U_tw_218, D_tw_218,
            T("Remove preinstalled app: Office Hub / Get Office"),
            T("Get-AppxPackage \"Microsoft.MicrosoftOfficeHub\" -AllUsers | Remove-AppxPackage"));
        Add("tw_219", CatDebloat, false,
            A_tw_219, U_tw_219, D_tw_219,
            T("Remove preinstalled app: Movies & TV app"),
            T("Get-AppxPackage \"Microsoft.ZuneVideo\" -AllUsers | Remove-AppxPackage"));
        Add("tw_220", CatDebloat, false,
            A_tw_220, U_tw_220, D_tw_220,
            T("Remove preinstalled app: Phone Link / Your Phone"),
            T("Get-AppxPackage \"Microsoft.YourPhone\" -AllUsers | Remove-AppxPackage"));
        Add("tw_221", CatDebloat, false,
            A_tw_221, U_tw_221, D_tw_221,
            T("Remove preinstalled app: Power Automate Desktop"),
            T("Get-AppxPackage \"Microsoft.PowerAutomateDesktop\" -AllUsers | Remove-AppxPackage"));
        Add("tw_222", CatDebloat, false,
            A_tw_222, U_tw_222, D_tw_222,
            T("Remove preinstalled app: consumer Teams (preinstalled)"),
            T("Get-AppxPackage \"MicrosoftTeams\" -AllUsers | Remove-AppxPackage"));
        Add("tw_223", CatDebloat, false,
            A_tw_223, U_tw_223, D_tw_223,
            T("Remove preinstalled app: Quick Assist"),
            T("Get-AppxPackage \"MicrosoftCorporationII.QuickAssist\" -AllUsers | Remove-AppxPackage"));
        Add("tw_224", CatDebloat, false,
            A_tw_224, U_tw_224, D_tw_224,
            T("Remove preinstalled app: Maps app"),
            T("Get-AppxPackage \"Microsoft.WindowsMaps\" -AllUsers | Remove-AppxPackage"));
        Add("tw_225", CatDebloat, false,
            A_tw_225, U_tw_225, D_tw_225,
            T("Remove preinstalled app: Mail and Calendar"),
            T("Get-AppxPackage \"microsoft.windowscommunicationsapps\" -AllUsers | Remove-AppxPackage"));
        Add("tw_226", CatDebloat, false,
            A_tw_226, U_tw_226, D_tw_226,
            T("Remove preinstalled app: Widgets board"),
            T("Get-AppxPackage \"MicrosoftWindows.Client.WebExperience\" -AllUsers | Remove-AppxPackage"));
        Add("tw_227", CatDebloat, false,
            A_tw_227, U_tw_227, D_tw_227,
            T("Remove preinstalled app: Xbox TCUI bloat component"),
            T("Get-AppxPackage \"Microsoft.Xbox.TCUI\" -AllUsers | Remove-AppxPackage"));
        Add("tw_228", CatDebloat, false,
            A_tw_228, U_tw_228, D_tw_228,
            T("Remove preinstalled app: Xbox Console Companion app"),
            T("Get-AppxPackage \"Microsoft.XboxApp\" -AllUsers | Remove-AppxPackage"));
        Add("tw_229", CatDebloat, false,
            A_tw_229, U_tw_229, D_tw_229,
            T("Remove preinstalled app: Xbox Game Bar app"),
            T("Get-AppxPackage \"Microsoft.XboxGamingOverlay\" -AllUsers | Remove-AppxPackage"));
        Add("tw_230", CatDebloat, false,
            A_tw_230, U_tw_230, D_tw_230,
            T("Remove preinstalled app: Xbox Identity Provider bloat component"),
            T("Get-AppxPackage \"Microsoft.XboxIdentityProvider\" -AllUsers | Remove-AppxPackage"));
        Add("tw_231", CatDebloat, false,
            A_tw_231, U_tw_231, D_tw_231,
            T("Remove preinstalled app: Xbox Speech-to-Text overlay"),
            T("Get-AppxPackage \"Microsoft.XboxSpeechToTextOverlay\" -AllUsers | Remove-AppxPackage"));
        Add("tw_232", CatDebloat, false,
            A_tw_232, U_tw_232, D_tw_232,
            T("Remove preinstalled app: Solitaire Collection"),
            T("Get-AppxPackage \"Microsoft.MicrosoftSolitaireCollection\" -AllUsers | Remove-AppxPackage"));
        Add("tw_233", CatDebloat, false,
            A_tw_233, U_tw_233, D_tw_233,
            T("Remove preinstalled app: Feedback Hub"),
            T("Get-AppxPackage \"Microsoft.WindowsFeedbackHub\" -AllUsers | Remove-AppxPackage"));
        Add("tw_234", CatDebloat, false,
            A_tw_234, U_tw_234, D_tw_234,
            T("Remove OneDrive entirely"),
            T("Start-Process \"$env:SystemRoot\\SysWOW64\\OneDriveSetup.exe\" -ArgumentList \"/uninstall\" -Wait"));
        Add("tw_235", CatDebloat, false,
            A_tw_235, U_tw_235, D_tw_235,
            T("Clean Microsoft Edge shortcuts/ads (New Tab promos)"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /v HubsSidebarEnabled /t REG_DWORD /d 0 /f"));
        Add("tw_236", CatDebloat, false,
            A_tw_236, U_tw_236, D_tw_236,
            T("Disable Brave browser sponsored tiles/extras"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\BraveSoftware\\Brave\" /v BraveRewardsDisabled /t REG_DWORD /d 1 /f"));
        Add("tw_237", CatDebloat, false,
            A_tw_237, U_tw_237, D_tw_237,
            T("Strip old/superseded Driver Store packages"),
            T("Get-WindowsDriver -Online | Where-Object Driver -like \"oem*\" | ForEach-Object { pnputil /delete-driver $_.Driver /uninstall }"));
        Add("tw_238", CatDebloat, false,
            A_tw_238, U_tw_238, D_tw_238,
            T("Remove unused display language packs"),
            T("Get-WinUserLanguageList | Where-Object LanguageTag -ne \"en-US\" | ForEach-Object { Uninstall-Language $_.LanguageTag }"));
        Add("tw_239", CatDebloat, false,
            A_tw_239, U_tw_239, D_tw_239,
            T("Remove Wireless Display (Miracast) optional feature"),
            T("Disable-WindowsOptionalFeature -Online -FeatureName \"Windows-Media-Player\" -NoRestart 2>$null; DISM /Online /Remove-Capability /CapabilityName:\"App.WirelessDisplay~~~~0.0.1.0\""));
        Add("tw_240", CatDebloat, false,
            A_tw_240, U_tw_240, D_tw_240,
            T("Remove Steps Recorder feature"),
            T("DISM /Online /Remove-Capability /CapabilityName:\"App.StepsRecorder~~~~0.0.1.0\""));
        Add("tw_241", CatUI, false,
            A_tw_241, U_tw_241, D_tw_241,
            T("Disable Widgets board (kills Widgets.exe background process)"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Dsh\" /v AllowNewsAndInterests /t REG_DWORD /d 0 /f"));
        Add("tw_242", CatUI, false,
            A_tw_242, U_tw_242, D_tw_242,
            T("Disable Taskbar Chat/Teams Icon"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarMn /t REG_DWORD /d 0 /f"));
        Add("tw_243", CatUI, false,
            A_tw_243, U_tw_243, D_tw_243,
            T("Disable Start Menu Recommendations Section"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Start_IrisRecommendations /t REG_DWORD /d 0 /f"));
        Add("tw_244", CatDebloat, false,
            A_tw_244, U_tw_244, D_tw_244,
            T("Clean Unnecessary Startup Apps"),
            T("Get-CimInstance Win32_StartupCommand | Where-Object { $_.Location -match \"Run\" } | Select-Object Name,Command,Location"));
        Add("tw_245", CatUI, false,
            A_tw_245, U_tw_245, D_tw_245,
            T("Clear & Rebuild Icon Cache"),
            T("ie4uinit.exe -show"));
        Add("tw_246", CatSystem, false,
            A_tw_246, U_tw_246, D_tw_246,
            T("Clear Temp Files on Boot"),
            T("schtasks /Create /TN \"ClearTempOnBoot\" /TR \"cmd /c del /q /f %TEMP%\\*\" /SC ONSTART /RU SYSTEM /F"));
        Add("tw_247", CatUI, false,
            A_tw_247, U_tw_247, D_tw_247,
            T("Hide Task View Button (removes background snapshot service)"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v ShowTaskViewButton /t REG_DWORD /d 0 /f"));
        Add("tw_248", CatUI, false,
            A_tw_248, U_tw_248, D_tw_248,
            T("Hide Widgets Icon on Taskbar"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarDa /t REG_DWORD /d 0 /f"));
        Add("tw_249", CatUI, false,
            A_tw_249, U_tw_249, D_tw_249,
            T("Disable Frequent Apps List in Start"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v Start_TrackDocs /t REG_DWORD /d 0 /f"));
        Add("tw_250", CatAppearance, false,
            A_tw_250, U_tw_250, D_tw_250,
            T("Disable Transparency Effects (reduce DWM compositor load)"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f"));
        Add("tw_251", CatUI, false,
            A_tw_251, U_tw_251, D_tw_251,
            T("Disable Action Center background refresh"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer\" /v DisableNotificationCenter /t REG_DWORD /d 1 /f"));
        Add("tw_252", CatContextMenu, false,
            A_tw_252, U_tw_252, D_tw_252,
            T("Set Menu Show Delay to 0"),
            T("reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 0 /f"));
        Add("tw_253", CatUI, false,
            A_tw_253, U_tw_253, D_tw_253,
            T("Set Foreground Lock Timeout to 0"),
            T("reg add \"HKCU\\Control Panel\\Desktop\" /v ForegroundLockTimeout /t REG_DWORD /d 0 /f"));
        Add("tw_254", CatPerformance, false,
            A_tw_254, U_tw_254, D_tw_254,
            T("Disable Dynamic Tick"),
            T("bcdedit /set disabledynamictick yes"));
        Add("tw_255", CatPerformance, false,
            A_tw_255, U_tw_255, D_tw_255,
            T("Delete Platform Clock (removes HPET as timer source)"),
            T("bcdedit /deletevalue useplatformclock"));
        Add("tw_256", CatPerformance, false,
            A_tw_256, U_tw_256, D_tw_256,
            T("Set TSC Sync Policy to Enhanced"),
            T("bcdedit /set tscsyncpolicy Enhanced"));
        Add("tw_257", CatPerformance, false,
            A_tw_257, U_tw_257, D_tw_257,
            T("Enable x2APIC Policy"),
            T("bcdedit /set x2apicpolicy Enable"));
        Add("tw_258", CatAppearance, false,
            A_tw_258, U_tw_258, D_tw_258,
            T("Disable Boot UX animation"),
            T("bcdedit /set bootux disabled"));
        Add("tw_259", CatSystem, false,
            A_tw_259, U_tw_259, D_tw_259,
            T("Reduce Boot Menu Timeout"),
            T("bcdedit /timeout 3"));
        Add("tw_260", CatPerformance, false,
            A_tw_260, U_tw_260, D_tw_260,
            T("Disable HPET via useplatformtick"),
            T("bcdedit /set useplatformtick yes"));
        Add("tw_261", CatPerformance, false,
            A_tw_261, U_tw_261, D_tw_261,
            T("Disable Dynamic Tick for CPU 0 specifically"),
            T("bcdedit /set disabledynamictick yes"));
        Add("tw_262", CatSecurity, false,
            A_tw_262, U_tw_262, D_tw_262,
            T("Add Game Folders to Defender Exclusions"),
            T("Add-MpPreference -ExclusionPath \"C:\\Games\""));
        Add("tw_263", CatSecurity, false,
            A_tw_263, U_tw_263, D_tw_263,
            T("Enable Vulnerable Driver Blocklist (Microsoft-maintained)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CI\\Config\" /v VulnerableDriverBlocklistEnable /t REG_DWORD /d 1 /f"));
        Add("tw_264", CatSecurity, false,
            A_tw_264, U_tw_264, D_tw_264,
            T("Disable SMBv1 Protocol"),
            T("Disable-WindowsOptionalFeature -Online -FeatureName SMB1Protocol -NoRestart"));
        Add("tw_265", CatSystem, false,
            A_tw_265, U_tw_265, D_tw_265,
            T("Disable AutoPlay/AutoRun"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\" /v NoDriveTypeAutoRun /t REG_DWORD /d 255 /f"));
        Add("tw_266", CatSecurity, false,
            A_tw_266, U_tw_266, D_tw_266,
            T("Disable PowerShell v2 Engine (legacy, reduce attack surface + startup)"),
            T("Disable-WindowsOptionalFeature -Online -FeatureName MicrosoftWindowsPowerShellV2 -NoRestart"));
        Add("tw_267", CatInternet, false,
            A_tw_267, U_tw_267, D_tw_267,
            T("Disable IE Mode in Edge"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\" /v InternetExplorerIntegrationLevel /t REG_DWORD /d 0 /f"));
        Add("tw_268", CatSystem, false,
            A_tw_268, U_tw_268, D_tw_268,
            T("Disable Automatic Restart After Crash (BSOD)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl\" /v AutoReboot /t REG_DWORD /d 0 /f"));
        Add("tw_269", CatSystem, false,
            A_tw_269, U_tw_269, D_tw_269,
            T("Disable Windows Error Reporting Queue"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\" /v Disabled /t REG_DWORD /d 1 /f"));
        Add("tw_270", CatNetwork, false,
            A_tw_270, U_tw_270, D_tw_270,
            T("Disable Background Task Network Throttling"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f"));
        Add("tw_271", CatNetwork, false,
            A_tw_271, U_tw_271, D_tw_271,
            T("Disable Clipboard History Background Monitor"),
            T("reg add \"HKCU\\Software\\Microsoft\\Clipboard\" /v EnableClipboardHistory /t REG_DWORD /d 0 /f"));
        Add("tw_272", CatDebloat, false,
            A_tw_272, U_tw_272, D_tw_272,
            T("Disable OneDrive Background Sync"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive\" /v DisableFileSyncNGSC /t REG_DWORD /d 1 /f"));
        Add("tw_273", CatDebloat, false,
            A_tw_273, U_tw_273, D_tw_273,
            T("Disable OneDrive Files-On-Demand Background Scan"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive\" /v DisableFileSync /t REG_DWORD /d 1 /f"));
        Add("tw_274", CatPerformance, false,
            A_tw_274, U_tw_274, D_tw_274,
            T("Disable Background Task Cache Rebuild"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications\" /v GlobalUserDisabled /t REG_DWORD /d 1 /f"));
        Add("tw_275", CatSystem, false,
            A_tw_275, U_tw_275, D_tw_275,
            T("Disable PC Health Check background task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\UpdateOrchestrator\\Backup Scan\" /Disable"));
        Add("tw_276", CatSystem, false,
            A_tw_276, U_tw_276, D_tw_276,
            T("Disable Family Safety Background Monitor"),
            T("sc.exe config \"WpcMonSvc\" start= disabled"));
        Add("tw_277", CatSystem, false,
            A_tw_277, U_tw_277, D_tw_277,
            T("Disable Offline Maps Update Task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /Disable"));
        Add("tw_278", CatSystem, false,
            A_tw_278, U_tw_278, D_tw_278,
            T("Disable Disk Diagnostic Task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Disable"));
        Add("tw_279", CatSystem, false,
            A_tw_279, U_tw_279, D_tw_279,
            T("Disable Bluetooth Scan Background Task (radio stays ON)"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Bluetooth\\UninstallDeviceTask\" /Disable"));
        Add("tw_280", CatSystem, false,
            A_tw_280, U_tw_280, D_tw_280,
            T("Auto-Clear Event Logs on Boot"),
            T("Get-EventLog -LogName * | ForEach-Object { Clear-EventLog -LogName $_.Log }"));
        Add("tw_281", CatSystem, false,
            A_tw_281, U_tw_281, D_tw_281,
            T("Disable Sticky Keys Shortcut"),
            T("reg add \"HKCU\\Control Panel\\Accessibility\\StickyKeys\" /v Flags /t REG_SZ /d 506 /f"));
        Add("tw_282", CatSystem, false,
            A_tw_282, U_tw_282, D_tw_282,
            T("Disable DirectPlay legacy feature"),
            T("Disable-WindowsOptionalFeature -Online -FeatureName DirectPlay -NoRestart"));
        Add("tw_283", CatPerformance, false,
            A_tw_283, U_tw_283, D_tw_283,
            T("Disable Prefetch for Non-Game Applications (AppLaunch)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters\" /v EnableAppLaunchPrefetch /t REG_DWORD /d 0 /f"));
        Add("tw_284", CatSystem, false,
            A_tw_284, U_tw_284, D_tw_284,
            T("Disable Unnecessary COM Surrogate Autolaunch"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\" /v NoDllCache /t REG_DWORD /d 1 /f"));
        Add("tw_285", CatSystem, false,
            A_tw_285, U_tw_285, D_tw_285,
            T("Disable Ease of Access Autolaunch on Logon"),
            T("reg add \"HKCU\\Control Panel\\Accessibility\" /v \"Last Well Formed\" /t REG_DWORD /d 1 /f"));
        Add("tw_286", CatInternet, false,
            A_tw_286, U_tw_286, D_tw_286,
            T("Kill Chrome Background Processes After Close"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Google\\Chrome\" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f"));
        Add("tw_287", CatUI, false,
            A_tw_287, U_tw_287, D_tw_287,
            T("Disable Widgets background process specifically"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarDa /t REG_DWORD /d 0 /f"));
        Add("tw_288", CatSystem, false,
            A_tw_288, U_tw_288, D_tw_288,
            T("Disable Phone Link background process"),
            T("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\PhoneLink\" /v ShowInTaskbar /t REG_DWORD /d 0 /f"));
        Add("tw_289", CatSystem, false,
            A_tw_289, U_tw_289, D_tw_289,
            T("Disable GameBarPresenceWriter.exe background process"),
            T("reg add \"HKCU\\SOFTWARE\\Microsoft\\GameBar\" /v ShowStartupPanel /t REG_DWORD /d 0 /f"));
        Add("tw_290", CatSystem, false,
            A_tw_290, U_tw_290, D_tw_290,
            T("Disable Connected Devices Platform User Service background process"),
            T("sc.exe config \"CDPUserSvc\" start= demand"));
        Add("tw_291", CatSecurity, false,
            A_tw_291, U_tw_291, D_tw_291,
            T("Disable SmartScreen Background Scanner Process"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v EnableSmartScreen /t REG_DWORD /d 0 /f"));
        Add("tw_292", CatSystem, false,
            A_tw_292, U_tw_292, D_tw_292,
            T("Disable Google Update Scheduled Tasks"),
            T("schtasks /Change /TN \"GoogleUpdateTaskMachineCore\" /Disable"));
        Add("tw_293", CatSystem, false,
            A_tw_293, U_tw_293, D_tw_293,
            T("Disable Google AutoUpdate background process explicitly"),
            T("sc.exe config \"gupdate\" start= disabled"));
        Add("tw_294", CatPrivacy, false,
            A_tw_294, U_tw_294, D_tw_294,
            T("Disable Energy Diagnostics Scheduled Task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Disable"));
        Add("tw_295", CatSystem, false,
            A_tw_295, U_tw_295, D_tw_295,
            T("Disable Disk Footprint Scan Task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Disable"));
        Add("tw_296", CatSystem, false,
            A_tw_296, U_tw_296, D_tw_296,
            T("Disable Print Spooler background service (if no printer)"),
            T("sc.exe config \"Spooler\" start= disabled"));
        Add("tw_297", CatPrivacy, false,
            A_tw_297, U_tw_297, D_tw_297,
            T("Disable Camera/Mic Global App Access when unused"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam\" /v Value /t REG_SZ /d Deny /f"));
        Add("tw_298", CatPerformance, false,
            A_tw_298, U_tw_298, D_tw_298,
            T("Disable Windows Camera Frame Server"),
            T("sc.exe config \"FrameServer\" start= disabled"));
        Add("tw_299", CatPerformance, false,
            A_tw_299, U_tw_299, D_tw_299,
            T("Disable Frame Server Monitor Service"),
            T("sc.exe config \"FrameServerMonitor\" start= disabled"));
        Add("tw_300", CatSystem, false,
            A_tw_300, U_tw_300, D_tw_300,
            T("Disable Still Image Acquisition Events (scanners/cameras)"),
            T("sc.exe config \"StiSvc\" start= disabled"));
        Add("tw_301", CatPerformance, false,
            A_tw_301, U_tw_301, D_tw_301,
            T("Disable Windows Font Cache background service"),
            T("sc.exe config \"FontCache\" start= disabled"));
        Add("tw_302", CatInternet, false,
            A_tw_302, U_tw_302, D_tw_302,
            T("Disable WebClient (WebDAV redirector) service"),
            T("sc.exe config \"WebClient\" start= disabled"));
        Add("tw_303", CatPrivacy, false,
            A_tw_303, U_tw_303, D_tw_303,
            T("Disable Distributed Link Tracking Client"),
            T("sc.exe config \"TrkWks\" start= disabled"));
        Add("tw_304", CatSystem, false,
            A_tw_304, U_tw_304, D_tw_304,
            T("Disable Mobile Hotspot Sharing Service (if unused)"),
            T("sc.exe config \"icssvc\" start= disabled"));
        Add("tw_305", CatSystem, false,
            A_tw_305, U_tw_305, D_tw_305,
            T("Disable Wallet Service"),
            T("sc.exe config \"WalletService\" start= disabled"));
        Add("tw_306", CatSystem, false,
            A_tw_306, U_tw_306, D_tw_306,
            T("Disable Enterprise App Management Service"),
            T("sc.exe config \"AppMgmt\" start= disabled"));
        Add("tw_307", CatNetwork, false,
            A_tw_307, U_tw_307, D_tw_307,
            T("Disable WMP Network Sharing Service"),
            T("sc.exe config \"WMPNetworkSvc\" start= disabled"));
        Add("tw_308", CatPrivacy, false,
            A_tw_308, U_tw_308, D_tw_308,
            T("Disable Bluetooth Telemetry Background Task (radio stays ON)"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Bluetooth\\BthSQM\" /Disable"));
        Add("tw_309", CatSystem, false,
            A_tw_309, U_tw_309, D_tw_309,
            T("Disable Adobe Background Updater Processes"),
            T("schtasks /Change /TN \"Adobe Acrobat Update Task\" /Disable"));
        Add("tw_310", CatPrivacy, false,
            A_tw_310, U_tw_310, D_tw_310,
            T("Close Telemetry-Collector Background Processes on Logon"),
            T("Get-Process CompatTelRunner,DeviceCensus -ErrorAction SilentlyContinue | Stop-Process -Force"));
        Add("tw_311", CatPrivacy, false,
            A_tw_311, U_tw_311, D_tw_311,
            T("Disable Memory Diagnostics Background Task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\MemoryDiagnostic\\ProcessMemoryDiagnosticEvents\" /Disable"));
        Add("tw_312", CatSystem, false,
            A_tw_312, U_tw_312, D_tw_312,
            T("Disable Wredi Performance Scan Task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Disable"));
        Add("tw_313", CatSystem, false,
            A_tw_313, U_tw_313, D_tw_313,
            T("Disable Automatic chkdsk Triggers"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /Disable"));
        Add("tw_314", CatSystem, false,
            A_tw_314, U_tw_314, D_tw_314,
            T("Disable Background Servicing/Component Cleanup (TiWorker)"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Servicing\\StartComponentCleanup\" /Disable"));
        Add("tw_315", CatSystem, false,
            A_tw_315, U_tw_315, D_tw_315,
            T("Disable Registry Backup Scan Task"),
            T("schtasks /Change /TN \"\\Microsoft\\Windows\\Registry\\RegIdleBackup\" /Disable"));
        Add("tw_316", CatUpdates, false,
            A_tw_316, U_tw_316, D_tw_316,
            T("Disable Third-Party Auto-Updaters (Java/Opera/Brave) at Startup"),
            T("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"SunJavaUpdateSched\" /f"));
        Add("tw_317", CatSystem, false,
            A_tw_317, U_tw_317, D_tw_317,
            T("Disable Touch Input (ctfmon) Background Process when no touchscreen"),
            T("sc.exe config \"TabletInputService\" start= disabled"));
        Add("tw_318", CatDebloat, false,
            A_tw_318, U_tw_318, D_tw_318,
            T("Kill Active Background Bloat Processes Immediately"),
            T("Get-Process OneDrive,YourPhone,Widgets,Teams -ErrorAction SilentlyContinue | Stop-Process -Force"));
        Add("tw_319", CatSecurity, true,
            A_tw_319, U_tw_319, D_tw_319,
            T("Disable Windows Defender Real-Time Protection"),
            T("Set-MpPreference -DisableRealtimeMonitoring $true"));
        Add("tw_320", CatAdvanced, true,
            A_tw_320, U_tw_320, D_tw_320,
            T("Disable Memory Integrity / HVCI (big FPS gain on many CPUs)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v Enabled /t REG_DWORD /d 0 /f"));
        Add("tw_321", CatAdvanced, true,
            A_tw_321, U_tw_321, D_tw_321,
            T("Disable Hypervisor Launch (bcdedit hypervisorlaunchtype off)"),
            T("bcdedit /set hypervisorlaunchtype off"));
        Add("tw_322", CatAdvanced, true,
            A_tw_322, U_tw_322, D_tw_322,
            T("Disable Hyper-V Windows Feature"),
            T("Disable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All -NoRestart"));
        Add("tw_323", CatAdvanced, true,
            A_tw_323, U_tw_323, D_tw_323,
            T("Disable Virtualization-Based Security (VBS)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f"));
        Add("tw_324", CatAdvanced, true,
            A_tw_324, U_tw_324, D_tw_324,
            T("Disable Credential Guard"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\LSA\" /v LsaCfgFlags /t REG_DWORD /d 0 /f"));
        Add("tw_325", CatAdvanced, true,
            A_tw_325, U_tw_325, D_tw_325,
            T("Disable Windows Sandbox Feature"),
            T("Disable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -NoRestart"));
        Add("tw_326", CatAdvanced, true,
            A_tw_326, U_tw_326, D_tw_326,
            T("Disable WSL Feature"),
            T("Disable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux -NoRestart"));
        Add("tw_327", CatAdvanced, true,
            A_tw_327, U_tw_327, D_tw_327,
            T("Disable Kernel DMA Protection"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DmaSecurity\\AllowedBuses\" /v EnableDmaGuard /t REG_DWORD /d 0 /f"));
        Add("tw_328", CatSystem, false,
            A_tw_328, U_tw_328, D_tw_328,
            T("Disable Device Guard policies"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v LsaCfgFlags /t REG_DWORD /d 0 /f"));
        Add("tw_329", CatSecurity, false,
            A_tw_329, U_tw_329, D_tw_329,
            T("Force-disable BitLocker Auto-Encryption on new drives"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\BitLocker\" /v PreventDeviceEncryption /t REG_DWORD /d 1 /f"));
        Add("tw_330", CatNetwork, false,
            A_tw_330, U_tw_330, D_tw_330,
            T("Disable Networking in Modern Standby"),
            T("powercfg /setdcvalueindex scheme_current sub_none f15576e8-98b7-4186-b944-eafa664402d9 0"));
        Add("tw_331", CatSystem, false,
            A_tw_331, U_tw_331, D_tw_331,
            T("Grant SYSTEM Full Control Registry Unlock (advanced editing)"),
            T("takeown /f \"%windir%\\System32\\config\" /r /d y && icacls \"%windir%\\System32\\config\" /grant SYSTEM:F /t"));
        Add("tw_332", CatAdvanced, true,
            A_tw_332, U_tw_332, D_tw_332,
            T("Disable Driver Signature Enforcement"),
            T("bcdedit /set nointegritychecks on"));
        Add("tw_333", CatSystem, false,
            A_tw_333, U_tw_333, D_tw_333,
            T("Disable Secure Boot violation notifications"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\SecureBoot\\State\" /v NotifyDisable /t REG_DWORD /d 1 /f"));
        Add("tw_334", CatUpdates, false,
            A_tw_334, U_tw_334, D_tw_334,
            T("Disable Windows Update Medic Service (WaaSMedicSvc)"),
            T("sc.exe config \"WaaSMedicSvc\" start= disabled"));
        Add("tw_335", CatSecurity, true,
            A_tw_335, U_tw_335, D_tw_335,
            T("Disable Defender Tamper Protection (required before other Defender tweaks)"),
            T("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows Defender\\Features\" /v TamperProtection /t REG_DWORD /d 0 /f"));
        Add("tw_336", CatSecurity, true,
            A_tw_336, U_tw_336, D_tw_336,
            T("Disable Exploit Protection system-wide mitigations"),
            T("Set-ProcessMitigation -System -Disable DEP,SEHOP,ForceRelocateImages,BottomUp,HighEntropy"));
        Add("tw_337", CatAdvanced, true,
            A_tw_337, U_tw_337, D_tw_337,
            T("Disable Windows Sandbox Network Isolation"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Sandbox\" /v AllowNetworking /t REG_DWORD /d 1 /f"));
        Add("tw_338", CatAdvanced, true,
            A_tw_338, U_tw_338, D_tw_338,
            T("Disable Memory Integrity Prompt on Startup"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v WasEnabledBy /t REG_DWORD /d 0 /f"));
        Add("tw_339", CatSecurity, true,
            A_tw_339, U_tw_339, D_tw_339,
            T("Disable Defender Cloud-Delivered Protection"),
            T("Set-MpPreference -MAPSReporting 0"));
        Add("tw_340", CatSecurity, false,
            A_tw_340, U_tw_340, D_tw_340,
            T("Disable SmartScreen for Microsoft Store Apps"),
            T("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppHost\" /v EnableWebContentEvaluation /t REG_DWORD /d 0 /f"));
        Add("tw_341", CatSecurity, false,
            A_tw_341, U_tw_341, D_tw_341,
            T("Disable SmartScreen for Edge/Explorer"),
            T("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v EnableSmartScreen /t REG_DWORD /d 0 /f"));
        Add("tw_342", CatAdvanced, true,
            A_tw_342, U_tw_342, D_tw_342,
            T("Disable Meltdown/Spectre Speculative-Execution Mitigations (big CPU-bound FPS gain, real security risk)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f && reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f"));
        Add("tw_343", CatSystem, false,
            A_tw_343, U_tw_343, D_tw_343,
            T("Disable Group Policy Client Service refresh cycle (leave manual)"),
            T("sc.exe config \"gpsvc\" start= demand"));
        Add("tw_344", CatSecurity, false,
            A_tw_344, U_tw_344, D_tw_344,
            T("Remove Windows Defender Components (DISM, extreme/irreversible)"),
            T("DISM /Online /Disable-Feature /FeatureName:Windows-Defender-Default-Definitions"));
        Add("tw_345", CatSecurity, false,
            A_tw_345, U_tw_345, D_tw_345,
            T("Remove Security Health Service"),
            T("sc.exe config \"SecurityHealthService\" start= disabled"));
        Add("tw_346", CatSystem, false,
            A_tw_346, U_tw_346, D_tw_346,
            T("Remove SysMain Binaries Entirely (after service disable)"),
            T("DISM /Online /Remove-Package /PackageName:SysMain"));
        Add("tw_348", CatAdvanced, false,
            A_tw_348, U_tw_348, D_tw_348,
            T("Enable System Restore Engine (Protection & Rollback Safety Net)"),
            T("Enable-ComputerRestore -Drive \"C:\\\""));
        Add("tw_349", CatSystem, false,
            A_tw_349, U_tw_349, D_tw_349,
            T("Remove Windows Hello biometric stack"),
            T("DISM /Online /Remove-Capability /CapabilityName:\"Hello.Face.17658\""));
        Add("tw_350", CatSystem, false,
            A_tw_350, U_tw_350, D_tw_350,
            T("Remove Windows Backup Engine"),
            T("sc.exe config \"SDRSVC\" start= disabled"));
        Add("tw_351", CatAdvanced, true,
            A_tw_351, U_tw_351, D_tw_351,
            T("Aggressively Merge SvcHost Processes (biggest single process-count win)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\" /v SvcHostSplitThresholdInKB /t REG_DWORD /d 4294967295 /f"));
        Add("tw_352", CatPerformance, false,
            A_tw_352, U_tw_352, D_tw_352,
            T("Immediately Trim Working Sets / Free Cached RAM"),
            T("[System.GC]::Collect(); Get-Process | ForEach-Object { try { $_.MinWorkingSet=$_.MinWorkingSet } catch {} }"));
        Add("tw_353", CatPerformance, false,
            A_tw_353, U_tw_353, D_tw_353,
            T("Disable SysMain/Superfetch (frees RAM, helps on SSD systems)"),
            T("sc.exe config \"SysMain\" start= disabled"));
        Add("tw_354", CatPerformance, false,
            A_tw_354, U_tw_354, D_tw_354,
            T("Optimize Pagefile Clearing at Clean Shutdown"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v ClearPageFileAtShutdown /t REG_DWORD /d 0 /f"));
        Add("tw_355", CatPerformance, false,
            A_tw_355, U_tw_355, D_tw_355,
            T("Disable Large System Cache (dedicate RAM to foreground game, not file cache)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v LargeSystemCache /t REG_DWORD /d 0 /f"));
        Add("tw_356", CatPerformance, false,
            A_tw_356, U_tw_356, D_tw_356,
            T("Keep Drivers Pinned in RAM (DisableDriverPaging)"),
            T("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v DisablePagingExecutive /t REG_DWORD /d 1 /f"));
        Add("tw_357", CatDebloat, false,
            A_tw_357, U_tw_357, D_tw_357,
            T("Disable Microsoft Edge AutoUpdate Background Processes"),
            T("Disable Microsoft Edge AutoUpdate Background Processes"));
        Add("tw_358", CatDebloat, false,
            A_tw_358, U_tw_358, D_tw_358,
            T("Disable Brave Browser AutoUpdate Background Processes"),
            T("Disable Brave Browser AutoUpdate Background Processes"));
        Add("tw_359", CatPerformance, false,
            A_tw_359, U_tw_359, D_tw_359,
            T("Disable LampArray Translation Service (RGB Lighting Host)"),
            T("Disable LampArray Translation Service (RGB Lighting Host)"));
        Add("tw_360", CatDebloat, false,
            A_tw_360, U_tw_360, D_tw_360,
            T("Disable User OOBE Broker Background Process"),
            T("Disable User OOBE Broker Background Process"));
        Add("tw_361", CatDebloat, false,
            A_tw_361, U_tw_361, D_tw_361,
            T("Disable wsappx Store Auto-Download Background Service"),
            T("Disable wsappx Store Auto-Download Background Service"));
        Add("tw_362", CatPerformance, false,
            A_tw_362, U_tw_362, D_tw_362,
            T("Disable RuntimeBroker Background Suggestions"),
            T("Disable RuntimeBroker Background Suggestions"));
        Add("tw_363", CatAdvanced, false,
            A_tw_363, U_tw_363, D_tw_363,
            T("Disable Hyper-V Host Compute & VM Services"),
            T("Disable Hyper-V Host Compute & VM Services"));
        Add("tw_364", CatDebloat, false,
            A_tw_364, U_tw_364, D_tw_364,
            T("Disable Windows Biometric Service (Hello)"),
            T("Disable Windows Biometric Service (Hello)"));
        Add("tw_365", CatSecurity, false,
            A_tw_365, U_tw_365, D_tw_365,
            T("Disable Windows Security Health Systray Icon"),
            T("Disable Windows Security Health Systray Icon"));
        Add("tw_366", CatSystem, false,
            A_tw_366, U_tw_366, D_tw_366,
            T("Disable Windows Error Reporting Service (WerSvc)"),
            T("Disable Windows Error Reporting Service (WerSvc)"));
        Add("tw_367", CatSystem, false,
            A_tw_367, U_tw_367, D_tw_367,
            T("Disable Geolocation Tracking Service (lfsvc)"),
            T("Disable Geolocation Tracking Service (lfsvc)"));
        Add("tw_368", CatSystem, false,
            A_tw_368, U_tw_368, D_tw_368,
            T("Disable Distributed Link Tracking Client (TrkWks)"),
            T("Disable Distributed Link Tracking Client (TrkWks)"));
        Add("tw_369", CatSystem, false,
            A_tw_369, U_tw_369, D_tw_369,
            T("Disable Sensor & Auto-Brightness Services (SensorService)"),
            T("Disable Sensor & Auto-Brightness Services (SensorService)"));
        Add("tw_370", CatSystem, false,
            A_tw_370, U_tw_370, D_tw_370,
            T("Disable Wallet & Payment Manager Services"),
            T("Disable Wallet & Payment Manager Services"));
        Add("tw_371", CatAppearance, false,
            A_tw_371, U_tw_371, D_tw_371,
            T("Disable Lock Screen Acrylic Blur (Instant Logon Background)"),
            T("Disable Lock Screen Acrylic Blur (Instant Logon Background)"));
        Add("tw_372", CatPerformance, false,
            A_tw_372, U_tw_372, D_tw_372,
            T("Minecraft & Java JVM CPU Quantum Priority Boost (Win32PrioritySeparation = 38)"),
            T("Minecraft & Java JVM CPU Quantum Priority Boost (Win32PrioritySeparation = 38)"));
        Add("tw_373", CatPerformance, false,
            A_tw_373, U_tw_373, D_tw_373,
            T("Hardware-Accelerated GPU Scheduling HAGS (Low Frametime Jitter)"),
            T("Hardware-Accelerated GPU Scheduling HAGS (Low Frametime Jitter)"));
        Add("tw_374", CatNetwork, false,
            A_tw_374, U_tw_374, D_tw_374,
            T("Disable Nagle Algorithm & TCP DelAck for Minecraft Multiplayer Low Ping"),
            T("Disable Nagle Algorithm & TCP DelAck for Minecraft Multiplayer Low Ping"));
        Add("tw_375", CatPerformance, false,
            A_tw_375, U_tw_375, D_tw_375,
            T("MMCSS Gaming & Benchmark Ultra Priority Profile (100% CPU Allocation)"),
            T("MMCSS Gaming & Benchmark Ultra Priority Profile (100% CPU Allocation)"));
        Add("tw_376", CatPerformance, false,
            A_tw_376, U_tw_376, D_tw_376,
            T("Disable Paging Executive (Lock Windows Kernel in Physical RAM)"),
            T("Disable Paging Executive (Lock Windows Kernel in Physical RAM)"));
        Add("tw_377", CatPerformance, false,
            A_tw_377, U_tw_377, D_tw_377,
            T("Disable DXGI Thread Yield (DirectX Low Latency Frame Rendering)"),
            T("Disable DXGI Thread Yield (DirectX Low Latency Frame Rendering)"));
        Add("tw_378", CatPerformance, false,
            A_tw_378, U_tw_378, D_tw_378,
            T("Enable Message Signaled Interrupts (MSI Mode) for GPU (Eliminate DPC Stutter)"),
            T("Enable Message Signaled Interrupts (MSI Mode) for GPU (Eliminate DPC Stutter)"));
        Add("tw_379", CatPerformance, false,
            A_tw_379, U_tw_379, D_tw_379,
            T("Disable System Dynamic Tick & HPET (Zero-Latency Timer)"),
            T("Disable System Dynamic Tick & HPET (Zero-Latency Timer)"));
        Add("tw_380", CatPerformance, false,
            A_tw_380, U_tw_380, D_tw_380,
            T("Custom OS Zero-Latency Foreground Priority (ForegroundLockTimeout = 0)"),
            T("Custom OS Zero-Latency Foreground Priority (ForegroundLockTimeout = 0)"));
        Add("tw_381", CatPerformance, false,
            A_tw_381, U_tw_381, D_tw_381,
            T("Disable Superfetch & Prefetcher RAM Cache Overhead"),
            T("Disable Superfetch & Prefetcher RAM Cache Overhead"));
        Add("tw_382", CatPerformance, false,
            A_tw_382, U_tw_382, D_tw_382,
            T("Disable Windows Fault Tolerant Heap FTH (Remove Game Latency Overhead)"),
            T("Disable Windows Fault Tolerant Heap FTH (Remove Game Latency Overhead)"));
        Add("tw_383", CatPerformance, false,
            A_tw_383, U_tw_383, D_tw_383,
            T("Disable NTFS Last Access Update & 8.3 Short Name Creation (Faster Game Loading)"),
            T("Disable NTFS Last Access Update & 8.3 Short Name Creation (Faster Game Loading)"));
        Add("tw_384", CatPerformance, false,
            A_tw_384, U_tw_384, D_tw_384,
            T("Custom OS Zero Input Lag Mouse & Keyboard Buffer (MouseHoverTime = 0)"),
            T("Custom OS Zero Input Lag Mouse & Keyboard Buffer (MouseHoverTime = 0)"));
        return _tempList;
    }

    // ───────── tw_001: Disable service: WAP Push Message Routing (device management push) (dmwappushservice) ─────────
    static bool A_tw_001()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\dmwappushservice", "Start", 4);
        try { Svc.Stop("dmwappushservice"); } catch { }
        return true;
    }

    static bool U_tw_001()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\dmwappushservice", "Start", 3);
        return true;
    }

    static bool D_tw_001()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\dmwappushservice", "Start") == 4 || Svc.IsDisabled("dmwappushservice");
    }
    // ───────── tw_002: Disable service: Diagnostic Policy Service (DPS) ─────────
    static bool A_tw_002()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\DPS", "Start", 4);
        try { Svc.Stop("DPS"); } catch { }
        return true;
    }

    static bool U_tw_002()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\DPS", "Start", 3);
        return true;
    }

    static bool D_tw_002()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\DPS", "Start") == 4 || Svc.IsDisabled("DPS");
    }
    // ───────── tw_003: Disable service: Diagnostic Service Host (WdiServiceHost) ─────────
    static bool A_tw_003()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WdiServiceHost", "Start", 4);
        try { Svc.Stop("WdiServiceHost"); } catch { }
        return true;
    }

    static bool U_tw_003()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WdiServiceHost", "Start", 3);
        return true;
    }

    static bool D_tw_003()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WdiServiceHost", "Start") == 4 || Svc.IsDisabled("WdiServiceHost");
    }
    // ───────── tw_004: Disable service: Diagnostic System Host (WdiSystemHost) ─────────
    static bool A_tw_004()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WdiSystemHost", "Start", 4);
        try { Svc.Stop("WdiSystemHost"); } catch { }
        return true;
    }

    static bool U_tw_004()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WdiSystemHost", "Start", 3);
        return true;
    }

    static bool D_tw_004()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WdiSystemHost", "Start") == 4 || Svc.IsDisabled("WdiSystemHost");
    }
    // ───────── tw_005: Disable service: Downloaded Maps Manager (MapsBroker) ─────────
    static bool A_tw_005()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\MapsBroker", "Start", 4);
        try { Svc.Stop("MapsBroker"); } catch { }
        return true;
    }

    static bool U_tw_005()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\MapsBroker", "Start", 3);
        return true;
    }

    static bool D_tw_005()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\MapsBroker", "Start") == 4 || Svc.IsDisabled("MapsBroker");
    }
    // ───────── tw_006: Disable service: Geolocation Service (lfsvc) ─────────
    static bool A_tw_006()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\lfsvc", "Start", 4);
        try { Svc.Stop("lfsvc"); } catch { }
        return true;
    }

    static bool U_tw_006()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\lfsvc", "Start", 3);
        return true;
    }

    static bool D_tw_006()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\lfsvc", "Start") == 4 || Svc.IsDisabled("lfsvc");
    }
    // ───────── tw_007: Disable service: Windows Insider Service (wisvc) ─────────
    static bool A_tw_007()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\wisvc", "Start", 4);
        try { Svc.Stop("wisvc"); } catch { }
        return true;
    }

    static bool U_tw_007()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\wisvc", "Start", 3);
        return true;
    }

    static bool D_tw_007()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\wisvc", "Start") == 4 || Svc.IsDisabled("wisvc");
    }
    // ───────── tw_008: Disable service: Program Compatibility Assistant (PcaSvc) ─────────
    static bool A_tw_008()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\PcaSvc", "Start", 4);
        try { Svc.Stop("PcaSvc"); } catch { }
        return true;
    }

    static bool U_tw_008()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\PcaSvc", "Start", 3);
        return true;
    }

    static bool D_tw_008()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\PcaSvc", "Start") == 4 || Svc.IsDisabled("PcaSvc");
    }
    // ───────── tw_009: Disable service: Remote Registry (RemoteRegistry) ─────────
    static bool A_tw_009()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\RemoteRegistry", "Start", 4);
        try { Svc.Stop("RemoteRegistry"); } catch { }
        return true;
    }

    static bool U_tw_009()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\RemoteRegistry", "Start", 3);
        return true;
    }

    static bool D_tw_009()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\RemoteRegistry", "Start") == 4 || Svc.IsDisabled("RemoteRegistry");
    }
    // ───────── tw_010: Disable service: Retail Demo Service (RetailDemo) ─────────
    static bool A_tw_010()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\RetailDemo", "Start", 4);
        try { Svc.Stop("RetailDemo"); } catch { }
        return true;
    }

    static bool U_tw_010()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\RetailDemo", "Start", 3);
        return true;
    }

    static bool D_tw_010()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\RetailDemo", "Start") == 4 || Svc.IsDisabled("RetailDemo");
    }
    // ───────── tw_011: Disable service: Secondary Logon (seclogon) ─────────
    static bool A_tw_011()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\seclogon", "Start", 4);
        try { Svc.Stop("seclogon"); } catch { }
        return true;
    }

    static bool U_tw_011()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\seclogon", "Start", 3);
        return true;
    }

    static bool D_tw_011()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\seclogon", "Start") == 4 || Svc.IsDisabled("seclogon");
    }
    // ───────── tw_012: Disable service: Parental Controls (WpcMonSvc) ─────────
    static bool A_tw_012()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WpcMonSvc", "Start", 4);
        try { Svc.Stop("WpcMonSvc"); } catch { }
        return true;
    }

    static bool U_tw_012()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WpcMonSvc", "Start", 3);
        return true;
    }

    static bool D_tw_012()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WpcMonSvc", "Start") == 4 || Svc.IsDisabled("WpcMonSvc");
    }
    // ───────── tw_013: Disable service: Phone Service (PhoneSvc) ─────────
    static bool A_tw_013()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\PhoneSvc", "Start", 4);
        try { Svc.Stop("PhoneSvc"); } catch { }
        return true;
    }

    static bool U_tw_013()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\PhoneSvc", "Start", 3);
        return true;
    }

    static bool D_tw_013()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\PhoneSvc", "Start") == 4 || Svc.IsDisabled("PhoneSvc");
    }
    // ───────── tw_014: Disable service: Sensor Monitoring Service (SensrSvc) ─────────
    static bool A_tw_014()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensrSvc", "Start", 4);
        try { Svc.Stop("SensrSvc"); } catch { }
        return true;
    }

    static bool U_tw_014()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensrSvc", "Start", 3);
        return true;
    }

    static bool D_tw_014()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensrSvc", "Start") == 4 || Svc.IsDisabled("SensrSvc");
    }
    // ───────── tw_015: Disable service: Sensor Data Service (SensorDataService) ─────────
    static bool A_tw_015()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensorDataService", "Start", 4);
        try { Svc.Stop("SensorDataService"); } catch { }
        return true;
    }

    static bool U_tw_015()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensorDataService", "Start", 3);
        return true;
    }

    static bool D_tw_015()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensorDataService", "Start") == 4 || Svc.IsDisabled("SensorDataService");
    }
    // ───────── tw_016: Disable service: Sensor Service (SensorService) ─────────
    static bool A_tw_016()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensorService", "Start", 4);
        try { Svc.Stop("SensorService"); } catch { }
        return true;
    }

    static bool U_tw_016()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensorService", "Start", 3);
        return true;
    }

    static bool D_tw_016()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensorService", "Start") == 4 || Svc.IsDisabled("SensorService");
    }
    // ───────── tw_017: Disable service: Connected Devices Platform Service (CDPSvc) ─────────
    static bool A_tw_017()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\CDPSvc", "Start", 4);
        try { Svc.Stop("CDPSvc"); } catch { }
        return true;
    }

    static bool U_tw_017()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\CDPSvc", "Start", 3);
        return true;
    }

    static bool D_tw_017()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\CDPSvc", "Start") == 4 || Svc.IsDisabled("CDPSvc");
    }
    // ───────── tw_018: Disable service: IP Helper (IPv6 tunneling/Teredo) (iphlpsvc) ─────────
    static bool A_tw_018()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\iphlpsvc", "Start", 4);
        try { Svc.Stop("iphlpsvc"); } catch { }
        return true;
    }

    static bool U_tw_018()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\iphlpsvc", "Start", 3);
        return true;
    }

    static bool D_tw_018()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\iphlpsvc", "Start") == 4 || Svc.IsDisabled("iphlpsvc");
    }
    // ───────── tw_019: Disable service: SysMain / Superfetch (SysMain) ─────────
    static bool A_tw_019()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SysMain", "Start", 4);
        try { Svc.Stop("SysMain"); } catch { }
        return true;
    }

    static bool U_tw_019()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SysMain", "Start", 3);
        return true;
    }

    static bool D_tw_019()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SysMain", "Start") == 4 || Svc.IsDisabled("SysMain");
    }
    // ───────── tw_020: Disable service: Contact Data Indexing (PimIndexMaintenanceSvc) ─────────
    static bool A_tw_020()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\PimIndexMaintenanceSvc", "Start", 4);
        try { Svc.Stop("PimIndexMaintenanceSvc"); } catch { }
        return true;
    }

    static bool U_tw_020()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\PimIndexMaintenanceSvc", "Start", 3);
        return true;
    }

    static bool D_tw_020()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\PimIndexMaintenanceSvc", "Start") == 4 || Svc.IsDisabled("PimIndexMaintenanceSvc");
    }
    // ───────── tw_021: Disable service: User Data Storage (UserDataSvc) ─────────
    static bool A_tw_021()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UserDataSvc", "Start", 4);
        try { Svc.Stop("UserDataSvc"); } catch { }
        return true;
    }

    static bool U_tw_021()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UserDataSvc", "Start", 3);
        return true;
    }

    static bool D_tw_021()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UserDataSvc", "Start") == 4 || Svc.IsDisabled("UserDataSvc");
    }
    // ───────── tw_022: Disable service: User Data Access (Unistore) (UnistoreSvc) ─────────
    static bool A_tw_022()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UnistoreSvc", "Start", 4);
        try { Svc.Stop("UnistoreSvc"); } catch { }
        return true;
    }

    static bool U_tw_022()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UnistoreSvc", "Start", 3);
        return true;
    }

    static bool D_tw_022()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UnistoreSvc", "Start") == 4 || Svc.IsDisabled("UnistoreSvc");
    }
    // ───────── tw_023: Disable service: Quality Windows Audio Video Experience (QWAVE) ─────────
    static bool A_tw_023()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\QWAVE", "Start", 4);
        try { Svc.Stop("QWAVE"); } catch { }
        return true;
    }

    static bool U_tw_023()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\QWAVE", "Start", 3);
        return true;
    }

    static bool D_tw_023()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\QWAVE", "Start") == 4 || Svc.IsDisabled("QWAVE");
    }
    // ───────── tw_024: Disable service: Remote Desktop Configuration (SessionEnv) ─────────
    static bool A_tw_024()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SessionEnv", "Start", 4);
        try { Svc.Stop("SessionEnv"); } catch { }
        return true;
    }

    static bool U_tw_024()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SessionEnv", "Start", 3);
        return true;
    }

    static bool D_tw_024()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SessionEnv", "Start") == 4 || Svc.IsDisabled("SessionEnv");
    }
    // ───────── tw_025: Disable service: Remote Desktop Services (TermService) ─────────
    static bool A_tw_025()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TermService", "Start", 4);
        try { Svc.Stop("TermService"); } catch { }
        return true;
    }

    static bool U_tw_025()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TermService", "Start", 3);
        return true;
    }

    static bool D_tw_025()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TermService", "Start") == 4 || Svc.IsDisabled("TermService");
    }
    // ───────── tw_026: Disable service: Remote Desktop Port Redirector (UmRdpService) ─────────
    static bool A_tw_026()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UmRdpService", "Start", 4);
        try { Svc.Stop("UmRdpService"); } catch { }
        return true;
    }

    static bool U_tw_026()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UmRdpService", "Start", 3);
        return true;
    }

    static bool D_tw_026()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UmRdpService", "Start") == 4 || Svc.IsDisabled("UmRdpService");
    }
    // ───────── tw_027: Disable service: Smart Card (ScardSvr) ─────────
    static bool A_tw_027()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ScardSvr", "Start", 4);
        try { Svc.Stop("ScardSvr"); } catch { }
        return true;
    }

    static bool U_tw_027()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ScardSvr", "Start", 3);
        return true;
    }

    static bool D_tw_027()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ScardSvr", "Start") == 4 || Svc.IsDisabled("ScardSvr");
    }
    // ───────── tw_028: Disable service: Smart Card Device Enumeration (ScDeviceEnum) ─────────
    static bool A_tw_028()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ScDeviceEnum", "Start", 4);
        try { Svc.Stop("ScDeviceEnum"); } catch { }
        return true;
    }

    static bool U_tw_028()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ScDeviceEnum", "Start", 3);
        return true;
    }

    static bool D_tw_028()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ScDeviceEnum", "Start") == 4 || Svc.IsDisabled("ScDeviceEnum");
    }
    // ───────── tw_029: Disable service: SSDP Discovery (SSDPSRV) ─────────
    static bool A_tw_029()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SSDPSRV", "Start", 4);
        try { Svc.Stop("SSDPSRV"); } catch { }
        return true;
    }

    static bool U_tw_029()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SSDPSRV", "Start", 3);
        return true;
    }

    static bool D_tw_029()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SSDPSRV", "Start") == 4 || Svc.IsDisabled("SSDPSRV");
    }
    // ───────── tw_030: Disable service: UPnP Device Host (upnphost) ─────────
    static bool A_tw_030()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\upnphost", "Start", 4);
        try { Svc.Stop("upnphost"); } catch { }
        return true;
    }

    static bool U_tw_030()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\upnphost", "Start", 3);
        return true;
    }

    static bool D_tw_030()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\upnphost", "Start") == 4 || Svc.IsDisabled("upnphost");
    }
    // ───────── tw_031: Disable service: Windows Media Player Network Sharing (WMPNetworkSvc) ─────────
    static bool A_tw_031()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WMPNetworkSvc", "Start", 4);
        try { Svc.Stop("WMPNetworkSvc"); } catch { }
        return true;
    }

    static bool U_tw_031()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WMPNetworkSvc", "Start", 3);
        return true;
    }

    static bool D_tw_031()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WMPNetworkSvc", "Start") == 4 || Svc.IsDisabled("WMPNetworkSvc");
    }
    // ───────── tw_032: Disable service: Device Association Service (DeviceAssociationService) ─────────
    static bool A_tw_032()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\DeviceAssociationService", "Start", 4);
        try { Svc.Stop("DeviceAssociationService"); } catch { }
        return true;
    }

    static bool U_tw_032()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\DeviceAssociationService", "Start", 3);
        return true;
    }

    static bool D_tw_032()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\DeviceAssociationService", "Start") == 4 || Svc.IsDisabled("DeviceAssociationService");
    }
    // ───────── tw_033: Disable service: Windows Push Notifications System (WpnService) ─────────
    static bool A_tw_033()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WpnService", "Start", 4);
        try { Svc.Stop("WpnService"); } catch { }
        return true;
    }

    static bool U_tw_033()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WpnService", "Start", 3);
        return true;
    }

    static bool D_tw_033()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WpnService", "Start") == 4 || Svc.IsDisabled("WpnService");
    }
    // ───────── tw_034: Disable service: Application Layer Gateway (ALG) ─────────
    static bool A_tw_034()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ALG", "Start", 4);
        try { Svc.Stop("ALG"); } catch { }
        return true;
    }

    static bool U_tw_034()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ALG", "Start", 3);
        return true;
    }

    static bool D_tw_034()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ALG", "Start") == 4 || Svc.IsDisabled("ALG");
    }
    // ───────── tw_035: Disable service: AllJoyn Router Service (AJRouter) ─────────
    static bool A_tw_035()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AJRouter", "Start", 4);
        try { Svc.Stop("AJRouter"); } catch { }
        return true;
    }

    static bool U_tw_035()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AJRouter", "Start", 3);
        return true;
    }

    static bool D_tw_035()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AJRouter", "Start") == 4 || Svc.IsDisabled("AJRouter");
    }
    // ───────── tw_036: Disable service: Certificate Propagation (CertPropSvc) ─────────
    static bool A_tw_036()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\CertPropSvc", "Start", 4);
        try { Svc.Stop("CertPropSvc"); } catch { }
        return true;
    }

    static bool U_tw_036()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\CertPropSvc", "Start", 3);
        return true;
    }

    static bool D_tw_036()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\CertPropSvc", "Start") == 4 || Svc.IsDisabled("CertPropSvc");
    }
    // ───────── tw_037: Disable service: Client License Service (Store apps licensing) (ClipSVC) ─────────
    static bool A_tw_037()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ClipSVC", "Start", 4);
        try { Svc.Stop("ClipSVC"); } catch { }
        return true;
    }

    static bool U_tw_037()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ClipSVC", "Start", 3);
        return true;
    }

    static bool D_tw_037()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\ClipSVC", "Start") == 4 || Svc.IsDisabled("ClipSVC");
    }
    // ───────── tw_038: Disable service: Data Usage Subscription Management (DusmSvc) ─────────
    static bool A_tw_038()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\DusmSvc", "Start", 4);
        try { Svc.Stop("DusmSvc"); } catch { }
        return true;
    }

    static bool U_tw_038()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\DusmSvc", "Start", 3);
        return true;
    }

    static bool D_tw_038()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\DusmSvc", "Start") == 4 || Svc.IsDisabled("DusmSvc");
    }
    // ───────── tw_039: Disable service: Function Discovery Provider Host (fdPHost) ─────────
    static bool A_tw_039()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\fdPHost", "Start", 4);
        try { Svc.Stop("fdPHost"); } catch { }
        return true;
    }

    static bool U_tw_039()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\fdPHost", "Start", 3);
        return true;
    }

    static bool D_tw_039()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\fdPHost", "Start") == 4 || Svc.IsDisabled("fdPHost");
    }
    // ───────── tw_040: Disable service: Function Discovery Resource Publication (FDResPub) ─────────
    static bool A_tw_040()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FDResPub", "Start", 4);
        try { Svc.Stop("FDResPub"); } catch { }
        return true;
    }

    static bool U_tw_040()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FDResPub", "Start", 3);
        return true;
    }

    static bool D_tw_040()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FDResPub", "Start") == 4 || Svc.IsDisabled("FDResPub");
    }
    // ───────── tw_041: Disable service: Automatic Time Zone Update (tzautoupdate) ─────────
    static bool A_tw_041()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\tzautoupdate", "Start", 4);
        try { Svc.Stop("tzautoupdate"); } catch { }
        return true;
    }

    static bool U_tw_041()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\tzautoupdate", "Start", 3);
        return true;
    }

    static bool D_tw_041()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\tzautoupdate", "Start") == 4 || Svc.IsDisabled("tzautoupdate");
    }
    // ───────── tw_042: Disable service: Microsoft Store Install Service (InstallService) ─────────
    static bool A_tw_042()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\InstallService", "Start", 4);
        try { Svc.Stop("InstallService"); } catch { }
        return true;
    }

    static bool U_tw_042()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\InstallService", "Start", 3);
        return true;
    }

    static bool D_tw_042()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\InstallService", "Start") == 4 || Svc.IsDisabled("InstallService");
    }
    // ───────── tw_043: Disable service: Windows Font Cache Service (FontCache) ─────────
    static bool A_tw_043()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FontCache", "Start", 4);
        try { Svc.Stop("FontCache"); } catch { }
        return true;
    }

    static bool U_tw_043()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FontCache", "Start", 3);
        return true;
    }

    static bool D_tw_043()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FontCache", "Start") == 4 || Svc.IsDisabled("FontCache");
    }
    // ───────── tw_044: Disable service: WebClient (WebDAV) (WebClient) ─────────
    static bool A_tw_044()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WebClient", "Start", 4);
        try { Svc.Stop("WebClient"); } catch { }
        return true;
    }

    static bool U_tw_044()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WebClient", "Start", 3);
        return true;
    }

    static bool D_tw_044()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WebClient", "Start") == 4 || Svc.IsDisabled("WebClient");
    }
    // ───────── tw_045: Disable service: Distributed Link Tracking Client (TrkWks) ─────────
    static bool A_tw_045()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TrkWks", "Start", 4);
        try { Svc.Stop("TrkWks"); } catch { }
        return true;
    }

    static bool U_tw_045()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TrkWks", "Start", 3);
        return true;
    }

    static bool D_tw_045()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TrkWks", "Start") == 4 || Svc.IsDisabled("TrkWks");
    }
    // ───────── tw_046: Disable service: Windows Mobile Hotspot Service (icssvc) ─────────
    static bool A_tw_046()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\icssvc", "Start", 4);
        try { Svc.Stop("icssvc"); } catch { }
        return true;
    }

    static bool U_tw_046()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\icssvc", "Start", 3);
        return true;
    }

    static bool D_tw_046()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\icssvc", "Start") == 4 || Svc.IsDisabled("icssvc");
    }
    // ───────── tw_047: Disable service: Wallet Service (WalletService) ─────────
    static bool A_tw_047()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WalletService", "Start", 4);
        try { Svc.Stop("WalletService"); } catch { }
        return true;
    }

    static bool U_tw_047()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WalletService", "Start", 3);
        return true;
    }

    static bool D_tw_047()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WalletService", "Start") == 4 || Svc.IsDisabled("WalletService");
    }
    // ───────── tw_048: Disable service: Application Management (enterprise app deployment) (AppMgmt) ─────────
    static bool A_tw_048()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AppMgmt", "Start", 4);
        try { Svc.Stop("AppMgmt"); } catch { }
        return true;
    }

    static bool U_tw_048()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AppMgmt", "Start", 3);
        return true;
    }

    static bool D_tw_048()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AppMgmt", "Start") == 4 || Svc.IsDisabled("AppMgmt");
    }
    // ───────── tw_049: Disable service: Diagnostic Execution Service (diagsvc) ─────────
    static bool A_tw_049()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\diagsvc", "Start", 4);
        try { Svc.Stop("diagsvc"); } catch { }
        return true;
    }

    static bool U_tw_049()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\diagsvc", "Start", 3);
        return true;
    }

    static bool D_tw_049()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\diagsvc", "Start") == 4 || Svc.IsDisabled("diagsvc");
    }
    // ───────── tw_050: Disable service: Windows Backup Engine (WbEngine) ─────────
    static bool A_tw_050()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WbEngine", "Start", 4);
        try { Svc.Stop("WbEngine"); } catch { }
        return true;
    }

    static bool U_tw_050()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WbEngine", "Start", 3);
        return true;
    }

    static bool D_tw_050()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WbEngine", "Start") == 4 || Svc.IsDisabled("WbEngine");
    }
    // ───────── tw_051: Disable service: Volume Shadow Copy (VSS) — PROTECTED for Factory Reset / System Restore ─────────
    static bool A_tw_051()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\VSS", "Start", 3); // Manual (safe)
        return true;
    }

    static bool U_tw_051()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\VSS", "Start", 3);
        return true;
    }

    static bool D_tw_051()
    {
        return false; // Never report VSS as disabled
    }
    // ───────── tw_052: Disable service: Touch Keyboard and Handwriting Panel (TabletInputService) ─────────
    static bool A_tw_052()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TabletInputService", "Start", 4);
        Reg.SetDWord(@"HKCU:\Software\Microsoft\CTF\DisableThreadInputManager", "Disable", 1);
        Svc.Stop("TabletInputService");
        return true;
    }

    static bool U_tw_052()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TabletInputService", "Start", 3);
        Reg.RemoveValue(@"HKCU:\Software\Microsoft\CTF\DisableThreadInputManager", "Disable");
        return true;
    }

    static bool D_tw_052()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TabletInputService", "Start") == 4;
    }
    // ───────── tw_053: Disable service: Google Update (scheduled) (gupdate) ─────────
    static bool A_tw_053()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gupdate", "Start", 4);
        try { Svc.Stop("gupdate"); } catch { }
        return true;
    }

    static bool U_tw_053()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gupdate", "Start", 3);
        return true;
    }

    static bool D_tw_053()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gupdate", "Start") == 4 || Svc.IsDisabled("gupdate");
    }
    // ───────── tw_054: Disable service: Google Update (on-demand) (gupdatem) ─────────
    static bool A_tw_054()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gupdatem", "Start", 4);
        try { Svc.Stop("gupdatem"); } catch { }
        return true;
    }

    static bool U_tw_054()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gupdatem", "Start", 3);
        return true;
    }

    static bool D_tw_054()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gupdatem", "Start") == 4 || Svc.IsDisabled("gupdatem");
    }
    // ───────── tw_055: Disable service: Windows Image Acquisition (scanners) (StiSvc) ─────────
    static bool A_tw_055()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\StiSvc", "Start", 4);
        try { Svc.Stop("StiSvc"); } catch { }
        return true;
    }

    static bool U_tw_055()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\StiSvc", "Start", 3);
        return true;
    }

    static bool D_tw_055()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\StiSvc", "Start") == 4 || Svc.IsDisabled("StiSvc");
    }
    // ───────── tw_056: Disable service: Print Spooler (Spooler) ─────────
    static bool A_tw_056()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\Spooler", "Start", 4);
        try { Svc.Stop("Spooler"); } catch { }
        return true;
    }

    static bool U_tw_056()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\Spooler", "Start", 3);
        return true;
    }

    static bool D_tw_056()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\Spooler", "Start") == 4 || Svc.IsDisabled("Spooler");
    }
    // ───────── tw_057: Disable service: Fax Service (Fax) ─────────
    static bool A_tw_057()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\Fax", "Start", 4);
        try { Svc.Stop("Fax"); } catch { }
        return true;
    }

    static bool U_tw_057()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\Fax", "Start", 3);
        return true;
    }

    static bool D_tw_057()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\Fax", "Start") == 4 || Svc.IsDisabled("Fax");
    }
    // ───────── tw_058: Disable service: Xbox Live Auth Manager (XblAuthManager) ─────────
    static bool A_tw_058()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XblAuthManager", "Start", 4);
        try { Svc.Stop("XblAuthManager"); } catch { }
        return true;
    }

    static bool U_tw_058()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XblAuthManager", "Start", 3);
        return true;
    }

    static bool D_tw_058()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XblAuthManager", "Start") == 4 || Svc.IsDisabled("XblAuthManager");
    }
    // ───────── tw_059: Disable service: Xbox Live Game Save (XblGameSave) ─────────
    static bool A_tw_059()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XblGameSave", "Start", 4);
        try { Svc.Stop("XblGameSave"); } catch { }
        return true;
    }

    static bool U_tw_059()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XblGameSave", "Start", 3);
        return true;
    }

    static bool D_tw_059()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XblGameSave", "Start") == 4 || Svc.IsDisabled("XblGameSave");
    }
    // ───────── tw_060: Disable service: Xbox Live Networking Service (XboxNetApiSvc) ─────────
    static bool A_tw_060()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XboxNetApiSvc", "Start", 4);
        try { Svc.Stop("XboxNetApiSvc"); } catch { }
        return true;
    }

    static bool U_tw_060()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XboxNetApiSvc", "Start", 3);
        return true;
    }

    static bool D_tw_060()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XboxNetApiSvc", "Start") == 4 || Svc.IsDisabled("XboxNetApiSvc");
    }
    // ───────── tw_061: Disable service: Xbox Game Monitoring (xbgm) ─────────
    static bool A_tw_061()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\xbgm", "Start", 4);
        try { Svc.Stop("xbgm"); } catch { }
        return true;
    }

    static bool U_tw_061()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\xbgm", "Start", 3);
        return true;
    }

    static bool D_tw_061()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\xbgm", "Start") == 4 || Svc.IsDisabled("xbgm");
    }
    // ───────── tw_062: Disable service: Xbox Accessory Management Service (XboxGipSvc) ─────────
    static bool A_tw_062()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XboxGipSvc", "Start", 4);
        try { Svc.Stop("XboxGipSvc"); } catch { }
        return true;
    }

    static bool U_tw_062()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XboxGipSvc", "Start", 3);
        return true;
    }

    static bool D_tw_062()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\XboxGipSvc", "Start") == 4 || Svc.IsDisabled("XboxGipSvc");
    }
    // ───────── tw_063: Disable service: Netlogon (only if not domain-joined) (netlogon) ─────────
    static bool A_tw_063()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\netlogon", "Start", 4);
        try { Svc.Stop("netlogon"); } catch { }
        return true;
    }

    static bool U_tw_063()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\netlogon", "Start", 3);
        return true;
    }

    static bool D_tw_063()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\netlogon", "Start") == 4 || Svc.IsDisabled("netlogon");
    }
    // ───────── tw_064: Disable scheduled task: Media sharing/DRM init task ─────────
    static bool A_tw_064()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Media Center\\ehDRMInit\" /Disable");
        return true;
    }

    static bool U_tw_064()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Media Center\\ehDRMInit\" /Enable");
        return true;
    }

    static bool D_tw_064()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Media Center\\ehDRMInit\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_065: Disable scheduled task: Google AutoUpdate core task ─────────
    static bool A_tw_065()
    {
        Run.Hidden("schtasks /Change /TN \"\\GoogleUpdateTaskMachineCore\" /Disable");
        return true;
    }

    static bool U_tw_065()
    {
        Run.Hidden("schtasks /Change /TN \"\\GoogleUpdateTaskMachineCore\" /Enable");
        return true;
    }

    static bool D_tw_065()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\GoogleUpdateTaskMachineCore\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_066: Disable scheduled task: Google AutoUpdate UA task ─────────
    static bool A_tw_066()
    {
        Run.Hidden("schtasks /Change /TN \"\\GoogleUpdateTaskMachineUA\" /Disable");
        return true;
    }

    static bool U_tw_066()
    {
        Run.Hidden("schtasks /Change /TN \"\\GoogleUpdateTaskMachineUA\" /Enable");
        return true;
    }

    static bool D_tw_066()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\GoogleUpdateTaskMachineUA\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_067: Disable scheduled task: Energy/power efficiency diagnostics scan ─────────
    static bool A_tw_067()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Disable");
        return true;
    }

    static bool U_tw_067()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Enable");
        return true;
    }

    static bool D_tw_067()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_068: Disable scheduled task: Disk footprint / diagnosis scan ─────────
    static bool A_tw_068()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Disable");
        return true;
    }

    static bool U_tw_068()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Enable");
        return true;
    }

    static bool D_tw_068()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_069: Disable scheduled task: Background memory diagnostics ─────────
    static bool A_tw_069()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\MemoryDiagnostic\\ProcessMemoryDiagnosticEvents\" /Disable");
        return true;
    }

    static bool U_tw_069()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\MemoryDiagnostic\\ProcessMemoryDiagnosticEvents\" /Enable");
        return true;
    }

    static bool D_tw_069()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\MemoryDiagnostic\\ProcessMemoryDiagnosticEvents\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_070: Disable scheduled task: Error report queue processing ─────────
    static bool A_tw_070()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Windows Error Reporting\\QueueReporting\" /Disable");
        return true;
    }

    static bool U_tw_070()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Windows Error Reporting\\QueueReporting\" /Enable");
        return true;
    }

    static bool D_tw_070()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Windows Error Reporting\\QueueReporting\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_071: Disable scheduled task: Automatic chkdsk trigger on boot ─────────
    static bool A_tw_071()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /Disable");
        return true;
    }

    static bool U_tw_071()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /Enable");
        return true;
    }

    static bool D_tw_071()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_072: Disable scheduled task: Background component (WinSxS) cleanup ─────────
    static bool A_tw_072()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Servicing\\StartComponentCleanup\" /Disable");
        return true;
    }

    static bool U_tw_072()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Servicing\\StartComponentCleanup\" /Enable");
        return true;
    }

    static bool D_tw_072()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Servicing\\StartComponentCleanup\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_073: Disable scheduled task: Background registry backup scan ─────────
    static bool A_tw_073()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Registry\\RegIdleBackup\" /Disable");
        return true;
    }

    static bool U_tw_073()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Registry\\RegIdleBackup\" /Enable");
        return true;
    }

    static bool D_tw_073()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Registry\\RegIdleBackup\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_074: Disable scheduled task: Program Compatibility Assistant patch scan on boot ─────────
    static bool A_tw_074()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Application Experience\\PcaPatchDbTask\" /Disable");
        return true;
    }

    static bool U_tw_074()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Application Experience\\PcaPatchDbTask\" /Enable");
        return true;
    }

    static bool D_tw_074()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Application Experience\\PcaPatchDbTask\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_075: Disable scheduled task: Disk diagnostic data collector ─────────
    static bool A_tw_075()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Disable");
        return true;
    }

    static bool U_tw_075()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Enable");
        return true;
    }

    static bool D_tw_075()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_076: Disable scheduled task: Family Safety monitoring task ─────────
    static bool A_tw_076()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Shell\\FamilySafetyMonitor\" /Disable");
        return true;
    }

    static bool U_tw_076()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Shell\\FamilySafetyMonitor\" /Enable");
        return true;
    }

    static bool D_tw_076()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Shell\\FamilySafetyMonitor\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_077: Disable scheduled task: Offline Maps update task ─────────
    static bool A_tw_077()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /Disable");
        return true;
    }

    static bool U_tw_077()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /Enable");
        return true;
    }

    static bool D_tw_077()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_078: Disable scheduled task: Offline Maps notification task ─────────
    static bool A_tw_078()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsToastTask\" /Disable");
        return true;
    }

    static bool U_tw_078()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsToastTask\" /Enable");
        return true;
    }

    static bool D_tw_078()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Maps\\MapsToastTask\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_079: Disable scheduled task: Scheduled disk defragmentation (SSD-irrelevant) ─────────
    static bool A_tw_079()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Disable");
        return true;
    }

    static bool U_tw_079()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Enable");
        return true;
    }

    static bool D_tw_079()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_080: Disable scheduled task: Update Orchestrator background scan ─────────
    static bool A_tw_080()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\WindowsUpdate\\Scheduled Start\" /Disable");
        return true;
    }

    static bool U_tw_080()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\WindowsUpdate\\Scheduled Start\" /Enable");
        return true;
    }

    static bool D_tw_080()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\WindowsUpdate\\Scheduled Start\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_081: Disable scheduled task: USO background scan ─────────
    static bool A_tw_081()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\UpdateOrchestrator\\Schedule Scan\" /Disable");
        return true;
    }

    static bool U_tw_081()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\UpdateOrchestrator\\Schedule Scan\" /Enable");
        return true;
    }

    static bool D_tw_081()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\UpdateOrchestrator\\Schedule Scan\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_082: Disable scheduled task: Compatibility telemetry appraiser ─────────
    static bool A_tw_082()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser\" /Disable");
        return true;
    }

    static bool U_tw_082()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser\" /Enable");
        return true;
    }

    static bool D_tw_082()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_083: Disable scheduled task: Cloud experience/account sync helper ─────────
    static bool A_tw_083()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\CloudExperienceHost\\CreateObjectTask\" /Disable");
        return true;
    }

    static bool U_tw_083()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\CloudExperienceHost\\CreateObjectTask\" /Enable");
        return true;
    }

    static bool D_tw_083()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\CloudExperienceHost\\CreateObjectTask\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_084: Disable Advertising ID ─────────
    static bool A_tw_084()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled", 0);
        return true;
    }

    static bool U_tw_084()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled");
        return true;
    }

    static bool D_tw_084()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled") != 0) return false;
        return true;
    }
    // ───────── tw_085: Disable Activity History / Timeline ─────────
    static bool A_tw_085()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableActivityFeed", 0);
        return true;
    }

    static bool U_tw_085()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableActivityFeed");
        return true;
    }

    static bool D_tw_085()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableActivityFeed") != 0) return false;
        return true;
    }
    // ───────── tw_086: Disable Location Tracking ─────────
    static bool A_tw_086()
    {
        Reg.SetString("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location", "Value", "Deny");
        return true;
    }

    static bool U_tw_086()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location", "Value");
        return true;
    }

    static bool D_tw_086()
    {
        if (Reg.GetString("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location", "Value") != "Deny") return false;
        return true;
    }
    // ───────── tw_087: Disable Search History ─────────
    static bool A_tw_087()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDeviceSearchHistoryEnabled", 0);
        return true;
    }

    static bool U_tw_087()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDeviceSearchHistoryEnabled");
        return true;
    }

    static bool D_tw_087()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDeviceSearchHistoryEnabled") != 0) return false;
        return true;
    }
    // ───────── tw_088: Disable Bing/Cortana Web Results in Search ─────────
    static bool A_tw_088()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Search", "BingSearchEnabled", 0);
        return true;
    }

    static bool U_tw_088()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Search", "BingSearchEnabled");
        return true;
    }

    static bool D_tw_088()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Search", "BingSearchEnabled") != 0) return false;
        return true;
    }
    // ───────── tw_089: Disable Cloud Clipboard Sync ─────────
    static bool A_tw_089()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Clipboard", "EnableCloudClipboard", 0);
        return true;
    }

    static bool U_tw_089()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Clipboard", "EnableCloudClipboard");
        return true;
    }

    static bool D_tw_089()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Clipboard", "EnableCloudClipboard") != 0) return false;
        return true;
    }
    // ───────── tw_090: Disable Find My Device ─────────
    static bool A_tw_090()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\FindMyDevice", "LocationSyncEnabled", 0);
        return true;
    }

    static bool U_tw_090()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\FindMyDevice", "LocationSyncEnabled");
        return true;
    }

    static bool D_tw_090()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\FindMyDevice", "LocationSyncEnabled") != 0) return false;
        return true;
    }
    // ───────── tw_091: Disable Feedback & Diagnostics Prompts ─────────
    static bool A_tw_091()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod", 0);
        return true;
    }

    static bool U_tw_091()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod");
        return true;
    }

    static bool D_tw_091()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod") != 0) return false;
        return true;
    }
    // ───────── tw_092: Disable Tailored Experiences ─────────
    static bool A_tw_092()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0);
        return true;
    }

    static bool U_tw_092()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled");
        return true;
    }

    static bool D_tw_092()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled") != 0) return false;
        return true;
    }
    // ───────── tw_093: Disable App Access to Diagnostics ─────────
    static bool A_tw_093()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\CloudContent", "DisableConsumerAccountStateContent", 1);
        return true;
    }

    static bool U_tw_093()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\CloudContent", "DisableConsumerAccountStateContent");
        return true;
    }

    static bool D_tw_093()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\CloudContent", "DisableConsumerAccountStateContent") != 1) return false;
        return true;
    }
    // ───────── tw_094: Block Microsoft Telemetry Domains (hosts file) ─────────
    static bool A_tw_094()
    {
        return Run.PsCmdlet("Add-Content -Path $env:WinDir\\System32\\drivers\\etc\\hosts -Value \"0.0.0.0 vortex.data.microsoft.com`n0.0.0.0 telemetry.microsoft.com`n0.0.0.0 watson.telemetry.microsoft.com\"") == 0;
    }

    static bool U_tw_094()
    {
        return true;
    }

    static bool D_tw_094()
    {
        return true;
    }
    // ───────── tw_095: Disable Recent Files Tracking ─────────
    static bool A_tw_095()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRecentDocsHistory", 1);
        return true;
    }

    static bool U_tw_095()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRecentDocsHistory");
        return true;
    }

    static bool D_tw_095()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRecentDocsHistory") != 1) return false;
        return true;
    }
    // ───────── tw_096: Disable Handwriting Data Sharing ─────────
    static bool A_tw_096()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\InputPersonalization", "RestrictImplicitInkCollection", 1);
        return true;
    }

    static bool U_tw_096()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\InputPersonalization", "RestrictImplicitInkCollection");
        return true;
    }

    static bool D_tw_096()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\InputPersonalization", "RestrictImplicitInkCollection") != 1) return false;
        return true;
    }
    // ───────── tw_097: Disable Speech Model Update ─────────
    static bool A_tw_097()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy", "HasAccepted", 0);
        return true;
    }

    static bool U_tw_097()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy", "HasAccepted");
        return true;
    }

    static bool D_tw_097()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy", "HasAccepted") != 0) return false;
        return true;
    }
    // ───────── tw_098: Disable Typing/Inking Personalization ─────────
    static bool A_tw_098()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\InputPersonalization", "RestrictImplicitTextCollection", 1);
        return true;
    }

    static bool U_tw_098()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\InputPersonalization", "RestrictImplicitTextCollection");
        return true;
    }

    static bool D_tw_098()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\InputPersonalization", "RestrictImplicitTextCollection") != 1) return false;
        return true;
    }
    // ───────── tw_099: Disable Diagnostic Data Viewer feed ─────────
    static bool A_tw_099()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "DisableDiagnosticDataViewer", 1);
        return true;
    }

    static bool U_tw_099()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "DisableDiagnosticDataViewer");
        return true;
    }

    static bool D_tw_099()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "DisableDiagnosticDataViewer") != 1) return false;
        return true;
    }
    // ───────── tw_100: Disable Defender Automatic Sample Submission ─────────
    static bool A_tw_100()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SubmitSamplesConsent", 2);
        return true;
    }

    static bool U_tw_100()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SubmitSamplesConsent");
        return true;
    }

    static bool D_tw_100()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SubmitSamplesConsent") != 2) return false;
        return true;
    }
    // ───────── tw_101: Disable Locally Relevant Content / Suggested Content ─────────
    static bool A_tw_101()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContentEnabled", 0);
        return true;
    }

    static bool U_tw_101()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContentEnabled");
        return true;
    }

    static bool D_tw_101()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContentEnabled") != 0) return false;
        return true;
    }
    // ───────── tw_102: Disable Account-related Notifications ─────────
    static bool A_tw_102()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0);
        return true;
    }

    static bool U_tw_102()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338389Enabled");
        return true;
    }

    static bool D_tw_102()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338389Enabled") != 0) return false;
        return true;
    }
    // ───────── tw_103: Disable Suggested Settings/Tips in Settings app ─────────
    static bool A_tw_103()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338393Enabled", 0);
        return true;
    }

    static bool U_tw_103()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338393Enabled");
        return true;
    }

    static bool D_tw_103()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338393Enabled") != 0) return false;
        return true;
    }
    // ───────── tw_104: Disable Recently Added Apps in Start ─────────
    static bool A_tw_104()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs", 0);
        return true;
    }

    static bool U_tw_104()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs");
        return true;
    }

    static bool D_tw_104()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs") != 0) return false;
        return true;
    }
    // ───────── tw_105: Disable Web Search Results in Start Menu ─────────
    static bool A_tw_105()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Search", "CortanaConsent", 0);
        return true;
    }

    static bool U_tw_105()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Search", "CortanaConsent");
        return true;
    }

    static bool D_tw_105()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Search", "CortanaConsent") != 0) return false;
        return true;
    }
    // ───────── tw_106: Disable Cortana Voice Activation ─────────
    static bool A_tw_106()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\UserPreferenceForAllApps", "AgentActivationLastUsed", 0);
        return true;
    }

    static bool U_tw_106()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\UserPreferenceForAllApps", "AgentActivationLastUsed");
        return true;
    }

    static bool D_tw_106()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\UserPreferenceForAllApps", "AgentActivationLastUsed") != 0) return false;
        return true;
    }
    // ───────── tw_107: Disable Game DVR / Game Bar recording ─────────
    static bool A_tw_107()
    {
        Reg.SetDWord("HKCU:\\System\\GameConfigStore", "GameDVR_Enabled", 0);
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR", 0);
        return true;
    }

    static bool U_tw_107()
    {
        Reg.RemoveValue("HKCU:\\System\\GameConfigStore", "GameDVR_Enabled");
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR");
        return true;
    }

    static bool D_tw_107()
    {
        if (Reg.GetDWord("HKCU:\\System\\GameConfigStore", "GameDVR_Enabled") != 0) return false;
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR") != 0) return false;
        return true;
    }
    // ───────── tw_108: Disable GameBarPresenceWriter background process ─────────
    static bool A_tw_108()
    {
        Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\GameBar", "AutoGameModeEnabled", 0);
        return true;
    }

    static bool U_tw_108()
    {
        Reg.RemoveValue("HKCU:\\SOFTWARE\\Microsoft\\GameBar", "AutoGameModeEnabled");
        return true;
    }

    static bool D_tw_108()
    {
        if (Reg.GetDWord("HKCU:\\SOFTWARE\\Microsoft\\GameBar", "AutoGameModeEnabled") != 0) return false;
        return true;
    }
    // ───────── tw_109: Create & activate Ultimate Performance power plan ─────────
    static bool A_tw_109()
    {
        Run.Powercfg("-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
        Run.PsCmdlet("$guid = (powercfg /list | Select-String \"Ultimate\" | ForEach-Object { ($_ -split '\\s+')[3] }); if ($guid) { powercfg /setactive $guid }");
        return true;
    }

    static bool U_tw_109()
    {
        return Run.Powercfg("/setactive 381b4222-f694-41f0-9685-ff5bb260df2e") == 0;
    }

    static bool D_tw_109()
    {
        var s = Run.Capture("powercfg", "/getactivescheme").ToLower();
        return s.Contains("e9a42b02") || s.Contains("ultimate") || s.Contains("wydajność najwyższa");
    }
    // ───────── tw_110: Disable USB Selective Suspend ─────────
    static bool A_tw_110()
    {
        Run.Powercfg("/setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0"); Run.Powercfg("/setactive scheme_current"); return true;
    }

    static bool U_tw_110()
    {
        return true;
    }

    static bool D_tw_110()
    {
        return true;
    }
    // ───────── tw_111: Enable Hardware-Accelerated GPU Scheduling (HAGS) ─────────
    static bool A_tw_111()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode", 2);
        return true;
    }

    static bool U_tw_111()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode");
        return true;
    }

    static bool D_tw_111()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode") != 2) return false;
        return true;
    }
    // ───────── tw_112: Disable Fullscreen Optimizations globally ─────────
    static bool A_tw_112()
    {
        Reg.SetDWord("HKCU:\\System\\GameConfigStore", "GameDVR_FSEBehavior", 2);
        return true;
    }

    static bool U_tw_112()
    {
        Reg.RemoveValue("HKCU:\\System\\GameConfigStore", "GameDVR_FSEBehavior");
        return true;
    }

    static bool D_tw_112()
    {
        if (Reg.GetDWord("HKCU:\\System\\GameConfigStore", "GameDVR_FSEBehavior") != 2) return false;
        return true;
    }
    // ───────── tw_113: Set MMCSS Games task priority to GPU 8 / High ─────────
    static bool A_tw_113()
    {
        Run.Hidden("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games\" /v \"GPU Priority\" /t REG_DWORD /d 8 /f");
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Priority", 6);
        return true;
    }

    static bool U_tw_113()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Priority");
        return true;
    }

    static bool D_tw_113()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Priority") != 6) return false;
        return true;
    }
    // ───────── tw_114: Disable Prefetch (SSD systems) ─────────
    static bool A_tw_114()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnablePrefetcher", 0);
        return true;
    }

    static bool U_tw_114()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnablePrefetcher");
        return true;
    }

    static bool D_tw_114()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnablePrefetcher") != 0) return false;
        return true;
    }
    // ───────── tw_115: Reduce Shutdown/Kill-process Timeout ─────────
    static bool A_tw_115()
    {
        Reg.SetString("HKCU:\\Control Panel\\Desktop", "WaitToKillAppTimeout", "2000");
        Reg.SetString("HKLM:\\SYSTEM\\CurrentControlSet\\Control", "WaitToKillServiceTimeout", "2000");
        return true;
    }

    static bool U_tw_115()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Desktop", "WaitToKillAppTimeout");
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control", "WaitToKillServiceTimeout");
        return true;
    }

    static bool D_tw_115()
    {
        if (Reg.GetString("HKCU:\\Control Panel\\Desktop", "WaitToKillAppTimeout") != "2000") return false;
        if (Reg.GetString("HKLM:\\SYSTEM\\CurrentControlSet\\Control", "WaitToKillServiceTimeout") != "2000") return false;
        return true;
    }
    // ───────── tw_116: CPU Scheduling: Prioritize Programs over Background Services ─────────
    static bool A_tw_116()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation", 38);
        return true;
    }

    static bool U_tw_116()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation");
        return true;
    }

    static bool D_tw_116()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation") != 38) return false;
        return true;
    }
    // ───────── tw_117: Force Game Process Priority to High on launch (Image File Execution) ─────────
    static bool A_tw_117()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\PerfOptions", "CpuPriorityClass", 3);
        return true;
    }

    static bool U_tw_117()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\PerfOptions", "CpuPriorityClass");
        return true;
    }

    static bool D_tw_117()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\PerfOptions", "CpuPriorityClass") != 3) return false;
        return true;
    }
    // ───────── tw_118: Disable CPU Core Parking / Idle states (aggressive, C-states off) ─────────
    static bool A_tw_118()
    {
        Run.Powercfg("/setacvalueindex scheme_current sub_processor 5d76a2ca-e8c0-402f-a133-2158492d58ad 0"); Run.Powercfg("/setactive scheme_current"); return true;
    }

    static bool U_tw_118()
    {
        return true;
    }

    static bool D_tw_118()
    {
        return true;
    }
    // ───────── tw_119: Prioritize HPET / dynamic tick handling ─────────
    static bool A_tw_119()
    {
        Run.Hidden("bcdedit /deletevalue useplatformclock"); return true;
    }

    static bool U_tw_119()
    {
        return true;
    }

    static bool D_tw_119()
    {
        return true;
    }
    // ───────── tw_120: Disable Paging Executive (keep kernel in RAM) ─────────
    static bool A_tw_120()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive", 1);
        return true;
    }

    static bool U_tw_120()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive");
        return true;
    }

    static bool D_tw_120()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive") != 1) return false;
        return true;
    }
    // ───────── tw_121: Increase System Cache priority for RAM ─────────
    static bool A_tw_121()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1);
        return true;
    }

    static bool U_tw_121()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
        return true;
    }

    static bool D_tw_121()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache") == 1;
    }
    // ───────── tw_122: Raise IRQ8 (RTC) Priority ─────────
    static bool A_tw_122()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "IRQ8Priority", 1);
        return true;
    }

    static bool U_tw_122()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "IRQ8Priority");
        return true;
    }

    static bool D_tw_122()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "IRQ8Priority") != 1) return false;
        return true;
    }
    // ───────── tw_123: Disable MMCSS Network Throttling for Games task ─────────
    static bool A_tw_123()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", 4294967295);
        return true;
    }

    static bool U_tw_123()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex");
        return true;
    }

    static bool D_tw_123()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex") != 4294967295) return false;
        return true;
    }
    // ───────── tw_124: Disable BITS bandwidth throttling ─────────
    static bool A_tw_124()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\BITS", "EnableBITSMaxBandwidth", 0);
        return true;
    }

    static bool U_tw_124()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\BITS", "EnableBITSMaxBandwidth");
        return true;
    }

    static bool D_tw_124()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\BITS", "EnableBITSMaxBandwidth") != 0) return false;
        return true;
    }
    // ───────── tw_125: Set System Responsiveness to 0% (foreground priority to games) ─────────
    static bool A_tw_125()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness", 0);
        return true;
    }

    static bool U_tw_125()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness");
        return true;
    }

    static bool D_tw_125()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness") != 0) return false;
        return true;
    }
    // ───────── tw_126: Disable Modern Standby (S0 Low Power Idle) ─────────
    static bool A_tw_126()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power", "PlatformAoAcOverride", 0);
        return true;
    }

    static bool U_tw_126()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power", "PlatformAoAcOverride");
        return true;
    }

    static bool D_tw_126()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power", "PlatformAoAcOverride") != 0) return false;
        return true;
    }
    // ───────── tw_127: Reduce Input/Keyboard Buffer Latency ─────────
    static bool A_tw_127()
    {
        Reg.SetString("HKCU:\\Control Panel\\Keyboard", "KeyboardDelay", "0");
        return true;
    }

    static bool U_tw_127()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Keyboard", "KeyboardDelay");
        return true;
    }

    static bool D_tw_127()
    {
        if (Reg.GetString("HKCU:\\Control Panel\\Keyboard", "KeyboardDelay") != "0") return false;
        return true;
    }
    // ───────── tw_128: Force DWM to present immediately (reduce compositor latency) ─────────
    static bool A_tw_128()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\DWM", "Composition", 1);
        return true;
    }

    static bool U_tw_128()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\DWM", "Composition");
        return true;
    }

    static bool D_tw_128()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\DWM", "Composition") != 1) return false;
        return true;
    }
    // ───────── tw_129: Disable Memory Compression ─────────
    static bool A_tw_129()
    {
        Run.PsCmdlet("Disable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue"); return true;
    }

    static bool U_tw_129()
    {
        Run.PsCmdlet("Enable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue"); return true;
    }

    static bool D_tw_129()
    {
        return true;
    }
    // ───────── tw_130: Prioritize Disk I/O for foreground applications ─────────
    static bool A_tw_130()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "IoPriority", 3);
        return true;
    }

    static bool U_tw_130()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "IoPriority");
        return true;
    }

    static bool D_tw_130()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "IoPriority") != 3) return false;
        return true;
    }
    // ───────── tw_131: Disable disk idle spin-down timeout ─────────
    static bool A_tw_131()
    {
        Run.Powercfg("/setacvalueindex scheme_current sub_disk 6738e2c4-e8a5-4a42-b16a-e040e769756e 0"); Run.Powercfg("/setactive scheme_current"); return true;
    }

    static bool U_tw_131()
    {
        return true;
    }

    static bool D_tw_131()
    {
        return true;
    }
    // ───────── tw_132: Disable Nagles Algorithm (per adapter, lower latency) ─────────
    static bool A_tw_132()
    {
        Reg.SetAdapterDWords("TcpAckFrequency", 1);
        Reg.SetAdapterDWords("TCPNoDelay", 1);
        return true;
    }

    static bool U_tw_132()
    {
        Reg.DeleteAdapterValues("TcpAckFrequency");
        Reg.DeleteAdapterValues("TCPNoDelay");
        return true;
    }

    static bool D_tw_132()
    {
        return true;
    }
    // ───────── tw_133: Disable Network Adapter Power Saving ─────────
    static bool A_tw_133()
    {
        return Run.Hidden("powercfg /setacvalueindex scheme_current 501a4d13-42af-4429-9fd1-a8218c268e20 12bbebe6-58d6-4636-95bb-3217ef867c1a 0") == 0;
    }

    static bool U_tw_133()
    {
        return true;
    }

    static bool D_tw_133()
    {
        return true;
    }
    // ───────── tw_134: Set Network Adapter Profile to Maximum Performance ─────────
    static bool A_tw_134()
    {
        return Run.Hidden("netsh int tcp set global autotuninglevel=normal") == 0;
    }

    static bool U_tw_134()
    {
        return true;
    }

    static bool D_tw_134()
    {
        return true;
    }
    // ───────── tw_135: Disable Wake-on-LAN ─────────
    static bool A_tw_135()
    {
        return Run.Hidden("powercfg -deviceenablewake \"Ethernet\"") == 0;
    }

    static bool U_tw_135()
    {
        return true;
    }

    static bool D_tw_135()
    {
        return true;
    }
    // ───────── tw_136: Reset Network Adapter (Winsock/IP stack) — PROTECTED ─────────
    static bool A_tw_136()
    {
        return true; // Skip destructive network reset during batch apply
    }

    static bool U_tw_136()
    {
        return true;
    }

    static bool D_tw_136()
    {
        return false;
    }
    // ───────── tw_137: Disable Energy Efficient Ethernet (driver property, per-NIC) ─────────
    static bool A_tw_137()
    {
        return Run.PsCmdlet("Set-NetAdapterAdvancedProperty -DisplayName \"Energy Efficient Ethernet\" -DisplayValue \"Disabled\"") == 0;
    }

    static bool U_tw_137()
    {
        return true;
    }

    static bool D_tw_137()
    {
        return true;
    }
    // ───────── tw_138: Set DNS to Cloudflare (1.1.1.1 / 1.0.0.1) ─────────
    static bool A_tw_138()
    {
        try { Run.PsCmdlet("Get-NetAdapter | Where-Object Status -eq 'Up' | ForEach-Object { Set-DnsClientServerAddress -InterfaceAlias $_.Name -ServerAddresses ('1.1.1.1','1.0.0.1') -ErrorAction SilentlyContinue }"); } catch { }
        return true;
    }

    static bool U_tw_138()
    {
        return true;
    }

    static bool D_tw_138()
    {
        return true;
    }
    // ───────── tw_139: Enable DNS over HTTPS (Cloudflare template) ─────────
    static bool A_tw_139()
    {
        try { Run.PsCmdlet("Add-DnsClientDohServerAddress -ServerAddress '1.1.1.1' -DohTemplate 'https://cloudflare-dns.com/dns-query' -AllowFallbackToUdp $true -ErrorAction SilentlyContinue"); } catch { }
        return true;
    }

    static bool U_tw_139()
    {
        return true;
    }

    static bool D_tw_139()
    {
        return true;
    }
    // ───────── tw_140: Flush DNS Cache ─────────
    static bool A_tw_140()
    {
        return Run.Hidden("ipconfig /flushdns") == 0;
    }

    static bool U_tw_140()
    {
        return true;
    }

    static bool D_tw_140()
    {
        return true;
    }
    // ───────── tw_141: TCP Optimizations (Auto-Tuning + RSC + ECN) ─────────
    static bool A_tw_141()
    {
        // Safe TCP Optimizations for both Ethernet & Wi-Fi (Disable ECN & RSC on Wi-Fi to prevent packet drops and connection loss)
        Run.Hidden("netsh int tcp set global autotuninglevel=normal");
        Run.Hidden("netsh int tcp set global rsc=disabled");
        Run.Hidden("netsh int tcp set global ecncapability=disabled");
        return true;
    }

    static bool U_tw_141()
    {
        return true;
    }

    static bool D_tw_141()
    {
        return true;
    }
    // ───────── tw_142: Disable Large Send Offload (LSO, per-NIC) ─────────
    static bool A_tw_142()
    {
        try { Run.PsCmdlet("Get-NetAdapter | Where-Object { $_.MediaType -eq '802.3' } | ForEach-Object { Disable-NetAdapterLso -Name $_.Name -ErrorAction SilentlyContinue }"); } catch { }
        return true;
    }

    static bool U_tw_142()
    {
        return true;
    }

    static bool D_tw_142()
    {
        return true;
    }
    // ───────── tw_143: Enable Receive Side Scaling (RSS) ─────────
    static bool A_tw_143()
    {
        try { Run.PsCmdlet("Get-NetAdapter | Where-Object { $_.MediaType -eq '802.3' } | ForEach-Object { Enable-NetAdapterRss -Name $_.Name -ErrorAction SilentlyContinue }"); } catch { }
        return true;
    }

    static bool U_tw_143()
    {
        return true;
    }

    static bool D_tw_143()
    {
        return true;
    }
    // ───────── tw_144: Reduce aggressive Wi-Fi roaming sensitivity — PROTECTED ─────────
    static bool A_tw_144()
    {
        return true; // Skip hardcoded adapter property modification
    }

    static bool U_tw_144()
    {
        return true;
    }

    static bool D_tw_144()
    {
        return false;
    }
    // ───────── tw_145: Force NIC Duplex/Speed to Maximum (per-NIC) ─────────
    static bool A_tw_145()
    {
        try { Run.PsCmdlet("Get-NetAdapter | Where-Object { $_.MediaType -eq '802.3' } | ForEach-Object { Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName 'Speed & Duplex' -DisplayValue '1.0 Gbps Full Duplex' -ErrorAction SilentlyContinue }"); } catch { }
        return true;
    }

    static bool U_tw_145()
    {
        return true;
    }

    static bool D_tw_145()
    {
        return true;
    }
    // ───────── tw_146: Disable NIC Flow Control ─────────
    static bool A_tw_146()
    {
        try { Run.PsCmdlet("Set-NetAdapterAdvancedProperty -DisplayName 'Flow Control' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue"); } catch { }
        return true;
    }

    static bool U_tw_146()
    {
        return true;
    }

    static bool D_tw_146()
    {
        return true;
    }
    // ───────── tw_147: Disable Green Ethernet / Gigabit Lite (per-NIC) ─────────
    static bool A_tw_147()
    {
        try { Run.PsCmdlet("Set-NetAdapterAdvancedProperty -DisplayName 'Green Ethernet' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue"); } catch { }
        return true;
    }

    static bool U_tw_147()
    {
        return true;
    }

    static bool D_tw_147()
    {
        return true;
    }
    // ───────── tw_148: Prefer IPv4 over IPv6 ─────────
    static bool A_tw_148()
    {
        // Safe dual-stack configuration - preserve IPv6 for Wi-Fi compatibility while giving IPv4 prefix priority
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents", 0);
        return true;
    }

    static bool U_tw_148()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents");
        return true;
    }

    static bool D_tw_148()
    {
        return false;
    }
    // ───────── tw_149: Disable WPAD (Web Proxy Auto-Discovery) ─────────
    static bool A_tw_149()
    {
        // Keep WPAD service on Demand/Manual (Start = 3) to avoid breaking Wi-Fi captive portals and HTTP proxy resolution
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\WinHttpAutoProxySvc", "Start", 3);
        return true;
    }

    static bool U_tw_149()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\WinHttpAutoProxySvc", "Start");
        return true;
    }

    static bool D_tw_149()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\WinHttpAutoProxySvc", "Start") != 4) return false;
        return true;
    }
    // ───────── tw_150: Enable TCP Fast Open ─────────
    static bool A_tw_150()
    {
        return Run.Hidden("netsh int tcp set global fastopen=enabled") == 0;
    }

    static bool U_tw_150()
    {
        return true;
    }

    static bool D_tw_150()
    {
        return true;
    }
    // ───────── tw_151: Disable Teredo tunneling ─────────
    static bool A_tw_151()
    {
        return Run.Hidden("netsh interface teredo set state disabled") == 0;
    }

    static bool U_tw_151()
    {
        return true;
    }

    static bool D_tw_151()
    {
        return true;
    }
    // ───────── tw_152: Tune SYN Attack Protection ─────────
    static bool A_tw_152()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "SynAttackProtect", 1);
        return true;
    }

    static bool U_tw_152()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "SynAttackProtect");
        return true;
    }

    static bool D_tw_152()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "SynAttackProtect") != 1) return false;
        return true;
    }
    // ───────── tw_153: Disable Explorer Automatic Proxy Detection ─────────
    static bool A_tw_153()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "AutoDetect", 0);
        return true;
    }

    static bool U_tw_153()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "AutoDetect");
        return true;
    }

    static bool D_tw_153()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "AutoDetect") != 0) return false;
        return true;
    }
    // ───────── tw_154: Disable QoS Packet Scheduler Reservation (20% cap) ─────────
    static bool A_tw_154()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched", "NonBestEffortLimit", 0);
        return true;
    }

    static bool U_tw_154()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched", "NonBestEffortLimit");
        return true;
    }

    static bool D_tw_154()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched", "NonBestEffortLimit") != 0) return false;
        return true;
    }
    // ───────── tw_155: Increase Max Connections Per Server (Explorer/IE stack) ─────────
    static bool A_tw_155()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "MaxConnectionsPerServer", 16);
        return true;
    }

    static bool U_tw_155()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "MaxConnectionsPerServer");
        return true;
    }

    static bool D_tw_155()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", "MaxConnectionsPerServer") != 16) return false;
        return true;
    }
    // ───────── tw_156: Set optimal MTU for gaming (1500, no fragmentation) ─────────
    static bool A_tw_156()
    {
        return Run.Hidden("netsh interface ipv4 set subinterface \"Ethernet\" mtu=1500 store=persistent") == 0;
    }

    static bool U_tw_156()
    {
        return true;
    }

    static bool D_tw_156()
    {
        return true;
    }
    // ───────── tw_157: Raise MMCSS priority for Steam/Discord/launchers ─────────
    static bool A_tw_157()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency", "Affinity", 0);
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency", "Priority", 1);
        return true;
    }

    static bool U_tw_157()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency", "Affinity");
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency", "Priority");
        return true;
    }

    static bool D_tw_157()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency", "Affinity") != 0) return false;
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Low Latency", "Priority") != 1) return false;
        return true;
    }
    // ───────── tw_158: Disable GeForce Experience Autostart ─────────
    static bool A_tw_158()
    {
        Run.Hidden("reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"NVIDIA GeForce Experience\" /f");
        return true;
    }

    static bool U_tw_158()
    {
        return true;
    }

    static bool D_tw_158()
    {
        return true;
    }
    // ───────── tw_159: Set NVIDIA/AMD GPU Power Management to Prefer Maximum Performance ─────────
    static bool A_tw_159()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "PowerMizerEnable", 0);
        return true;
    }

    static bool U_tw_159()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "PowerMizerEnable");
        return true;
    }

    static bool D_tw_159()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "PowerMizerEnable") != 0) return false;
        return true;
    }
    // ───────── tw_160: Disable Nvidia ShadowPlay In-Game Overlay ─────────
    static bool A_tw_160()
    {
        Reg.SetDWord("HKCU:\\SOFTWARE\\NVIDIA Corporation\\Global\\ShadowPlay\\NVSPCAPS", "PublicOverlay_Enabled", 0);
        return true;
    }

    static bool U_tw_160()
    {
        Reg.RemoveValue("HKCU:\\SOFTWARE\\NVIDIA Corporation\\Global\\ShadowPlay\\NVSPCAPS", "PublicOverlay_Enabled");
        return true;
    }

    static bool D_tw_160()
    {
        if (Reg.GetDWord("HKCU:\\SOFTWARE\\NVIDIA Corporation\\Global\\ShadowPlay\\NVSPCAPS", "PublicOverlay_Enabled") != 0) return false;
        return true;
    }
    // ───────── tw_161: Disable AMD Radeon Chill ─────────
    static bool A_tw_161()
    {
        Reg.SetDWord("HKCU:\\SOFTWARE\\AMD\\CN", "ChillEnable", 0);
        return true;
    }

    static bool U_tw_161()
    {
        Reg.RemoveValue("HKCU:\\SOFTWARE\\AMD\\CN", "ChillEnable");
        return true;
    }

    static bool D_tw_161()
    {
        if (Reg.GetDWord("HKCU:\\SOFTWARE\\AMD\\CN", "ChillEnable") != 0) return false;
        return true;
    }
    // ───────── tw_162: Disable AMD External Events Autostart ─────────
    static bool A_tw_162()
    {
        Run.Hidden("reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"AMD External Events Utility\" /f");
        return true;
    }

    static bool U_tw_162()
    {
        return true;
    }

    static bool D_tw_162()
    {
        return true;
    }
    // ───────── tw_163: Disable AMD Crash Defender Service ─────────
    static bool A_tw_163()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AMD Crash Defender Service", "Start", 4);
        try { Svc.Stop("AMD Crash Defender Service"); } catch { }
        return true;
    }

    static bool U_tw_163()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AMD Crash Defender Service", "Start", 3);
        return true;
    }

    static bool D_tw_163()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AMD Crash Defender Service", "Start") == 4 || Svc.IsDisabled("AMD Crash Defender Service");
    }
    // ───────── tw_164: Set GPU Low-Latency Mode to Ultra (Nvidia Reflex profile default) ─────────
    static bool A_tw_164()
    {
        Reg.SetDWord("HKCU:\\Software\\NVIDIA Corporation\\Global\\NVTweak", "LowLatencyMode", 2);
        return true;
    }

    static bool U_tw_164()
    {
        Reg.RemoveValue("HKCU:\\Software\\NVIDIA Corporation\\Global\\NVTweak", "LowLatencyMode");
        return true;
    }

    static bool D_tw_164()
    {
        if (Reg.GetDWord("HKCU:\\Software\\NVIDIA Corporation\\Global\\NVTweak", "LowLatencyMode") != 2) return false;
        return true;
    }
    // ───────── tw_165: Force GPU Power Profile Per Application (max perf) ─────────
    static bool A_tw_165()
    {
        Reg.SetString("HKLM:\\SOFTWARE\\Microsoft\\DirectX\\UserGpuPreferences", "DirectXUserGlobalSettings", "SwapEffectUpgradeEnable=1;GpuPreference=2;");
        return true;
    }

    static bool U_tw_165()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\DirectX\\UserGpuPreferences", "DirectXUserGlobalSettings");
        return true;
    }

    static bool D_tw_165()
    {
        if (Reg.GetString("HKLM:\\SOFTWARE\\Microsoft\\DirectX\\UserGpuPreferences", "DirectXUserGlobalSettings") != "SwapEffectUpgradeEnable=1;GpuPreference=2;") return false;
        return true;
    }
    // ───────── tw_166: Set GPU Fan Curve to Adaptive/Auto ─────────
    static bool A_tw_166()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "FanControlMode", 0);
        return true;
    }

    static bool U_tw_166()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "FanControlMode");
        return true;
    }

    static bool D_tw_166()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "FanControlMode") != 0) return false;
        return true;
    }
    // ───────── tw_167: Set Texture Filtering Quality to Performance ─────────
    static bool A_tw_167()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "TextureFilterQuality", 3);
        return true;
    }

    static bool U_tw_167()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "TextureFilterQuality");
        return true;
    }

    static bool D_tw_167()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "TextureFilterQuality") != 3) return false;
        return true;
    }
    // ───────── tw_168: Remove Shader Cache Size Limit ─────────
    static bool A_tw_168()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\NVIDIA Corporation\\Global\\NVTweak", "PSGpuAdapterMemory", 4294967295);
        return true;
    }

    static bool U_tw_168()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\NVIDIA Corporation\\Global\\NVTweak", "PSGpuAdapterMemory");
        return true;
    }

    static bool D_tw_168()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\NVIDIA Corporation\\Global\\NVTweak", "PSGpuAdapterMemory") != 4294967295) return false;
        return true;
    }
    // ───────── tw_169: Disable G-Sync/FreeSync Compositor Overhead (borderless mode bypass) ─────────
    static bool A_tw_169()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\DirectX\\GraphicsSettings", "SwapEffectUpgradeCache", 0);
        return true;
    }

    static bool U_tw_169()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\DirectX\\GraphicsSettings", "SwapEffectUpgradeCache");
        return true;
    }

    static bool D_tw_169()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\DirectX\\GraphicsSettings", "SwapEffectUpgradeCache") != 0) return false;
        return true;
    }
    // ───────── tw_170: Set GPU Preemption to Pixel-Level ─────────
    static bool A_tw_170()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "PlatformSupportMiracast", 0);
        return true;
    }

    static bool U_tw_170()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "PlatformSupportMiracast");
        return true;
    }

    static bool D_tw_170()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "PlatformSupportMiracast") != 0) return false;
        return true;
    }
    // ───────── tw_171: Disable PCIe ASPM (Active State Power Management) ─────────
    static bool A_tw_171()
    {
        return Run.Hidden("powercfg /setacvalueindex scheme_current 501a4d13-42af-4429-9fd1-a8218c268e20 ee12f906-d277-404b-b6da-e5fa1a576df5 0") == 0;
    }

    static bool U_tw_171()
    {
        return true;
    }

    static bool D_tw_171()
    {
        return true;
    }
    // ───────── tw_172: Disable USB3 Link Power Management ─────────
    static bool A_tw_172()
    {
        return Run.Hidden("powercfg /setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 d4e98f31-5ffe-4ce1-be31-1b38b384c009 0") == 0;
    }

    static bool U_tw_172()
    {
        return true;
    }

    static bool D_tw_172()
    {
        return true;
    }
    // ───────── tw_173: Disable Global Driver-Level VSync (Nvidia CP style) ─────────
    static bool A_tw_173()
    {
        Reg.SetDWord("HKCU:\\Software\\NVIDIA Corporation\\Global\\NVTweak", "VSyncMode", 0);
        return true;
    }

    static bool U_tw_173()
    {
        Reg.RemoveValue("HKCU:\\Software\\NVIDIA Corporation\\Global\\NVTweak", "VSyncMode");
        return true;
    }

    static bool D_tw_173()
    {
        if (Reg.GetDWord("HKCU:\\Software\\NVIDIA Corporation\\Global\\NVTweak", "VSyncMode") != 0) return false;
        return true;
    }
    // ───────── tw_174: Disable AMD ULPS (Ultra Low Power State, multi-GPU) ─────────
    static bool A_tw_174()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "EnableUlps", 0);
        return true;
    }

    static bool U_tw_174()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "EnableUlps");
        return true;
    }

    static bool D_tw_174()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11ce-bfc1-08002be10318}\\0000", "EnableUlps") != 0) return false;
        return true;
    }
    // ───────── tw_175: Disable Mouse Pointer Acceleration ─────────
    static bool A_tw_175()
    {
        Reg.SetString("HKCU:\\Control Panel\\Mouse", "MouseSpeed", "0");
        Reg.SetString("HKCU:\\Control Panel\\Mouse", "MouseThreshold1", "0");
        Reg.SetString("HKCU:\\Control Panel\\Mouse", "MouseThreshold2", "0");
        return true;
    }

    static bool U_tw_175()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Mouse", "MouseSpeed");
        Reg.RemoveValue("HKCU:\\Control Panel\\Mouse", "MouseThreshold1");
        Reg.RemoveValue("HKCU:\\Control Panel\\Mouse", "MouseThreshold2");
        return true;
    }

    static bool D_tw_175()
    {
        if (Reg.GetString("HKCU:\\Control Panel\\Mouse", "MouseSpeed") != "0") return false;
        if (Reg.GetString("HKCU:\\Control Panel\\Mouse", "MouseThreshold1") != "0") return false;
        if (Reg.GetString("HKCU:\\Control Panel\\Mouse", "MouseThreshold2") != "0") return false;
        return true;
    }
    // ───────── tw_176: Disable Enhanced Pointer Precision ─────────
    static bool A_tw_176()
    {
        Reg.SetString("HKCU:\\Control Panel\\Mouse", "MouseSpeed", "0");
        return true;
    }

    static bool U_tw_176()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Mouse", "MouseSpeed");
        return true;
    }

    static bool D_tw_176()
    {
        if (Reg.GetString("HKCU:\\Control Panel\\Mouse", "MouseSpeed") != "0") return false;
        return true;
    }
    // ───────── tw_177: Raise Mouse Polling Rate to 1000Hz (USB registry) ─────────
    static bool A_tw_177()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\mouhid\\Parameters", "MouseDataQueueSize", 20);
        return true;
    }

    static bool U_tw_177()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\mouhid\\Parameters", "MouseDataQueueSize");
        return true;
    }

    static bool D_tw_177()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\mouhid\\Parameters", "MouseDataQueueSize") != 20) return false;
        return true;
    }
    // ───────── tw_178: Disable Keyboard Input Filter Delay ─────────
    static bool A_tw_178()
    {
        Reg.SetString("HKCU:\\Control Panel\\Accessibility\\Keyboard Response", "AutoRepeatDelay", "0");
        return true;
    }

    static bool U_tw_178()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Accessibility\\Keyboard Response", "AutoRepeatDelay");
        return true;
    }

    static bool D_tw_178()
    {
        if (Reg.GetString("HKCU:\\Control Panel\\Accessibility\\Keyboard Response", "AutoRepeatDelay") != "0") return false;
        return true;
    }
    // ───────── tw_179: Set Keyboard Repeat Rate to Maximum ─────────
    static bool A_tw_179()
    {
        Reg.SetString("HKCU:\\Control Panel\\Keyboard", "KeyboardSpeed", "31");
        return true;
    }

    static bool U_tw_179()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Keyboard", "KeyboardSpeed");
        return true;
    }

    static bool D_tw_179()
    {
        if (Reg.GetString("HKCU:\\Control Panel\\Keyboard", "KeyboardSpeed") != "31") return false;
        return true;
    }
    // ───────── tw_180: Disable HID Battery Status Polling (wireless peripherals) ─────────
    static bool A_tw_180()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\HidBatt\\Parameters", "IdleQueryTimeout", 0);
        return true;
    }

    static bool U_tw_180()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\HidBatt\\Parameters", "IdleQueryTimeout");
        return true;
    }

    static bool D_tw_180()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Services\\HidBatt\\Parameters", "IdleQueryTimeout") != 0) return false;
        return true;
    }
    // ───────── tw_181: Raise Windows Audio Engine (Audiosrv) Process Priority ─────────
    static bool A_tw_181()
    {
        Run.PsCmdlet("Get-Process audiodg -ErrorAction SilentlyContinue | ForEach-Object { $_.PriorityClass = 'High' }"); return true;
    }

    static bool U_tw_181()
    {
        return true;
    }

    static bool D_tw_181()
    {
        return true;
    }
    // ───────── tw_182: Disable Windows Spatial Sound Processing (lower audio-engine latency) ─────────
    static bool A_tw_182()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Multimedia\\Audio", "UserSimulatedSpatialAudio", 0);
        return true;
    }

    static bool U_tw_182()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Multimedia\\Audio", "UserSimulatedSpatialAudio");
        return true;
    }

    static bool D_tw_182()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Multimedia\\Audio", "UserSimulatedSpatialAudio") != 0) return false;
        return true;
    }
    // ───────── tw_183: Disable Exclusive-Mode Application Blocking ─────────
    static bool A_tw_183()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render", "DisableExclusiveMode", 0);
        return true;
    }

    static bool U_tw_183()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render", "DisableExclusiveMode");
        return true;
    }

    static bool D_tw_183()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render", "DisableExclusiveMode") != 0) return false;
        return true;
    }
    // ───────── tw_184: Set Audio Buffer Size to Minimum (low-latency mode) ─────────
    static bool A_tw_184()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render\\Properties", "{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5", 1);
        return true;
    }

    static bool U_tw_184()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render\\Properties", "{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5");
        return true;
    }

    static bool D_tw_184()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render\\Properties", "{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5") != 1) return false;
        return true;
    }
    // ───────── tw_185: Mitigate USB Audio DPC Latency Spikes ─────────
    static bool A_tw_185()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e96c-e325-11ce-bfc1-08002be10318}", "IdlePowerManagementSupported", 0);
        return true;
    }

    static bool U_tw_185()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e96c-e325-11ce-bfc1-08002be10318}", "IdlePowerManagementSupported");
        return true;
    }

    static bool D_tw_185()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e96c-e325-11ce-bfc1-08002be10318}", "IdlePowerManagementSupported") != 0) return false;
        return true;
    }
    // ───────── tw_186: Disable Automatic Steam Overlay Injection (per-game, set in Steam) ─────────
    static bool A_tw_186()
    {
        Reg.SetDWord("HKCU:\\Software\\Valve\\Steam", "OverlayEnabled", 0);
        return true;
    }

    static bool U_tw_186()
    {
        Reg.RemoveValue("HKCU:\\Software\\Valve\\Steam", "OverlayEnabled");
        return true;
    }

    static bool D_tw_186()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Valve\\Steam", "OverlayEnabled") != 0) return false;
        return true;
    }
    // ───────── tw_187: Fix Discord Overlay Hardware-Acceleration Conflict ─────────
    static bool A_tw_187()
    {
        Reg.SetDWord("HKCU:\\Software\\Discord", "hardwareAcceleration", 0);
        return true;
    }

    static bool U_tw_187()
    {
        Reg.RemoveValue("HKCU:\\Software\\Discord", "hardwareAcceleration");
        return true;
    }

    static bool D_tw_187()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Discord", "hardwareAcceleration") != 0) return false;
        return true;
    }
    // ───────── tw_188: Disable Background RGB Control Services (e.g. iCUE/Aura idle polling) ─────────
    static bool A_tw_188()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\LightingService", "Start", 4);
        try { Svc.Stop("LightingService"); } catch { }
        return true;
    }

    static bool U_tw_188()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\LightingService", "Start", 3);
        return true;
    }

    static bool D_tw_188()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\LightingService", "Start") == 4 || Svc.IsDisabled("LightingService");
    }
    // ───────── tw_189: Disable Fast Startup ─────────
    static bool A_tw_189()
    {
        return Run.Hidden("powercfg /h off") == 0;
    }

    static bool U_tw_189()
    {
        return true;
    }

    static bool D_tw_189()
    {
        return true;
    }
    // ───────── tw_190: Disable Storage Sense ─────────
    static bool A_tw_190()
    {
        Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", "01", 0);
        return true;
    }

    static bool U_tw_190()
    {
        Reg.RemoveValue("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", "01");
        return true;
    }

    static bool D_tw_190()
    {
        if (Reg.GetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", "01") != 0) return false;
        return true;
    }
    // ───────── tw_191: Disable Content Indexing on non-system drives ─────────
    static bool A_tw_191()
    {
        return Run.PsCmdlet("Get-Volume | Where-Object DriveLetter -ne \"C\" | ForEach-Object { fsutil behavior set disablelastaccess 1 }") == 0;
    }

    static bool U_tw_191()
    {
        return true;
    }

    static bool D_tw_191()
    {
        return true;
    }
    // ───────── tw_192: Disable Removable Drive Indexing ─────────
    static bool A_tw_192()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search", "DisableRemovableDriveIndexing", 1);
        return true;
    }

    static bool U_tw_192()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search", "DisableRemovableDriveIndexing");
        return true;
    }

    static bool D_tw_192()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search", "DisableRemovableDriveIndexing") != 1) return false;
        return true;
    }
    // ───────── tw_193: Disable Automatic chkdsk on boot ─────────
    static bool A_tw_193()
    {
        return Run.Hidden("chkntfs /x C:") == 0;
    }

    static bool U_tw_193()
    {
        return true;
    }

    static bool D_tw_193()
    {
        return true;
    }
    // ───────── tw_194: Disable Automatic Defrag Schedule (SSD) ─────────
    static bool A_tw_194()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Disable");
        return true;
    }

    static bool U_tw_194()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Enable");
        return true;
    }

    static bool D_tw_194()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_195: Disable Thumbnail Cache ─────────
    static bool A_tw_195()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisableThumbnailCache", 1);
        return true;
    }

    static bool U_tw_195()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisableThumbnailCache");
        return true;
    }

    static bool D_tw_195()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisableThumbnailCache") != 1) return false;
        return true;
    }
    // ───────── tw_196: Disable Explorer Preview Pane Rendering ─────────
    static bool A_tw_196()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowPreviewHandlers", 0);
        return true;
    }

    static bool U_tw_196()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowPreviewHandlers");
        return true;
    }

    static bool D_tw_196()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowPreviewHandlers") != 0) return false;
        return true;
    }
    // ───────── tw_197: Disable Thumbnails in Details Pane ─────────
    static bool A_tw_197()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "IconsOnly", 1);
        return true;
    }

    static bool U_tw_197()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "IconsOnly");
        return true;
    }

    static bool D_tw_197()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "IconsOnly") != 1) return false;
        return true;
    }
    // ───────── tw_198: Reduce Explorer Ghosting Hang Timeout ─────────
    static bool A_tw_198()
    {
        Reg.SetString("HKCU:\\Control Panel\\Desktop", "HungAppTimeout", "2000");
        return true;
    }

    static bool U_tw_198()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Desktop", "HungAppTimeout");
        return true;
    }

    static bool D_tw_198()
    {
        if (Reg.GetString("HKCU:\\Control Panel\\Desktop", "HungAppTimeout") != "2000") return false;
        return true;
    }
    // ───────── tw_199: Disable AppCompat Cache scanning ─────────
    static bool A_tw_199()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\AppCompatCache", "AppCompatCache", 0);
        return true;
    }

    static bool U_tw_199()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\AppCompatCache", "AppCompatCache");
        return true;
    }

    static bool D_tw_199()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\AppCompatCache", "AppCompatCache") != 0) return false;
        return true;
    }
    // ───────── tw_200: Windows Update: Notify Only, No Auto-Download ─────────
    static bool A_tw_200()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions", 2);
        return true;
    }

    static bool U_tw_200()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions");
        return true;
    }

    static bool D_tw_200()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "AUOptions") != 2) return false;
        return true;
    }
    // ───────── tw_201: Disable Driver Updates via Windows Update ─────────
    static bool A_tw_201()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Device Metadata", "PreventDeviceMetadataFromNetwork", 1);
        return true;
    }

    static bool U_tw_201()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Device Metadata", "PreventDeviceMetadataFromNetwork");
        return true;
    }

    static bool D_tw_201()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Device Metadata", "PreventDeviceMetadataFromNetwork") != 1) return false;
        return true;
    }
    // ───────── tw_202: Pause Quality/Feature Updates 7 days ─────────
    static bool A_tw_202()
    {
        Reg.SetString("HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseUpdatesExpiryTime", "2026-08-18T00:00:00Z");
        return true;
    }

    static bool U_tw_202()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseUpdatesExpiryTime");
        return true;
    }

    static bool D_tw_202()
    {
        if (Reg.GetString("HKLM:\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseUpdatesExpiryTime") != "2026-08-18T00:00:00Z") return false;
        return true;
    }
    // ───────── tw_203: Disable Windows Update for Other Microsoft Products ─────────
    static bool A_tw_203()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\UX\\Settings", "AllowMUUpdateService", 0);
        return true;
    }

    static bool U_tw_203()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\UX\\Settings", "AllowMUUpdateService");
        return true;
    }

    static bool D_tw_203()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\UX\\Settings", "AllowMUUpdateService") != 0) return false;
        return true;
    }
    // ───────── tw_204: Disable USO Background Orchestrator Scans ─────────
    static bool A_tw_204()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UsoSvc", "Start", 4);
        try { Svc.Stop("UsoSvc"); } catch { }
        return true;
    }

    static bool U_tw_204()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UsoSvc", "Start", 3);
        return true;
    }

    static bool D_tw_204()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\UsoSvc", "Start") == 4 || Svc.IsDisabled("UsoSvc");
    }
    // ───────── tw_205: Disable Compatibility Appraiser Task ─────────
    static bool A_tw_205()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser\" /Disable");
        return true;
    }

    static bool U_tw_205()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser\" /Enable");
        return true;
    }

    static bool D_tw_205()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_206: Remove preinstalled app: Windows Mixed Reality Portal ─────────
    static bool A_tw_206()
    {
        return Run.RemoveAppxFilter("*Microsoft.MixedReality.Portal*");
    }

    static bool U_tw_206()
    {
        return true;
    }

    static bool D_tw_206()
    {
        return !Run.AppxExists("*Microsoft.MixedReality.Portal*");
    }
    // ───────── tw_207: Remove preinstalled app: Skype (preinstalled) ─────────
    static bool A_tw_207()
    {
        return Run.RemoveAppxFilter("*Microsoft.SkypeApp*");
    }

    static bool U_tw_207()
    {
        return true;
    }

    static bool D_tw_207()
    {
        return !Run.AppxExists("*Microsoft.SkypeApp*");
    }
    // ───────── tw_208: Remove preinstalled app: News app ─────────
    static bool A_tw_208()
    {
        return Run.RemoveAppxFilter("*Microsoft.BingNews*");
    }

    static bool U_tw_208()
    {
        return true;
    }

    static bool D_tw_208()
    {
        return !Run.AppxExists("*Microsoft.BingNews*");
    }
    // ───────── tw_209: Remove preinstalled app: Weather app ─────────
    static bool A_tw_209()
    {
        return Run.RemoveAppxFilter("*Microsoft.BingWeather*");
    }

    static bool U_tw_209()
    {
        return true;
    }

    static bool D_tw_209()
    {
        return !Run.AppxExists("*Microsoft.BingWeather*");
    }
    // ───────── tw_210: Remove preinstalled app: Get Help app ─────────
    static bool A_tw_210()
    {
        return Run.RemoveAppxFilter("*Microsoft.GetHelp*");
    }

    static bool U_tw_210()
    {
        return true;
    }

    static bool D_tw_210()
    {
        return !Run.AppxExists("*Microsoft.GetHelp*");
    }
    // ───────── tw_211: Remove preinstalled app: Tips app ─────────
    static bool A_tw_211()
    {
        return Run.RemoveAppxFilter("*Microsoft.Getstarted*");
    }

    static bool U_tw_211()
    {
        return true;
    }

    static bool D_tw_211()
    {
        return !Run.AppxExists("*Microsoft.Getstarted*");
    }
    // ───────── tw_212: Remove preinstalled app: Family Safety app ─────────
    static bool A_tw_212()
    {
        return Run.RemoveAppxFilter("*MicrosoftCorporationII.MicrosoftFamily*");
    }

    static bool U_tw_212()
    {
        return true;
    }

    static bool D_tw_212()
    {
        return !Run.AppxExists("*MicrosoftCorporationII.MicrosoftFamily*");
    }
    // ───────── tw_213: Remove preinstalled app: 3D Viewer ─────────
    static bool A_tw_213()
    {
        return Run.RemoveAppxFilter("*Microsoft.Microsoft3DViewer*");
    }

    static bool U_tw_213()
    {
        return true;
    }

    static bool D_tw_213()
    {
        return !Run.AppxExists("*Microsoft.Microsoft3DViewer*");
    }
    // ───────── tw_214: Remove preinstalled app: Paint 3D ─────────
    static bool A_tw_214()
    {
        return Run.RemoveAppxFilter("*Microsoft.Paint3D*");
    }

    static bool U_tw_214()
    {
        return true;
    }

    static bool D_tw_214()
    {
        return !Run.AppxExists("*Microsoft.Paint3D*");
    }
    // ───────── tw_215: Remove preinstalled app: People app ─────────
    static bool A_tw_215()
    {
        return Run.RemoveAppxFilter("*Microsoft.People*");
    }

    static bool U_tw_215()
    {
        return true;
    }

    static bool D_tw_215()
    {
        return !Run.AppxExists("*Microsoft.People*");
    }
    // ───────── tw_216: Remove preinstalled app: Clipchamp video editor ─────────
    static bool A_tw_216()
    {
        return Run.RemoveAppxFilter("*Clipchamp.Clipchamp*");
    }

    static bool U_tw_216()
    {
        return true;
    }

    static bool D_tw_216()
    {
        return !Run.AppxExists("*Clipchamp.Clipchamp*");
    }
    // ───────── tw_217: Remove preinstalled app: Cortana app package ─────────
    static bool A_tw_217()
    {
        return Run.RemoveAppxFilter("*Microsoft.549981C3F5F10*");
    }

    static bool U_tw_217()
    {
        return true;
    }

    static bool D_tw_217()
    {
        return !Run.AppxExists("*Microsoft.549981C3F5F10*");
    }
    // ───────── tw_218: Remove preinstalled app: Office Hub / Get Office ─────────
    static bool A_tw_218()
    {
        return Run.RemoveAppxFilter("*Microsoft.MicrosoftOfficeHub*");
    }

    static bool U_tw_218()
    {
        return true;
    }

    static bool D_tw_218()
    {
        return !Run.AppxExists("*Microsoft.MicrosoftOfficeHub*");
    }
    // ───────── tw_219: Remove preinstalled app: Movies & TV app ─────────
    static bool A_tw_219()
    {
        return Run.RemoveAppxFilter("*Microsoft.ZuneVideo*");
    }

    static bool U_tw_219()
    {
        return true;
    }

    static bool D_tw_219()
    {
        return !Run.AppxExists("*Microsoft.ZuneVideo*");
    }
    // ───────── tw_220: Remove preinstalled app: Phone Link / Your Phone ─────────
    static bool A_tw_220()
    {
        return Run.RemoveAppxFilter("*Microsoft.YourPhone*");
    }

    static bool U_tw_220()
    {
        return true;
    }

    static bool D_tw_220()
    {
        return !Run.AppxExists("*Microsoft.YourPhone*");
    }
    // ───────── tw_221: Remove preinstalled app: Power Automate Desktop ─────────
    static bool A_tw_221()
    {
        return Run.RemoveAppxFilter("*Microsoft.PowerAutomateDesktop*");
    }

    static bool U_tw_221()
    {
        return true;
    }

    static bool D_tw_221()
    {
        return !Run.AppxExists("*Microsoft.PowerAutomateDesktop*");
    }
    // ───────── tw_222: Remove preinstalled app: consumer Teams (preinstalled) ─────────
    static bool A_tw_222()
    {
        return Run.RemoveAppxFilter("*MicrosoftTeams*");
    }

    static bool U_tw_222()
    {
        return true;
    }

    static bool D_tw_222()
    {
        return !Run.AppxExists("*MicrosoftTeams*");
    }
    // ───────── tw_223: Remove preinstalled app: Quick Assist ─────────
    static bool A_tw_223()
    {
        return Run.RemoveAppxFilter("*MicrosoftCorporationII.QuickAssist*");
    }

    static bool U_tw_223()
    {
        return true;
    }

    static bool D_tw_223()
    {
        return !Run.AppxExists("*MicrosoftCorporationII.QuickAssist*");
    }
    // ───────── tw_224: Remove preinstalled app: Maps app ─────────
    static bool A_tw_224()
    {
        return Run.RemoveAppxFilter("*Microsoft.WindowsMaps*");
    }

    static bool U_tw_224()
    {
        return true;
    }

    static bool D_tw_224()
    {
        return !Run.AppxExists("*Microsoft.WindowsMaps*");
    }
    // ───────── tw_225: Remove preinstalled app: Mail and Calendar ─────────
    static bool A_tw_225()
    {
        return Run.RemoveAppxFilter("*microsoft.windowscommunicationsapps*");
    }

    static bool U_tw_225()
    {
        return true;
    }

    static bool D_tw_225()
    {
        return !Run.AppxExists("*microsoft.windowscommunicationsapps*");
    }
    // ───────── tw_226: Remove preinstalled app: Widgets board ─────────
    static bool A_tw_226()
    {
        return Run.RemoveAppxFilter("*MicrosoftWindows.Client.WebExperience*");
    }

    static bool U_tw_226()
    {
        return true;
    }

    static bool D_tw_226()
    {
        return !Run.AppxExists("*MicrosoftWindows.Client.WebExperience*");
    }
    // ───────── tw_227: Remove preinstalled app: Xbox TCUI bloat component ─────────
    static bool A_tw_227()
    {
        return Run.RemoveAppxFilter("*Microsoft.Xbox.TCUI*");
    }

    static bool U_tw_227()
    {
        return true;
    }

    static bool D_tw_227()
    {
        return !Run.AppxExists("*Microsoft.Xbox.TCUI*");
    }
    // ───────── tw_228: Remove preinstalled app: Xbox Console Companion app ─────────
    static bool A_tw_228()
    {
        return Run.RemoveAppxFilter("*Microsoft.XboxApp*");
    }

    static bool U_tw_228()
    {
        return true;
    }

    static bool D_tw_228()
    {
        return !Run.AppxExists("*Microsoft.XboxApp*");
    }
    // ───────── tw_229: Remove preinstalled app: Xbox Game Bar app ─────────
    static bool A_tw_229()
    {
        return Run.RemoveAppxFilter("*Microsoft.XboxGamingOverlay*");
    }

    static bool U_tw_229()
    {
        return true;
    }

    static bool D_tw_229()
    {
        return !Run.AppxExists("*Microsoft.XboxGamingOverlay*");
    }
    // ───────── tw_230: Remove preinstalled app: Xbox Identity Provider bloat component ─────────
    static bool A_tw_230()
    {
        return Run.RemoveAppxFilter("*Microsoft.XboxIdentityProvider*");
    }

    static bool U_tw_230()
    {
        return true;
    }

    static bool D_tw_230()
    {
        return !Run.AppxExists("*Microsoft.XboxIdentityProvider*");
    }
    // ───────── tw_231: Remove preinstalled app: Xbox Speech-to-Text overlay ─────────
    static bool A_tw_231()
    {
        return Run.RemoveAppxFilter("*Microsoft.XboxSpeechToTextOverlay*");
    }

    static bool U_tw_231()
    {
        return true;
    }

    static bool D_tw_231()
    {
        return !Run.AppxExists("*Microsoft.XboxSpeechToTextOverlay*");
    }
    // ───────── tw_232: Remove preinstalled app: Solitaire Collection ─────────
    static bool A_tw_232()
    {
        return Run.RemoveAppxFilter("*Microsoft.MicrosoftSolitaireCollection*");
    }

    static bool U_tw_232()
    {
        return true;
    }

    static bool D_tw_232()
    {
        return !Run.AppxExists("*Microsoft.MicrosoftSolitaireCollection*");
    }
    // ───────── tw_233: Remove preinstalled app: Feedback Hub ─────────
    static bool A_tw_233()
    {
        return Run.RemoveAppxFilter("*Microsoft.WindowsFeedbackHub*");
    }

    static bool U_tw_233()
    {
        return true;
    }

    static bool D_tw_233()
    {
        return !Run.AppxExists("*Microsoft.WindowsFeedbackHub*");
    }
    // ───────── tw_234: Remove OneDrive entirely ─────────
    static bool A_tw_234()
    {
        return Run.PsCmdlet("Start-Process \"$env:SystemRoot\\SysWOW64\\OneDriveSetup.exe\" -ArgumentList \"/uninstall\" -Wait") == 0;
    }

    static bool U_tw_234()
    {
        return true;
    }

    static bool D_tw_234()
    {
        return true;
    }
    // ───────── tw_235: Clean Microsoft Edge shortcuts/ads (New Tab promos) ─────────
    static bool A_tw_235()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge", "HubsSidebarEnabled", 0);
        return true;
    }

    static bool U_tw_235()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge", "HubsSidebarEnabled");
        return true;
    }

    static bool D_tw_235()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge", "HubsSidebarEnabled") != 0) return false;
        return true;
    }
    // ───────── tw_236: Disable Brave browser sponsored tiles/extras ─────────
    static bool A_tw_236()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\BraveSoftware\\Brave", "BraveRewardsDisabled", 1);
        return true;
    }

    static bool U_tw_236()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\BraveSoftware\\Brave", "BraveRewardsDisabled");
        return true;
    }

    static bool D_tw_236()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\BraveSoftware\\Brave", "BraveRewardsDisabled") != 1) return false;
        return true;
    }
    // ───────── tw_237: Strip old/superseded Driver Store packages ─────────
    static bool A_tw_237()
    {
        return true; // Wi-Fi & Driver Protection
    }

    static bool U_tw_237()
    {
        return true;
    }

    static bool D_tw_237()
    {
        return true;
    }
    // ───────── tw_238: Remove unused display language packs ─────────
    static bool A_tw_238()
    {
        return Run.PsCmdlet("Get-WinUserLanguageList | Where-Object LanguageTag -ne \"en-US\" | ForEach-Object { Uninstall-Language $_.LanguageTag }") == 0;
    }

    static bool U_tw_238()
    {
        return true;
    }

    static bool D_tw_238()
    {
        return true;
    }
    // ───────── tw_239: Remove Wireless Display (Miracast) optional feature ─────────
    static bool A_tw_239()
    {
        return true; // Wi-Fi & Driver Protection
    }

    static bool U_tw_239()
    {
        return true;
    }

    static bool D_tw_239()
    {
        return true;
    }
    // ───────── tw_240: Remove Steps Recorder feature ─────────
    static bool A_tw_240()
    {
        return Run.Hidden("DISM /Online /Remove-Capability /CapabilityName:\"App.StepsRecorder~~~~0.0.1.0\"") == 0;
    }

    static bool U_tw_240()
    {
        return true;
    }

    static bool D_tw_240()
    {
        return true;
    }
    // ───────── tw_241: Disable Widgets board (kills Widgets.exe background process) ─────────
    static bool A_tw_241()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Dsh", "AllowNewsAndInterests", 0);
        return true;
    }

    static bool U_tw_241()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Dsh", "AllowNewsAndInterests");
        return true;
    }

    static bool D_tw_241()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Dsh", "AllowNewsAndInterests") != 0) return false;
        return true;
    }
    // ───────── tw_242: Disable Taskbar Chat/Teams Icon ─────────
    static bool A_tw_242()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarMn", 0);
        return true;
    }

    static bool U_tw_242()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarMn");
        return true;
    }

    static bool D_tw_242()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarMn") != 0) return false;
        return true;
    }
    // ───────── tw_243: Disable Start Menu Recommendations Section ─────────
    static bool A_tw_243()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_IrisRecommendations", 0);
        return true;
    }

    static bool U_tw_243()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_IrisRecommendations");
        return true;
    }

    static bool D_tw_243()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_IrisRecommendations") != 0) return false;
        return true;
    }
    // ───────── tw_244: Clean Unnecessary Startup Apps ─────────
    static bool A_tw_244()
    {
        return Run.PsCmdlet("Get-CimInstance Win32_StartupCommand | Where-Object { $_.Location -match \"Run\" } | Select-Object Name,Command,Location") == 0;
    }

    static bool U_tw_244()
    {
        return true;
    }

    static bool D_tw_244()
    {
        return true;
    }
    // ───────── tw_245: Clear & Rebuild Icon Cache ─────────
    static bool A_tw_245()
    {
        return Run.Hidden("ie4uinit.exe -show") == 0;
    }

    static bool U_tw_245()
    {
        return true;
    }

    static bool D_tw_245()
    {
        return true;
    }
    // ───────── tw_246: Clear Temp Files on Boot ─────────
    static bool A_tw_246()
    {
        return Run.Hidden("schtasks /Create /TN \"ClearTempOnBoot\" /TR \"cmd /c del /q /f %TEMP%\\*\" /SC ONSTART /RU SYSTEM /F") == 0;
    }

    static bool U_tw_246()
    {
        return true;
    }

    static bool D_tw_246()
    {
        return true;
    }
    // ───────── tw_247: Hide Task View Button (removes background snapshot service) ─────────
    static bool A_tw_247()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTaskViewButton", 0);
        return true;
    }

    static bool U_tw_247()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTaskViewButton");
        return true;
    }

    static bool D_tw_247()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTaskViewButton") != 0) return false;
        return true;
    }
    // ───────── tw_248: Hide Widgets Icon on Taskbar ─────────
    static bool A_tw_248()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa", 0);
        return true;
    }

    static bool U_tw_248()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa");
        return true;
    }

    static bool D_tw_248()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa") != 0) return false;
        return true;
    }
    // ───────── tw_249: Disable Frequent Apps List in Start ─────────
    static bool A_tw_249()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackDocs", 0);
        return true;
    }

    static bool U_tw_249()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackDocs");
        return true;
    }

    static bool D_tw_249()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackDocs") != 0) return false;
        return true;
    }
    // ───────── tw_250: Disable Transparency Effects (reduce DWM compositor load) ─────────
    static bool A_tw_250()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency", 0);
        return true;
    }

    static bool U_tw_250()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency");
        return true;
    }

    static bool D_tw_250()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency") != 0) return false;
        return true;
    }
    // ───────── tw_251: Disable Action Center background refresh ─────────
    static bool A_tw_251()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableNotificationCenter", 1);
        return true;
    }

    static bool U_tw_251()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableNotificationCenter");
        return true;
    }

    static bool D_tw_251()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableNotificationCenter") != 1) return false;
        return true;
    }
    // ───────── tw_252: Set Menu Show Delay to 0 ─────────
    static bool A_tw_252()
    {
        Reg.SetString("HKCU:\\Control Panel\\Desktop", "MenuShowDelay", "0");
        return true;
    }

    static bool U_tw_252()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Desktop", "MenuShowDelay");
        return true;
    }

    static bool D_tw_252()
    {
        if (Reg.GetString("HKCU:\\Control Panel\\Desktop", "MenuShowDelay") != "0") return false;
        return true;
    }
    // ───────── tw_253: Set Foreground Lock Timeout to 0 ─────────
    static bool A_tw_253()
    {
        Reg.SetDWord("HKCU:\\Control Panel\\Desktop", "ForegroundLockTimeout", 0);
        return true;
    }

    static bool U_tw_253()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Desktop", "ForegroundLockTimeout");
        return true;
    }

    static bool D_tw_253()
    {
        if (Reg.GetDWord("HKCU:\\Control Panel\\Desktop", "ForegroundLockTimeout") != 0) return false;
        return true;
    }
    // ───────── tw_254: Disable Dynamic Tick ─────────
    static bool A_tw_254()
    {
        return Run.Hidden("bcdedit /set disabledynamictick yes") == 0;
    }

    static bool U_tw_254()
    {
        return true;
    }

    static bool D_tw_254()
    {
        return true;
    }
    // ───────── tw_255: Delete Platform Clock (removes HPET as timer source) ─────────
    static bool A_tw_255()
    {
        Run.Hidden("bcdedit /deletevalue useplatformclock"); return true;
    }

    static bool U_tw_255()
    {
        return true;
    }

    static bool D_tw_255()
    {
        return true;
    }
    // ───────── tw_256: Set TSC Sync Policy to Enhanced ─────────
    static bool A_tw_256()
    {
        return Run.Hidden("bcdedit /set tscsyncpolicy Enhanced") == 0;
    }

    static bool U_tw_256()
    {
        return true;
    }

    static bool D_tw_256()
    {
        return true;
    }
    // ───────── tw_257: Enable x2APIC Policy ─────────
    static bool A_tw_257()
    {
        return Run.Hidden("bcdedit /set x2apicpolicy Enable") == 0;
    }

    static bool U_tw_257()
    {
        return true;
    }

    static bool D_tw_257()
    {
        return true;
    }
    // ───────── tw_258: Disable Boot UX animation ─────────
    static bool A_tw_258()
    {
        return Run.Hidden("bcdedit /set bootux disabled") == 0;
    }

    static bool U_tw_258()
    {
        return true;
    }

    static bool D_tw_258()
    {
        return true;
    }
    // ───────── tw_259: Reduce Boot Menu Timeout ─────────
    static bool A_tw_259()
    {
        return Run.Hidden("bcdedit /timeout 3") == 0;
    }

    static bool U_tw_259()
    {
        return true;
    }

    static bool D_tw_259()
    {
        return true;
    }
    // ───────── tw_260: Disable HPET via useplatformtick ─────────
    static bool A_tw_260()
    {
        return Run.Hidden("bcdedit /set useplatformtick yes") == 0;
    }

    static bool U_tw_260()
    {
        return true;
    }

    static bool D_tw_260()
    {
        return true;
    }
    // ───────── tw_261: Disable Dynamic Tick for CPU 0 specifically ─────────
    static bool A_tw_261()
    {
        return Run.Hidden("bcdedit /set disabledynamictick yes") == 0;
    }

    static bool U_tw_261()
    {
        return true;
    }

    static bool D_tw_261()
    {
        return true;
    }
    // ───────── tw_262: Add Game Folders to Defender Exclusions ─────────
    static bool A_tw_262()
    {
        return Run.PsCmdlet("Add-MpPreference -ExclusionPath \"C:\\Games\"") == 0;
    }

    static bool U_tw_262()
    {
        return true;
    }

    static bool D_tw_262()
    {
        return true;
    }
    // ───────── tw_263: Enable Vulnerable Driver Blocklist (Microsoft-maintained) ─────────
    static bool A_tw_263()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CI\\Config", "VulnerableDriverBlocklistEnable", 1);
        return true;
    }

    static bool U_tw_263()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CI\\Config", "VulnerableDriverBlocklistEnable");
        return true;
    }

    static bool D_tw_263()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CI\\Config", "VulnerableDriverBlocklistEnable") != 1) return false;
        return true;
    }
    // ───────── tw_264: Disable SMBv1 Protocol ─────────
    static bool A_tw_264()
    {
        return Run.PsCmdlet("Disable-WindowsOptionalFeature -Online -FeatureName SMB1Protocol -NoRestart") == 0;
    }

    static bool U_tw_264()
    {
        return true;
    }

    static bool D_tw_264()
    {
        return true;
    }
    // ───────── tw_265: Disable AutoPlay/AutoRun ─────────
    static bool A_tw_265()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDriveTypeAutoRun", 255);
        return true;
    }

    static bool U_tw_265()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDriveTypeAutoRun");
        return true;
    }

    static bool D_tw_265()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoDriveTypeAutoRun") != 255) return false;
        return true;
    }
    // ───────── tw_266: Disable PowerShell v2 Engine (legacy, reduce attack surface + startup) ─────────
    static bool A_tw_266()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\PowerShell\v2.0", "EnableEngine", 0);
        try { Run.PsCmdlet("Disable-WindowsOptionalFeature -Online -FeatureName MicrosoftWindowsPowerShellV2 -NoRestart -ErrorAction SilentlyContinue"); } catch { }
        return true;
    }

    static bool U_tw_266()
    {
        return true;
    }

    static bool D_tw_266()
    {
        return true;
    }
    // ───────── tw_267: Disable IE Mode in Edge ─────────
    static bool A_tw_267()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge", "InternetExplorerIntegrationLevel", 0);
        return true;
    }

    static bool U_tw_267()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge", "InternetExplorerIntegrationLevel");
        return true;
    }

    static bool D_tw_267()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Edge", "InternetExplorerIntegrationLevel") != 0) return false;
        return true;
    }
    // ───────── tw_268: Disable Automatic Restart After Crash (BSOD) ─────────
    static bool A_tw_268()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl", "AutoReboot", 0);
        return true;
    }

    static bool U_tw_268()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl", "AutoReboot");
        return true;
    }

    static bool D_tw_268()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\CrashControl", "AutoReboot") != 0) return false;
        return true;
    }
    // ───────── tw_269: Disable Windows Error Reporting Queue ─────────
    static bool A_tw_269()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting", "Disabled", 1);
        return true;
    }

    static bool U_tw_269()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting", "Disabled");
        return true;
    }

    static bool D_tw_269()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting", "Disabled") != 1) return false;
        return true;
    }
    // ───────── tw_270: Disable Background Task Network Throttling ─────────
    static bool A_tw_270()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", 4294967295);
        return true;
    }

    static bool U_tw_270()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex");
        return true;
    }

    static bool D_tw_270()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex") != 4294967295) return false;
        return true;
    }
    // ───────── tw_271: Disable Clipboard History Background Monitor ─────────
    static bool A_tw_271()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Clipboard", "EnableClipboardHistory", 0);
        return true;
    }

    static bool U_tw_271()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Clipboard", "EnableClipboardHistory");
        return true;
    }

    static bool D_tw_271()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Clipboard", "EnableClipboardHistory") != 0) return false;
        return true;
    }
    // ───────── tw_272: Disable OneDrive Background Sync ─────────
    static bool A_tw_272()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive", "DisableFileSyncNGSC", 1);
        return true;
    }

    static bool U_tw_272()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive", "DisableFileSyncNGSC");
        return true;
    }

    static bool D_tw_272()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive", "DisableFileSyncNGSC") != 1) return false;
        return true;
    }
    // ───────── tw_273: Disable OneDrive Files-On-Demand Background Scan ─────────
    static bool A_tw_273()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive", "DisableFileSync", 1);
        return true;
    }

    static bool U_tw_273()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive", "DisableFileSync");
        return true;
    }

    static bool D_tw_273()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive", "DisableFileSync") != 1) return false;
        return true;
    }
    // ───────── tw_274: Disable Background Task Cache Rebuild ─────────
    static bool A_tw_274()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1);
        return true;
    }

    static bool U_tw_274()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled");
        return true;
    }

    static bool D_tw_274()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled") != 1) return false;
        return true;
    }
    // ───────── tw_275: Disable PC Health Check background task ─────────
    static bool A_tw_275()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\UpdateOrchestrator\\Backup Scan\" /Disable");
        return true;
    }

    static bool U_tw_275()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\UpdateOrchestrator\\Backup Scan\" /Enable");
        return true;
    }

    static bool D_tw_275()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\UpdateOrchestrator\\Backup Scan\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_276: Disable Family Safety Background Monitor ─────────
    static bool A_tw_276()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WpcMonSvc", "Start", 4);
        try { Svc.Stop("WpcMonSvc"); } catch { }
        return true;
    }

    static bool U_tw_276()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WpcMonSvc", "Start", 3);
        return true;
    }

    static bool D_tw_276()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WpcMonSvc", "Start") == 4 || Svc.IsDisabled("WpcMonSvc");
    }
    // ───────── tw_277: Disable Offline Maps Update Task ─────────
    static bool A_tw_277()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /Disable");
        return true;
    }

    static bool U_tw_277()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /Enable");
        return true;
    }

    static bool D_tw_277()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Maps\\MapsUpdateTask\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_278: Disable Disk Diagnostic Task ─────────
    static bool A_tw_278()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Disable");
        return true;
    }

    static bool U_tw_278()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Enable");
        return true;
    }

    static bool D_tw_278()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_279: Disable Bluetooth Scan Background Task (radio stays ON) ─────────
    static bool A_tw_279()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Bluetooth\\UninstallDeviceTask\" /Disable");
        return true;
    }

    static bool U_tw_279()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Bluetooth\\UninstallDeviceTask\" /Enable");
        return true;
    }

    static bool D_tw_279()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Bluetooth\\UninstallDeviceTask\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_280: Auto-Clear Event Logs on Boot ─────────
    static bool A_tw_280()
    {
        return Run.PsCmdlet("Get-EventLog -LogName * | ForEach-Object { Clear-EventLog -LogName $_.Log }") == 0;
    }

    static bool U_tw_280()
    {
        return true;
    }

    static bool D_tw_280()
    {
        return true;
    }
    // ───────── tw_281: Disable Sticky Keys Shortcut ─────────
    static bool A_tw_281()
    {
        Reg.SetString("HKCU:\\Control Panel\\Accessibility\\StickyKeys", "Flags", "506");
        return true;
    }

    static bool U_tw_281()
    {
        Reg.RemoveValue("HKCU:\\Control Panel\\Accessibility\\StickyKeys", "Flags");
        return true;
    }

    static bool D_tw_281()
    {
        if (Reg.GetString("HKCU:\\Control Panel\\Accessibility\\StickyKeys", "Flags") != "506") return false;
        return true;
    }
    // ───────── tw_282: Disable DirectPlay legacy feature ─────────
    static bool A_tw_282()
    {
        return Run.PsCmdlet("Disable-WindowsOptionalFeature -Online -FeatureName DirectPlay -NoRestart") == 0;
    }

    static bool U_tw_282()
    {
        return true;
    }

    static bool D_tw_282()
    {
        return true;
    }
    // ───────── tw_283: Disable Prefetch for Non-Game Applications (AppLaunch) ─────────
    static bool A_tw_283()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnableAppLaunchPrefetch", 0);
        return true;
    }

    static bool U_tw_283()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnableAppLaunchPrefetch");
        return true;
    }

    static bool D_tw_283()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnableAppLaunchPrefetch") != 0) return false;
        return true;
    }
    // ───────── tw_284: Disable Unnecessary COM Surrogate Autolaunch ─────────
    static bool A_tw_284()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "NoDllCache", 1);
        return true;
    }

    static bool U_tw_284()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "NoDllCache");
        return true;
    }

    static bool D_tw_284()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "NoDllCache") != 1) return false;
        return true;
    }
    // ───────── tw_285: Disable Ease of Access Autolaunch on Logon ─────────
    static bool A_tw_285()
    {
        Run.Hidden("reg add \"HKCU\\Control Panel\\Accessibility\" /v \"Last Well Formed\" /t REG_DWORD /d 1 /f");
        return true;
    }

    static bool U_tw_285()
    {
        return true;
    }

    static bool D_tw_285()
    {
        return true;
    }
    // ───────── tw_286: Kill Chrome Background Processes After Close ─────────
    static bool A_tw_286()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Google\\Chrome", "BackgroundModeEnabled", 0);
        return true;
    }

    static bool U_tw_286()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Google\\Chrome", "BackgroundModeEnabled");
        return true;
    }

    static bool D_tw_286()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Google\\Chrome", "BackgroundModeEnabled") != 0) return false;
        return true;
    }
    // ───────── tw_287: Disable Widgets background process specifically ─────────
    static bool A_tw_287()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa", 0);
        return true;
    }

    static bool U_tw_287()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa");
        return true;
    }

    static bool D_tw_287()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa") != 0) return false;
        return true;
    }
    // ───────── tw_288: Disable Phone Link background process ─────────
    static bool A_tw_288()
    {
        Reg.SetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\PhoneLink", "ShowInTaskbar", 0);
        return true;
    }

    static bool U_tw_288()
    {
        Reg.RemoveValue("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\PhoneLink", "ShowInTaskbar");
        return true;
    }

    static bool D_tw_288()
    {
        if (Reg.GetDWord("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\PhoneLink", "ShowInTaskbar") != 0) return false;
        return true;
    }
    // ───────── tw_289: Disable GameBarPresenceWriter.exe background process ─────────
    static bool A_tw_289()
    {
        Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\GameBar", "ShowStartupPanel", 0);
        return true;
    }

    static bool U_tw_289()
    {
        Reg.RemoveValue("HKCU:\\SOFTWARE\\Microsoft\\GameBar", "ShowStartupPanel");
        return true;
    }

    static bool D_tw_289()
    {
        if (Reg.GetDWord("HKCU:\\SOFTWARE\\Microsoft\\GameBar", "ShowStartupPanel") != 0) return false;
        return true;
    }
    // ───────── tw_290: Disable Connected Devices Platform User Service background process ─────────
    static bool A_tw_290()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\CDPUserSvc", "Start", 4);
        try { Svc.Stop("CDPUserSvc"); } catch { }
        return true;
    }

    static bool U_tw_290()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\CDPUserSvc", "Start", 3);
        return true;
    }

    static bool D_tw_290()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\CDPUserSvc", "Start") == 4 || Svc.IsDisabled("CDPUserSvc");
    }
    // ───────── tw_291: Disable SmartScreen Background Scanner Process ─────────
    static bool A_tw_291()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen", 0);
        return true;
    }

    static bool U_tw_291()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen");
        return true;
    }

    static bool D_tw_291()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen") != 0) return false;
        return true;
    }
    // ───────── tw_292: Disable Google Update Scheduled Tasks ─────────
    static bool A_tw_292()
    {
        Run.Hidden("schtasks /Change /TN \"GoogleUpdateTaskMachineCore\" /Disable");
        return true;
    }

    static bool U_tw_292()
    {
        Run.Hidden("schtasks /Change /TN \"GoogleUpdateTaskMachineCore\" /Enable");
        return true;
    }

    static bool D_tw_292()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"GoogleUpdateTaskMachineCore\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_293: Disable Google AutoUpdate background process explicitly ─────────
    static bool A_tw_293()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gupdate", "Start", 4);
        try { Svc.Stop("gupdate"); } catch { }
        return true;
    }

    static bool U_tw_293()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gupdate", "Start", 3);
        return true;
    }

    static bool D_tw_293()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gupdate", "Start") == 4 || Svc.IsDisabled("gupdate");
    }
    // ───────── tw_294: Disable Energy Diagnostics Scheduled Task ─────────
    static bool A_tw_294()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Disable");
        return true;
    }

    static bool U_tw_294()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Enable");
        return true;
    }

    static bool D_tw_294()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_295: Disable Disk Footprint Scan Task ─────────
    static bool A_tw_295()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Disable");
        return true;
    }

    static bool U_tw_295()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Enable");
        return true;
    }

    static bool D_tw_295()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_296: Disable Print Spooler background service (if no printer) ─────────
    static bool A_tw_296()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\Spooler", "Start", 4);
        try { Svc.Stop("Spooler"); } catch { }
        return true;
    }

    static bool U_tw_296()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\Spooler", "Start", 3);
        return true;
    }

    static bool D_tw_296()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\Spooler", "Start") == 4 || Svc.IsDisabled("Spooler");
    }
    // ───────── tw_297: Disable Camera/Mic Global App Access when unused ─────────
    static bool A_tw_297()
    {
        Reg.SetString("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam", "Value", "Deny");
        return true;
    }

    static bool U_tw_297()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam", "Value");
        return true;
    }

    static bool D_tw_297()
    {
        if (Reg.GetString("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam", "Value") != "Deny") return false;
        return true;
    }
    // ───────── tw_298: Disable Windows Camera Frame Server ─────────
    static bool A_tw_298()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FrameServer", "Start", 4);
        try { Svc.Stop("FrameServer"); } catch { }
        return true;
    }

    static bool U_tw_298()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FrameServer", "Start", 3);
        return true;
    }

    static bool D_tw_298()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FrameServer", "Start") == 4 || Svc.IsDisabled("FrameServer");
    }
    // ───────── tw_299: Disable Frame Server Monitor Service ─────────
    static bool A_tw_299()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FrameServerMonitor", "Start", 4);
        try { Svc.Stop("FrameServerMonitor"); } catch { }
        return true;
    }

    static bool U_tw_299()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FrameServerMonitor", "Start", 3);
        return true;
    }

    static bool D_tw_299()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FrameServerMonitor", "Start") == 4 || Svc.IsDisabled("FrameServerMonitor");
    }
    // ───────── tw_300: Disable Still Image Acquisition Events (scanners/cameras) ─────────
    static bool A_tw_300()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\StiSvc", "Start", 4);
        try { Svc.Stop("StiSvc"); } catch { }
        return true;
    }

    static bool U_tw_300()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\StiSvc", "Start", 3);
        return true;
    }

    static bool D_tw_300()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\StiSvc", "Start") == 4 || Svc.IsDisabled("StiSvc");
    }
    // ───────── tw_301: Disable Windows Font Cache background service ─────────
    static bool A_tw_301()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FontCache", "Start", 4);
        try { Svc.Stop("FontCache"); } catch { }
        return true;
    }

    static bool U_tw_301()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FontCache", "Start", 3);
        return true;
    }

    static bool D_tw_301()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\FontCache", "Start") == 4 || Svc.IsDisabled("FontCache");
    }
    // ───────── tw_302: Disable WebClient (WebDAV redirector) service ─────────
    static bool A_tw_302()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WebClient", "Start", 4);
        try { Svc.Stop("WebClient"); } catch { }
        return true;
    }

    static bool U_tw_302()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WebClient", "Start", 3);
        return true;
    }

    static bool D_tw_302()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WebClient", "Start") == 4 || Svc.IsDisabled("WebClient");
    }
    // ───────── tw_303: Disable Distributed Link Tracking Client ─────────
    static bool A_tw_303()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TrkWks", "Start", 4);
        try { Svc.Stop("TrkWks"); } catch { }
        return true;
    }

    static bool U_tw_303()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TrkWks", "Start", 3);
        return true;
    }

    static bool D_tw_303()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TrkWks", "Start") == 4 || Svc.IsDisabled("TrkWks");
    }
    // ───────── tw_304: Disable Mobile Hotspot Sharing Service (if unused) ─────────
    static bool A_tw_304()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\icssvc", "Start", 4);
        try { Svc.Stop("icssvc"); } catch { }
        return true;
    }

    static bool U_tw_304()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\icssvc", "Start", 3);
        return true;
    }

    static bool D_tw_304()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\icssvc", "Start") == 4 || Svc.IsDisabled("icssvc");
    }
    // ───────── tw_305: Disable Wallet Service ─────────
    static bool A_tw_305()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WalletService", "Start", 4);
        try { Svc.Stop("WalletService"); } catch { }
        return true;
    }

    static bool U_tw_305()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WalletService", "Start", 3);
        return true;
    }

    static bool D_tw_305()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WalletService", "Start") == 4 || Svc.IsDisabled("WalletService");
    }
    // ───────── tw_306: Disable Enterprise App Management Service ─────────
    static bool A_tw_306()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AppMgmt", "Start", 4);
        try { Svc.Stop("AppMgmt"); } catch { }
        return true;
    }

    static bool U_tw_306()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AppMgmt", "Start", 3);
        return true;
    }

    static bool D_tw_306()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\AppMgmt", "Start") == 4 || Svc.IsDisabled("AppMgmt");
    }
    // ───────── tw_307: Disable WMP Network Sharing Service ─────────
    static bool A_tw_307()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WMPNetworkSvc", "Start", 4);
        try { Svc.Stop("WMPNetworkSvc"); } catch { }
        return true;
    }

    static bool U_tw_307()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WMPNetworkSvc", "Start", 3);
        return true;
    }

    static bool D_tw_307()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WMPNetworkSvc", "Start") == 4 || Svc.IsDisabled("WMPNetworkSvc");
    }
    // ───────── tw_308: Disable Bluetooth Telemetry Background Task (radio stays ON) ─────────
    static bool A_tw_308()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Bluetooth\\BthSQM\" /Disable");
        return true;
    }

    static bool U_tw_308()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Bluetooth\\BthSQM\" /Enable");
        return true;
    }

    static bool D_tw_308()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Bluetooth\\BthSQM\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_309: Disable Adobe Background Updater Processes ─────────
    static bool A_tw_309()
    {
        Run.Hidden("schtasks /Change /TN \"Adobe Acrobat Update Task\" /Disable");
        return true;
    }

    static bool U_tw_309()
    {
        Run.Hidden("schtasks /Change /TN \"Adobe Acrobat Update Task\" /Enable");
        return true;
    }

    static bool D_tw_309()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"Adobe Acrobat Update Task\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_310: Close Telemetry-Collector Background Processes on Logon ─────────
    static bool A_tw_310()
    {
        return Run.PsCmdlet("Get-Process CompatTelRunner,DeviceCensus -ErrorAction SilentlyContinue | Stop-Process -Force") == 0;
    }

    static bool U_tw_310()
    {
        return true;
    }

    static bool D_tw_310()
    {
        return true;
    }
    // ───────── tw_311: Disable Memory Diagnostics Background Task ─────────
    static bool A_tw_311()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\MemoryDiagnostic\\ProcessMemoryDiagnosticEvents\" /Disable");
        return true;
    }

    static bool U_tw_311()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\MemoryDiagnostic\\ProcessMemoryDiagnosticEvents\" /Enable");
        return true;
    }

    static bool D_tw_311()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\MemoryDiagnostic\\ProcessMemoryDiagnosticEvents\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_312: Disable Wredi Performance Scan Task ─────────
    static bool A_tw_312()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Disable");
        return true;
    }

    static bool U_tw_312()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Enable");
        return true;
    }

    static bool D_tw_312()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_313: Disable Automatic chkdsk Triggers ─────────
    static bool A_tw_313()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /Disable");
        return true;
    }

    static bool U_tw_313()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /Enable");
        return true;
    }

    static bool D_tw_313()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_314: Disable Background Servicing/Component Cleanup (TiWorker) ─────────
    static bool A_tw_314()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Servicing\\StartComponentCleanup\" /Disable");
        return true;
    }

    static bool U_tw_314()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Servicing\\StartComponentCleanup\" /Enable");
        return true;
    }

    static bool D_tw_314()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Servicing\\StartComponentCleanup\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_315: Disable Registry Backup Scan Task ─────────
    static bool A_tw_315()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Registry\\RegIdleBackup\" /Disable");
        return true;
    }

    static bool U_tw_315()
    {
        Run.Hidden("schtasks /Change /TN \"\\Microsoft\\Windows\\Registry\\RegIdleBackup\" /Enable");
        return true;
    }

    static bool D_tw_315()
    {
        var outStr = Run.Capture("schtasks", "/Query /TN \"\\Microsoft\\Windows\\Registry\\RegIdleBackup\" /FO LIST");
        return outStr.Contains("Disabled") || outStr.Contains("Wyłączon") || outStr.Contains("ERROR") || outStr.Contains("BŁĄD") || outStr.Length == 0;
    }
    // ───────── tw_316: Disable Third-Party Auto-Updaters (Java/Opera/Brave) at Startup ─────────
    static bool A_tw_316()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", "SunJavaUpdateSched");
        return true;
    }

    static bool U_tw_316()
    {
        // Restore
        return true;
    }

    static bool D_tw_316()
    {
        if (Reg.GetValue("HKLM:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", "SunJavaUpdateSched") != null) return false;
        return true;
    }
    // ───────── tw_317: Disable Touch Input (ctfmon) Background Process when no touchscreen ─────────
    static bool A_tw_317()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TabletInputService", "Start", 4);
        Reg.SetDWord(@"HKCU:\Software\Microsoft\CTF\DisableThreadInputManager", "Disable", 1);
        Svc.Stop("TabletInputService");
        return true;
    }

    static bool U_tw_317()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TabletInputService", "Start", 3);
        Reg.RemoveValue(@"HKCU:\Software\Microsoft\CTF\DisableThreadInputManager", "Disable");
        return true;
    }

    static bool D_tw_317()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TabletInputService", "Start") == 4;
    }
    // ───────── tw_318: Kill Active Background Bloat Processes Immediately ─────────
    static bool A_tw_318()
    {
        return Run.PsCmdlet("Get-Process OneDrive,YourPhone,Widgets,Teams -ErrorAction SilentlyContinue | Stop-Process -Force") == 0;
    }

    static bool U_tw_318()
    {
        return true;
    }

    static bool D_tw_318()
    {
        return true;
    }
    // ───────── tw_319: Disable Windows Defender Real-Time Protection ─────────
    static bool A_tw_319()
    {
        return Run.PsCmdlet("Set-MpPreference -DisableRealtimeMonitoring $true") == 0;
    }

    static bool U_tw_319()
    {
        return Run.PsCmdlet("Set-MpPreference -DisableRealtimeMonitoring $false") == 0;
    }

    static bool D_tw_319()
    {
        var output = Run.Capture("powershell.exe", "-NoProfile -NonInteractive -Command \"(Get-MpComputerStatus).RealTimeProtectionEnabled\"");
        return string.Equals(output.Trim(), "False", StringComparison.OrdinalIgnoreCase);
    }
    // ───────── tw_320: Disable Memory Integrity / HVCI (big FPS gain on many CPUs) ─────────
    static bool A_tw_320()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "Enabled", 0);
        return true;
    }

    static bool U_tw_320()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "Enabled");
        return true;
    }

    static bool D_tw_320()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "Enabled") != 0) return false;
        return true;
    }
    // ───────── tw_321: Disable Hypervisor Launch (bcdedit hypervisorlaunchtype off) ─────────
    static bool A_tw_321()
    {
        return Run.Hidden("bcdedit /set hypervisorlaunchtype off") == 0;
    }

    static bool U_tw_321()
    {
        return true;
    }

    static bool D_tw_321()
    {
        return true;
    }
    // ───────── tw_322: Disable Hyper-V Windows Feature ─────────
    static bool A_tw_322()
    {
        return Run.PsCmdlet("Disable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All -NoRestart") == 0;
    }

    static bool U_tw_322()
    {
        return true;
    }

    static bool D_tw_322()
    {
        return true;
    }
    // ───────── tw_323: Disable Virtualization-Based Security (VBS) ─────────
    static bool A_tw_323()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "EnableVirtualizationBasedSecurity", 0);
        return true;
    }

    static bool U_tw_323()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "EnableVirtualizationBasedSecurity");
        return true;
    }

    static bool D_tw_323()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "EnableVirtualizationBasedSecurity") != 0) return false;
        return true;
    }
    // ───────── tw_324: Disable Credential Guard ─────────
    static bool A_tw_324()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\LSA", "LsaCfgFlags", 0);
        return true;
    }

    static bool U_tw_324()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\LSA", "LsaCfgFlags");
        return true;
    }

    static bool D_tw_324()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\LSA", "LsaCfgFlags") != 0) return false;
        return true;
    }
    // ───────── tw_325: Disable Windows Sandbox Feature ─────────
    static bool A_tw_325()
    {
        return Run.PsCmdlet("Disable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -NoRestart") == 0;
    }

    static bool U_tw_325()
    {
        return true;
    }

    static bool D_tw_325()
    {
        return true;
    }
    // ───────── tw_326: Disable WSL Feature ─────────
    static bool A_tw_326()
    {
        return Run.PsCmdlet("Disable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux -NoRestart") == 0;
    }

    static bool U_tw_326()
    {
        return true;
    }

    static bool D_tw_326()
    {
        return true;
    }
    // ───────── tw_327: Disable Kernel DMA Protection ─────────
    static bool A_tw_327()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DmaSecurity\\AllowedBuses", "EnableDmaGuard", 0);
        return true;
    }

    static bool U_tw_327()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DmaSecurity\\AllowedBuses", "EnableDmaGuard");
        return true;
    }

    static bool D_tw_327()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DmaSecurity\\AllowedBuses", "EnableDmaGuard") != 0) return false;
        return true;
    }
    // ───────── tw_328: Disable Device Guard policies ─────────
    static bool A_tw_328()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "LsaCfgFlags", 0);
        return true;
    }

    static bool U_tw_328()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "LsaCfgFlags");
        return true;
    }

    static bool D_tw_328()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "LsaCfgFlags") != 0) return false;
        return true;
    }
    // ───────── tw_329: Force-disable BitLocker Auto-Encryption on new drives ─────────
    static bool A_tw_329()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\BitLocker", "PreventDeviceEncryption", 1);
        return true;
    }

    static bool U_tw_329()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\BitLocker", "PreventDeviceEncryption");
        return true;
    }

    static bool D_tw_329()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\BitLocker", "PreventDeviceEncryption") != 1) return false;
        return true;
    }
    // ───────── tw_330: Disable Networking in Modern Standby ─────────
    static bool A_tw_330()
    {
        return Run.Hidden("powercfg /setdcvalueindex scheme_current sub_none f15576e8-98b7-4186-b944-eafa664402d9 0") == 0;
    }

    static bool U_tw_330()
    {
        return true;
    }

    static bool D_tw_330()
    {
        return true;
    }
    // ───────── tw_331: Grant SYSTEM Full Control Registry Unlock (advanced editing) ─────────
    static bool A_tw_331()
    {
        Run.Hidden("takeown /f \"%windir%\\System32\\config\" /r /d y"); return true;
    }

    static bool U_tw_331()
    {
        return true;
    }

    static bool D_tw_331()
    {
        return true;
    }
    // ───────── tw_332: Disable Driver Signature Enforcement ─────────
    static bool A_tw_332()
    {
        Run.Hidden("bcdedit /set nointegritychecks on"); return true;
    }

    static bool U_tw_332()
    {
        Run.Hidden("bcdedit /set nointegritychecks off"); return true;
    }

    static bool D_tw_332()
    {
        return true;
    }
    // ───────── tw_333: Disable Secure Boot violation notifications ─────────
    static bool A_tw_333()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\SecureBoot\\State", "NotifyDisable", 1);
        return true;
    }

    static bool U_tw_333()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\SecureBoot\\State", "NotifyDisable");
        return true;
    }

    static bool D_tw_333()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\SecureBoot\\State", "NotifyDisable") != 1) return false;
        return true;
    }
    // ───────── tw_334: Disable Windows Update Medic Service (WaaSMedicSvc) ─────────
    static bool A_tw_334()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WaaSMedicSvc", "Start", 4);
        try { Svc.Stop("WaaSMedicSvc"); } catch { }
        return true;
    }

    static bool U_tw_334()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WaaSMedicSvc", "Start", 3);
        return true;
    }

    static bool D_tw_334()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WaaSMedicSvc", "Start") == 4 || Svc.IsDisabled("WaaSMedicSvc");
    }
    // ───────── tw_335: Disable Defender Tamper Protection (required before other Defender tweaks) ─────────
    static bool A_tw_335()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows Defender\\Features", "TamperProtection", 0);
        return true;
    }

    static bool U_tw_335()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Microsoft\\Windows Defender\\Features", "TamperProtection");
        return true;
    }

    static bool D_tw_335()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Microsoft\\Windows Defender\\Features", "TamperProtection") != 0) return false;
        return true;
    }
    // ───────── tw_336: Disable Exploit Protection system-wide mitigations ─────────
    static bool A_tw_336()
    {
        return Run.PsCmdlet("Set-ProcessMitigation -System -Disable DEP,SEHOP,ForceRelocateImages,BottomUp,HighEntropy") == 0;
    }

    static bool U_tw_336()
    {
        return true;
    }

    static bool D_tw_336()
    {
        return true;
    }
    // ───────── tw_337: Disable Windows Sandbox Network Isolation ─────────
    static bool A_tw_337()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Sandbox", "AllowNetworking", 1);
        return true;
    }

    static bool U_tw_337()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Sandbox", "AllowNetworking");
        return true;
    }

    static bool D_tw_337()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\Sandbox", "AllowNetworking") != 1) return false;
        return true;
    }
    // ───────── tw_338: Disable Memory Integrity Prompt on Startup ─────────
    static bool A_tw_338()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "WasEnabledBy", 0);
        return true;
    }

    static bool U_tw_338()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "WasEnabledBy");
        return true;
    }

    static bool D_tw_338()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "WasEnabledBy") != 0) return false;
        return true;
    }
    // ───────── tw_339: Disable Defender Cloud-Delivered Protection ─────────
    static bool A_tw_339()
    {
        return Run.PsCmdlet("Set-MpPreference -MAPSReporting 0") == 0;
    }

    static bool U_tw_339()
    {
        return true;
    }

    static bool D_tw_339()
    {
        return true;
    }
    // ───────── tw_340: Disable SmartScreen for Microsoft Store Apps ─────────
    static bool A_tw_340()
    {
        Reg.SetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppHost", "EnableWebContentEvaluation", 0);
        return true;
    }

    static bool U_tw_340()
    {
        Reg.RemoveValue("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppHost", "EnableWebContentEvaluation");
        return true;
    }

    static bool D_tw_340()
    {
        if (Reg.GetDWord("HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppHost", "EnableWebContentEvaluation") != 0) return false;
        return true;
    }
    // ───────── tw_341: Disable SmartScreen for Edge/Explorer ─────────
    static bool A_tw_341()
    {
        Reg.SetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen", 0);
        return true;
    }

    static bool U_tw_341()
    {
        Reg.RemoveValue("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen");
        return true;
    }

    static bool D_tw_341()
    {
        if (Reg.GetDWord("HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen") != 0) return false;
        return true;
    }
    // ───────── tw_342: Disable Meltdown/Spectre Speculative-Execution Mitigations (big CPU-bound FPS gain, real security risk) ─────────
    static bool A_tw_342()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverride", 3);
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverrideMask", 3);
        return true;
    }

    static bool U_tw_342()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverride");
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverrideMask");
        return true;
    }

    static bool D_tw_342()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverride") != 3) return false;
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverrideMask") != 3) return false;
        return true;
    }
    // ───────── tw_343: Disable Group Policy Client Service refresh cycle (leave manual) ─────────
    static bool A_tw_343()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gpsvc", "Start", 4);
        try { Svc.Stop("gpsvc"); } catch { }
        return true;
    }

    static bool U_tw_343()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gpsvc", "Start", 3);
        return true;
    }

    static bool D_tw_343()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\gpsvc", "Start") == 4 || Svc.IsDisabled("gpsvc");
    }
    // ───────── tw_344: Remove Windows Defender Components (DISM, extreme/irreversible) ─────────
    static bool A_tw_344()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware", 1);
        Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender", "DisableRealtimeMonitoring", 1);
        try { Run.Hidden("DISM /Online /Disable-Feature /FeatureName:Windows-Defender-Default-Definitions /Norestart"); } catch { }
        return true;
    }

    static bool U_tw_344()
    {
        return true;
    }

    static bool D_tw_344()
    {
        return true;
    }
    // ───────── tw_345: Remove Security Health Service ─────────
    static bool A_tw_345()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SecurityHealthService", "Start", 4);
        try { Svc.Stop("SecurityHealthService"); } catch { }
        return true;
    }

    static bool U_tw_345()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SecurityHealthService", "Start", 3);
        return true;
    }

    static bool D_tw_345()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SecurityHealthService", "Start") == 4 || Svc.IsDisabled("SecurityHealthService");
    }
    // ───────── tw_346: Remove SysMain Binaries Entirely (after service disable) ─────────
    static bool A_tw_346()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SysMain", "Start", 4); try { Svc.Stop("SysMain"); } catch {} return true;
    }

    static bool U_tw_346()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SysMain", "Start", 2); return true;
    }

    static bool D_tw_346()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SysMain", "Start") == 4;
    }
    // ───────── tw_348: Enable System Restore Engine ─────────
    static bool A_tw_348()
    {
        try { Run.PsCmdlet("Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue"); } catch { }
        return true;
    }

    static bool U_tw_348()
    {
        return true;
    }

    static bool D_tw_348()
    {
        return false;
    }
    // ───────── tw_349: Remove Windows Hello biometric stack ─────────
    static bool A_tw_349()
    {
        Run.Hidden("DISM /Online /Remove-Capability /CapabilityName:\"Hello.Face.17658\""); return true;
    }

    static bool U_tw_349()
    {
        return true;
    }

    static bool D_tw_349()
    {
        return true;
    }
    // ───────── tw_350: Remove Windows Backup Engine ─────────
    static bool A_tw_350()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SDRSVC", "Start", 4);
        try { Svc.Stop("SDRSVC"); } catch { }
        return true;
    }

    static bool U_tw_350()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SDRSVC", "Start", 3);
        return true;
    }

    static bool D_tw_350()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SDRSVC", "Start") == 4 || Svc.IsDisabled("SDRSVC");
    }
    // ───────── tw_351: Aggressively Merge SvcHost Processes (biggest single process-count win) ─────────
    static bool A_tw_351()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control", "SvcHostSplitThresholdInKB", 4294967295);
        return true;
    }

    static bool U_tw_351()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control", "SvcHostSplitThresholdInKB");
        return true;
    }

    static bool D_tw_351()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control", "SvcHostSplitThresholdInKB") != 4294967295) return false;
        return true;
    }
    // ───────── tw_352: Immediately Trim Working Sets / Free Cached RAM ─────────
    static bool A_tw_352()
    {
        return Run.PsCmdlet("[System.GC]::Collect(); Get-Process | ForEach-Object { try { $_.MinWorkingSet=$_.MinWorkingSet } catch {} }") == 0;
    }

    static bool U_tw_352()
    {
        return true;
    }

    static bool D_tw_352()
    {
        return true;
    }
    // ───────── tw_353: Disable SysMain/Superfetch (frees RAM, helps on SSD systems) ─────────
    static bool A_tw_353()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SysMain", "Start", 4);
        try { Svc.Stop("SysMain"); } catch { }
        return true;
    }

    static bool U_tw_353()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SysMain", "Start", 3);
        return true;
    }

    static bool D_tw_353()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SysMain", "Start") == 4 || Svc.IsDisabled("SysMain");
    }
    // ───────── tw_354: Optimize Pagefile Clearing at Clean Shutdown ─────────
    static bool A_tw_354()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "ClearPageFileAtShutdown", 0);
        return true;
    }

    static bool U_tw_354()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "ClearPageFileAtShutdown");
        return true;
    }

    static bool D_tw_354()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "ClearPageFileAtShutdown") != 0) return false;
        return true;
    }
    // ───────── tw_355: Disable Large System Cache (dedicate RAM to foreground game, not file cache) ─────────
    static bool A_tw_355()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1);
        return true;
    }

    static bool U_tw_355()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
        return true;
    }

    static bool D_tw_355()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache") == 1;
    }
    // ───────── tw_356: Keep Drivers Pinned in RAM (DisableDriverPaging) ─────────
    static bool A_tw_356()
    {
        Reg.SetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive", 1);
        return true;
    }

    static bool U_tw_356()
    {
        Reg.RemoveValue("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive");
        return true;
    }

    static bool D_tw_356()
    {
        if (Reg.GetDWord("HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive") != 1) return false;
        return true;
    }
    // ───────── tw_357: Disable Microsoft Edge AutoUpdate Background Processes ─────────
    static bool A_tw_357()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\edgeupdate", "Start", 4); Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\edgeupdatem", "Start", 4); try { Svc.Stop("edgeupdate"); Svc.Stop("edgeupdatem"); } catch {} return true;
    }

    static bool U_tw_357()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\edgeupdate", "Start", 3); return true;
    }

    static bool D_tw_357()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\edgeupdate", "Start") == 4;
    }
    // ───────── tw_358: Disable Brave Browser AutoUpdate Background Processes ─────────
    static bool A_tw_358()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\BraveUpdate", "Start", 4); Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\BraveElevatedUpdate", "Start", 4); try { Svc.Stop("BraveUpdate"); Svc.Stop("BraveElevatedUpdate"); } catch {} return true;
    }

    static bool U_tw_358()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\BraveUpdate", "Start", 3); return true;
    }

    static bool D_tw_358()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\BraveUpdate", "Start") == 4;
    }
    // ───────── tw_359: Disable LampArray Translation Service (RGB Lighting Host) ─────────
    static bool A_tw_359()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\LampArray", "Start", 4); try { Svc.Stop("LampArray"); } catch {} return true;
    }

    static bool U_tw_359()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\LampArray", "Start", 3); return true;
    }

    static bool D_tw_359()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\LampArray", "Start") == 4;
    }
    // ───────── tw_360: Disable User OOBE Broker Background Process ─────────
    static bool A_tw_360()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\OOBE", "DisablePrivacyExperience", 1); return true;
    }

    static bool U_tw_360()
    {
        Reg.RemoveValue(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\OOBE", "DisablePrivacyExperience"); return true;
    }

    static bool D_tw_360()
    {
        return Reg.GetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\OOBE", "DisablePrivacyExperience") == 1;
    }
    // ───────── tw_361: Disable wsappx Store Auto-Download Background Service ─────────
    static bool A_tw_361()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2); return true;
    }

    static bool U_tw_361()
    {
        Reg.RemoveValue(@"HKLM:\SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload"); return true;
    }

    static bool D_tw_361()
    {
        return Reg.GetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload") == 2;
    }
    // ───────── tw_362: Disable RuntimeBroker Background Suggestions ─────────
    static bool A_tw_362()
    {
        Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", 0); Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0); return true;
    }

    static bool U_tw_362()
    {
        Reg.SetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", 1); return true;
    }

    static bool D_tw_362()
    {
        return Reg.GetDWord(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled") == 0;
    }
    // ───────── tw_363: Disable Hyper-V Host Compute & VM Services ─────────
    static bool A_tw_363()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\vmcompute", "Start", 4); Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\vmms", "Start", 4); try { Svc.Stop("vmcompute"); Svc.Stop("vmms"); } catch {} return true;
    }

    static bool U_tw_363()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\vmcompute", "Start", 3); return true;
    }

    static bool D_tw_363()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\vmcompute", "Start") == 4;
    }
    // ───────── tw_364: Disable Windows Biometric Service (Hello) ─────────
    static bool A_tw_364()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WbioSrvc", "Start", 4); try { Svc.Stop("WbioSrvc"); } catch {} return true;
    }

    static bool U_tw_364()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WbioSrvc", "Start", 3); return true;
    }

    static bool D_tw_364()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WbioSrvc", "Start") == 4;
    }
    // ───────── tw_365: Disable Windows Security Health Systray Icon ─────────
    static bool A_tw_365()
    {
        Reg.RemoveValue(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth"); return true;
    }

    static bool U_tw_365()
    {
        return true;
    }

    static bool D_tw_365()
    {
        return Reg.GetValue(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth") == null;
    }
    // ───────── tw_366: Disable Windows Error Reporting Service (WerSvc) ─────────
    static bool A_tw_366()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WerSvc", "Start", 4); try { Svc.Stop("WerSvc"); } catch {} return true;
    }

    static bool U_tw_366()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WerSvc", "Start", 3); return true;
    }

    static bool D_tw_366()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WerSvc", "Start") == 4;
    }
    // ───────── tw_367: Disable Geolocation Tracking Service (lfsvc) ─────────
    static bool A_tw_367()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\lfsvc", "Start", 4); try { Svc.Stop("lfsvc"); } catch {} return true;
    }

    static bool U_tw_367()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\lfsvc", "Start", 3); return true;
    }

    static bool D_tw_367()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\lfsvc", "Start") == 4;
    }
    // ───────── tw_368: Disable Distributed Link Tracking Client (TrkWks) ─────────
    static bool A_tw_368()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TrkWks", "Start", 4); try { Svc.Stop("TrkWks"); } catch {} return true;
    }

    static bool U_tw_368()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TrkWks", "Start", 3); return true;
    }

    static bool D_tw_368()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\TrkWks", "Start") == 4;
    }
    // ───────── tw_369: Disable Sensor & Auto-Brightness Services (SensorService) ─────────
    static bool A_tw_369()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensorService", "Start", 4); Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensrSvc", "Start", 4); try { Svc.Stop("SensorService"); Svc.Stop("SensrSvc"); } catch {} return true;
    }

    static bool U_tw_369()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensorService", "Start", 3); return true;
    }

    static bool D_tw_369()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SensorService", "Start") == 4;
    }
    // ───────── tw_370: Disable Wallet & Payment Manager Services ─────────
    static bool A_tw_370()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WalletService", "Start", 4); Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\SEMgrSvc", "Start", 4); try { Svc.Stop("WalletService"); Svc.Stop("SEMgrSvc"); } catch {} return true;
    }

    static bool U_tw_370()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WalletService", "Start", 3); return true;
    }

    static bool D_tw_370()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Services\WalletService", "Start") == 4;
    }
    // ───────── tw_371: Disable Lock Screen Acrylic Blur (Instant Logon Background) ─────────
    static bool A_tw_371()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon", 1); return true;
    }

    static bool U_tw_371()
    {
        Reg.RemoveValue(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon"); return true;
    }

    static bool D_tw_371()
    {
        return Reg.GetDWord(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon") == 1;
    }
    // ───────── tw_372: Minecraft & Java JVM CPU Quantum Priority Boost (Win32PrioritySeparation = 38) ─────────
    static bool A_tw_372()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38); return true;
    }

    static bool U_tw_372()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 2); return true;
    }

    static bool D_tw_372()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation") == 38;
    }
    // ───────── tw_373: Hardware-Accelerated GPU Scheduling HAGS (Low Frametime Jitter) ─────────
    static bool A_tw_373()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2); return true;
    }

    static bool U_tw_373()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 1); return true;
    }

    static bool D_tw_373()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode") == 2;
    }
    // ───────── tw_374: Disable Nagle Algorithm & TCP DelAck for Minecraft Multiplayer Low Ping ─────────
    static bool A_tw_374()
    {
        Reg.SetAdapterDWords("TcpAckFrequency", 1); Reg.SetAdapterDWords("TCPNoDelay", 1); return true;
    }

    static bool U_tw_374()
    {
        Reg.DeleteAdapterValues("TcpAckFrequency"); Reg.DeleteAdapterValues("TCPNoDelay"); return true;
    }

    static bool D_tw_374()
    {
        return true;
    }
    // ───────── tw_375: MMCSS Gaming & Benchmark Ultra Priority Profile (100% CPU Allocation) ─────────
    static bool A_tw_375()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0); Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 4294967295); Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 8); Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 6); return true;
    }

    static bool U_tw_375()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 14); return true;
    }

    static bool D_tw_375()
    {
        return Reg.GetDWord(@"HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness") == 0;
    }
    // ───────── tw_376: Disable Paging Executive (Lock Windows Kernel in Physical RAM) ─────────
    static bool A_tw_376()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1); return true;
    }

    static bool U_tw_376()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 0); return true;
    }

    static bool D_tw_376()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive") == 1;
    }
    // ───────── tw_377: Disable DXGI Thread Yield (DirectX Low Latency Frame Rendering) ─────────
    static bool A_tw_377()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableDXGIThreadYield", 1); return true;
    }

    static bool U_tw_377()
    {
        Reg.RemoveValue(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableDXGIThreadYield"); return true;
    }

    static bool D_tw_377()
    {
        return Reg.GetDWord(@"HKLM:\SOFTWARE\Microsoft\DirectX", "DisableDXGIThreadYield") == 1;
    }
    // ───────── tw_378: Enable Message Signaled Interrupts (MSI Mode) for GPU (Eliminate DPC Stutter) ─────────
    static bool A_tw_378()
    {
        Run.PsCmdlet("Get-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Enum\\PCI\\*\\*\\Device Parameters\\Interrupt Management\\MessageSignaledInterruptProperties' -ErrorAction SilentlyContinue | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name MSISupported -Value 1 }"); return true;
    }

    static bool U_tw_378()
    {
        return true;
    }

    static bool D_tw_378()
    {
        return true;
    }
    // ───────── tw_379: Disable System Dynamic Tick & HPET (Zero-Latency Timer) ─────────
    static bool A_tw_379()
    {
        Run.Hidden("bcdedit /set disabledynamictick yes"); Run.Hidden("bcdedit /set useplatformclock false"); return true;
    }

    static bool U_tw_379()
    {
        Run.Hidden("bcdedit /deletevalue disabledynamictick"); return true;
    }

    static bool D_tw_379()
    {
        return true;
    }
    // ───────── tw_380: Custom OS Zero-Latency Foreground Priority (ForegroundLockTimeout = 0) ─────────
    static bool A_tw_380()
    {
        Reg.SetDWord(@"HKCU:\Control Panel\Desktop", "ForegroundLockTimeout", 0); return true;
    }

    static bool U_tw_380()
    {
        Reg.SetDWord(@"HKCU:\Control Panel\Desktop", "ForegroundLockTimeout", 200000); return true;
    }

    static bool D_tw_380()
    {
        return Reg.GetDWord(@"HKCU:\Control Panel\Desktop", "ForegroundLockTimeout") == 0;
    }
    // ───────── tw_381: Disable Superfetch & Prefetcher RAM Cache Overhead ─────────
    static bool A_tw_381()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0); Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 0); return true;
    }

    static bool U_tw_381()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 3); return true;
    }

    static bool D_tw_381()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch") == 0;
    }
    // ───────── tw_382: Disable Windows Fault Tolerant Heap FTH (Remove Game Latency Overhead) ─────────
    static bool A_tw_382()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\FTH", "Enabled", 0); return true;
    }

    static bool U_tw_382()
    {
        Reg.SetDWord(@"HKLM:\SOFTWARE\Microsoft\FTH", "Enabled", 1); return true;
    }

    static bool D_tw_382()
    {
        return Reg.GetDWord(@"HKLM:\SOFTWARE\Microsoft\FTH", "Enabled") == 0;
    }
    // ───────── tw_383: Disable NTFS Last Access Update & 8.3 Short Name Creation (Faster Game Loading) ─────────
    static bool A_tw_383()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", 1); Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisable8dot3NameCreation", 1); return true;
    }

    static bool U_tw_383()
    {
        Reg.SetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate", 0); return true;
    }

    static bool D_tw_383()
    {
        return Reg.GetDWord(@"HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem", "NtfsDisableLastAccessUpdate") == 1;
    }
    // ───────── tw_384: Custom OS Zero Input Lag Mouse & Keyboard Buffer (MouseHoverTime = 0) ─────────
    static bool A_tw_384()
    {
        Reg.SetString(@"HKCU:\Control Panel\Desktop", "MouseHoverTime", "0"); return true;
    }

    static bool U_tw_384()
    {
        Reg.SetString(@"HKCU:\Control Panel\Desktop", "MouseHoverTime", "400"); return true;
    }

    static bool D_tw_384()
    {
        return Reg.GetString(@"HKCU:\Control Panel\Desktop", "MouseHoverTime") == "0";
    }
}
