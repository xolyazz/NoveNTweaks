using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace NoveNTweaks;

// Port of the PowerShell Set-Reg / Get-RegVal / Remove-Reg helpers.
// Accepts PowerShell-style paths like "HKLM:\SOFTWARE\..." and converts
// them to HKEY + subkey.
//
// v2.0 — hardened version: every setter returns a truthful success flag,
// (all writes must succeed), and SetDWordAll/SetStringAll batch helpers
// make it easy to write correct Apply functions that don't lie about success.
public static class Reg
{
    public enum ValueType { DWord, String, Binary, QWord, ExpandString, MultiString }

    // ───────── Single-value setters (return true only if the write succeeded) ─────────
    public static bool SetDWord(string psPath, string name, int value)
        => SetDWord(psPath, name, (uint)value);

    public static bool SetDWord(string psPath, string name, uint value)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        return SetDWord(root, sub, name, value);
    }

    public static bool SetDWord(RegistryKey root, string subkey, string name, uint value)
    {
        try
        {
            using var k = root.CreateSubKey(subkey, true);
            if (k == null) return false;
            k.SetValue(name, value, RegistryValueKind.DWord);
            return true;
        }
        catch { return false; }
    }

    public static bool SetQWord(string psPath, string name, long value)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        try
        {
            using var k = root.CreateSubKey(sub, true);
            if (k == null) return false;
            k.SetValue(name, value, RegistryValueKind.QWord);
            return true;
        }
        catch { return false; }
    }

    public static bool SetString(string psPath, string name, string value)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        return SetString(root, sub, name, value);
    }

    public static bool SetString(RegistryKey root, string subkey, string name, string value)
    {
        try
        {
            using var k = root.CreateSubKey(subkey, true);
            if (k == null) return false;
            k.SetValue(name, value, RegistryValueKind.String);
            return true;
        }
        catch { return false; }
    }

    public static bool SetExpandString(string psPath, string name, string value)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        try
        {
            using var k = root.CreateSubKey(sub, true);
            if (k == null) return false;
            k.SetValue(name, value, RegistryValueKind.ExpandString);
            return true;
        }
        catch { return false; }
    }

    public static bool SetMultiString(string psPath, string name, string[] value)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        try
        {
            using var k = root.CreateSubKey(sub, true);
            if (k == null) return false;
            k.SetValue(name, value, RegistryValueKind.MultiString);
            return true;
        }
        catch { return false; }
    }

    public static bool SetBinary(string psPath, string name, byte[] data)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        try
        {
            using var k = root.CreateSubKey(sub, true);
            if (k == null) return false;
            k.SetValue(name, data, RegistryValueKind.Binary);
            return true;
        }
        catch { return false; }
    }

    // ───────── Batch setters: return true only if EVERY write succeeded ─────────
    // These let Apply functions honestly report success instead of lying.
    public static bool SetDWordAll(params (string path, string name, int value)[] entries)
    {
        bool allOk = true;
        foreach (var (path, name, value) in entries)
            if (!SetDWord(path, name, value)) allOk = false;
        return allOk;
    }

    public static bool SetStringAll(params (string path, string name, string value)[] entries)
    {
        bool allOk = true;
        foreach (var (path, name, value) in entries)
            if (!SetString(path, name, value)) allOk = false;
        return allOk;
    }

    public static bool SetAdapterDWords(string name, int value)
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", true);
            if (k == null) return false;
            foreach (var sub in k.GetSubKeyNames())
            {
                using var subk = k.OpenSubKey(sub, true);
                subk?.SetValue(name, value, RegistryValueKind.DWord);
            }
            return true;
        }
        catch { return false; }
    }

    public static bool DeleteAdapterValues(string name)
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", true);
            if (k == null) return false;
            foreach (var sub in k.GetSubKeyNames())
            {
                using var subk = k.OpenSubKey(sub, true);
                if (subk != null && subk.GetValue(name) != null)
                    subk.DeleteValue(name, false);
            }
            return true;
        }
        catch { return false; }
    }

    // ───────── Value removers ─────────
    public static bool RemoveValue(string psPath, string name)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        try
        {
            using var k = root.OpenSubKey(sub, true);
            if (k == null) return true; // key absent -> value absent -> success
            if (k.GetValue(name) != null) k.DeleteValue(name, false);
            return true;
        }
        catch { return false; }
    }

    // ───────── Value getters ─────────
    public static int? GetDWord(string psPath, string name)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return null;
        try
        {
            using var k = root.OpenSubKey(sub, false);
            if (k == null) return null;
            var v = k.GetValue(name);
            if (v is int i) return i;
            if (v is long l) return (int)l;
            return null;
        }
        catch { return null; }
    }

    public static string GetString(string psPath, string name)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return null;
        try
        {
            using var k = root.OpenSubKey(sub, false);
            return k?.GetValue(name) as string;
        }
        catch { return null; }
    }

    public static object GetValue(string psPath, string name)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return null;
        try
        {
            using var k = root.OpenSubKey(sub, false);
            return k?.GetValue(name);
        }
        catch { return null; }
    }

    // True if the DWORD value exists and equals the expected value.
    public static bool DWordEquals(string psPath, string name, int expected)
        => GetDWord(psPath, name) == expected;

    // Get a BINARY (REG_BINARY) value as a byte[] (returns null if missing or wrong type).
    public static byte[] GetBinary(string psPath, string name)
    {
        var v = GetValue(psPath, name);
        return v as byte[];
    }

    public static bool DWordEquals(string psPath, string name, uint expected)
        => GetDWord(psPath, name) == (int)expected;

    // True if the DWORD value exists and equals ANY of the expected values
    // (useful when a tweak accepts multiple "applied" states).
    public static bool DWordIn(string psPath, string name, params int[] expected)
    {
        var v = GetDWord(psPath, name);
        if (v == null) return false;
        foreach (var e in expected) if (v == e) return true;
        return false;
    }

    // True if a string value exists and equals the expected value.
    public static bool StringEquals(string psPath, string name, string expected)
        => string.Equals(GetString(psPath, name), expected, StringComparison.OrdinalIgnoreCase);

    // True if a registry value exists (regardless of its value).
    public static bool ValueExists(string psPath, string name)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        try
        {
            using var k = root.OpenSubKey(sub, false);
            return k?.GetValue(name) != null;
        }
        catch { return false; }
    }

    // True if a registry key exists (not a value — the key itself).
    public static bool KeyExists(string psPath)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        try
        {
            using var k = root.OpenSubKey(sub, false);
            return k != null;
        }
        catch { return false; }
    }

    // Create a registry key (and parents) if it doesn't exist.
    public static bool EnsureKey(string psPath)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        try
        {
            using var k = root.CreateSubKey(sub, true);
            return k != null;
        }
        catch { return false; }
    }

    // Delete a registry key and all its subkeys.
    public static bool DeleteKey(string psPath)
    {
        var (root, sub) = Parse(psPath);
        if (root == null) return false;
        try
        {
            root.DeleteSubKeyTree(sub, false);
            return true;
        }
        catch { return false; }
    }

    public static (RegistryKey root, string subkey) Parse(string psPath)
    {
        if (string.IsNullOrEmpty(psPath)) return (null, null);
        string prefix; RegistryKey root;
        if (psPath.StartsWith("HKLM:\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKLM:\\"; root = Registry.LocalMachine; }
        else if (psPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKLM\\"; root = Registry.LocalMachine; }
        else if (psPath.StartsWith("HKCU:\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKCU:\\"; root = Registry.CurrentUser; }
        else if (psPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKCU\\"; root = Registry.CurrentUser; }
        else if (psPath.StartsWith("HKCR:\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKCR:\\"; root = Registry.ClassesRoot; }
        else if (psPath.StartsWith("HKCR\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKCR\\"; root = Registry.ClassesRoot; }
        else if (psPath.StartsWith("HKU:\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKU:\\"; root = Registry.Users; }
        else if (psPath.StartsWith("HKU\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKU\\"; root = Registry.Users; }
        else if (psPath.StartsWith("HKCC:\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKCC:\\"; root = Registry.CurrentConfig; }
        else if (psPath.StartsWith("HKCC\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKCC\\"; root = Registry.CurrentConfig; }
        else if (psPath.StartsWith("HKEY_LOCAL_MACHINE\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKEY_LOCAL_MACHINE\\"; root = Registry.LocalMachine; }
        else if (psPath.StartsWith("HKEY_CURRENT_USER\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKEY_CURRENT_USER\\"; root = Registry.CurrentUser; }
        else if (psPath.StartsWith("HKEY_CLASSES_ROOT\\", StringComparison.OrdinalIgnoreCase))
        { prefix = "HKEY_CLASSES_ROOT\\"; root = Registry.ClassesRoot; }
        else return (null, null);

        return (root, psPath.Substring(prefix.Length).TrimStart('\\'));
    }
}
