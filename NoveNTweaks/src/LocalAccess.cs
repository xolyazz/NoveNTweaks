namespace NoveNTweaks;

/// <summary>Local welcome gate. Any nonblank text opens this source edition.</summary>
internal static class LocalAccess
{
    private const string SettingsPath = @"HKCU:\SOFTWARE\NoveNTweaks";
    public static bool HasEntered => Reg.GetDWord(SettingsPath, "SourceAccessAccepted") == 1;

    public static bool TryEnter(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        // Remember only the welcome state, never the entered text.
        Reg.SetDWord(SettingsPath, "SourceAccessAccepted", 1);
        return true;
    }

    public static void Reset() => Reg.RemoveValue(SettingsPath, "SourceAccessAccepted");
}
