# Release validation: 3.6.2

Date: 2026-10-06. Build host: Windows 11 25H2 x64, build 26200.9278.

- The published C# source compiles to the 3.6.2.0 x64 configuration application using .NET Framework 4.x.
- All three published C++ sources compile using the Windhawk 1.7.3 compiler and engine import library.
- All three DLLs are x64 and export `_Z10Wh_ModInitv`, `_Z15Wh_ModAfterInitv` and `_Z12Wh_ModUninitv`, matching Windhawk's loader.
- DLL imports use Windows components plus Windhawk's engine and its `libc++.whl` / `libunwind.whl` runtime libraries. The official offline Windhawk installer supplies these runtime libraries.
- The installer PowerShell script parses and its read-only preflight passes on the build computer.
- Both NSIS installers compile without warnings; their stored CRC-32 equals the calculated CRC over the installer contents.
- A configuration-window render exits successfully. Source scanning finds no development-user paths or common credential patterns.

Installer SHA-256:

```
Taskbar-Setup-3.6.2-x64.exe
5B298DDABE34EF066F3EC8788D67F4EE70E0EFED8BA75A711AED50A8796A9AC4

Menu-Setup-3.6.2-x64.exe
128EB35A9C34BC289DD51744D7BD8EA172C82DBC40E7B4E54CFFA2CA29319CC1
```

Limits: the installers were not executed on the active desktop, and clean-machine install, enable, uninstall and Windows-update compatibility tests remain outstanding. Compilation and integrity checks alone do not establish those results. These packages are intended for an initial testing release.
