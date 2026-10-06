# Third-party notices

This project is distributed under GPL-3.0. Configuration and installer packaging changes: ZzzXR0305.

The three `.wh.cpp` modules are based on work by **Michael Maltsev (m417z)** and contributors to [ramensoftware/windhawk-mods](https://github.com/ramensoftware/windhawk-mods). Their original GPL-3.0 notices are retained. The adaptive taskbar module contains project-specific foreground sampling and brush changes; it derives from Windows 11 Taskbar Styler 1.8.

- Start Menu Styler 1.6 source: upstream commit `f03c2a238bb19ba9f0a9481d7697b8ddf14e4be8`.
- Taskbar Styler 1.8 base and Notification Center Styler 1.6 source snapshot: upstream commit `8be5d442cd3d958f58b91428d3cf9039536924bd`.

Installers include an unmodified, digitally signed **Windhawk 1.7.3 offline installer**. Upstream source for that release: [ramensoftware/windhawk, v1.7.3](https://github.com/ramensoftware/windhawk/tree/v1.7.3); [complete source archive](https://github.com/ramensoftware/windhawk/archive/refs/tags/v1.7.3.zip). Windhawk source is GPL-3.0 and includes components under other licenses; consult its LICENSE, dependencies and the official installer. Windhawk is developed by Ramen Software / Michael Maltsev; this project is an independent customization package.

NSIS is used to generate installers. Its licenses are documented at [NSIS license](https://nsis.sourceforge.io/License); NSIS itself is not bundled as a development tool in the application.

TranslucentTB is optional and separately obtained from its official project. It is not redistributed in these installers.
