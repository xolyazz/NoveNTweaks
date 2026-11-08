# NoveN Tweaks

An old Windows tweaking app I made with [Kl3ban](https://github.com/Kl3ban). I'm [xolyazz](https://github.com/xolyazz).

It's been sitting around for a while, but it still works pretty well, so I figured I'd put the source here. I've cleaned up the project and removed the old license check. The original UI is still there.

Built with C# and WPF on .NET 8, for Windows 10/11 x64.

## What's in it

There are 400+ tweaks and actions across privacy, performance, debloating, updates, networking, DNS, services, security, appearance, and context menus. Some are simple Windows preferences; others change registry values, disable services, or remove built-in apps.

You can search for tweaks, check their detected state, and apply or undo individual changes where supported. There's also an optimization wizard with Minimal, Normal, and FullOpti selections if you want a starting point. Read through the choices before applying them, especially FullOpti.

The app also has:

- An app installer using winget, useful when setting up Windows.
- A dashboard with hardware info and CPU, GPU, and RAM usage.
- Restore point controls and backup-related tools.
- Settings import and export.
- Dark and light themes, plus multiple languages including English and Polish.

## This version

These are the cleaned v1.4 sources. No bundled EXE, bin/obj folders, or obfuscation.

The old license server is no longer used. Just type anything into the welcome screen and press Enter or click Continue. The field can't be blank. It only remembers that you've passed the screen, not what you typed. Resetting the app settings brings it back.

## Build

You'll need Windows x64 and the .NET 8 SDK.

```powershell
dotnet build NoveNTweaks.sln -c Release
```

Or run `build.bat`. The output goes to:

```text
NoveNTweaks/bin/x64/Release/net8.0-windows/win-x64/
```

Run `publish.bat` if you want a standalone EXE with the .NET runtime included. That output goes to `artifacts/publish/`.

## Before you use it

The app runs as administrator and changes Windows settings. Make a restore point first and read what you're enabling. Some options turn off Windows features or security protections, so don't just select everything.

It's an older project. Some tweaks may behave differently on newer Windows builds, and I wouldn't expect every option to help every PC.

## Credits

Made by [xolyazz](https://github.com/xolyazz) and [Kl3ban](https://github.com/Kl3ban).

Existing credits for included modules are kept in the code. Third-party app icons belong to their respective owners.
