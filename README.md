# Windows Native Customizer

Windows 11 原生开始菜单、快捷设置定制，以及透明任务栏前景自动黑白切换。中文配置界面，GPL-3.0 开源。

**本版本依赖 Windhawk。** 安装包包含官方 Windhawk 1.7.3 离线安装程序，缺少时自动安装；用户无需另找依赖，但这不等于完全独立运行。配置器关闭后无需常驻，实际模块由 Windhawk 后台服务加载。

## 下载与使用

从 [Releases](https://github.com/ZzzXR0305/windows-native-customizer/releases) 下载：

- `Taskbar-Setup-3.6.2-x64.exe`：任务栏自动黑白配置入口。
- `Menu-Setup-3.6.2-x64.exe`：原生开始菜单与快捷设置配置入口。

安装需要管理员权限。两种入口共用同一份配置程序和模块，可以分别安装。安装后从桌面入口打开，点击启用/应用；需要恢复时使用各页面的恢复按钮。首次启用任务栏模块会重启 Explorer 一次，保存好正在操作的文件窗口。

任务栏右侧的时间、日期、语言和系统托盘前景随背景切换黑白；提醒和麦克风保留不同颜色。模块在任务栏附近采样物理像素、每 250 ms 检查，两次稳定结果后更新画刷。透明背景需自行安装 [TranslucentTB](https://github.com/TranslucentTB/TranslucentTB) 等工具，本软件不负责提供透明效果。

## 兼容范围与已有 Windhawk

只面向 **Windows 11 x64**。开发电脑为 Windows 11 25H2，build 26200.9278；尚未在其他机器完成安装实测，不保证 Windows 10、ARM64 或所有未来 Windows 更新兼容。Windows 内部界面更新可能影响模块。

为限制加载范围，程序只接受 `StartMenuExperienceHost.exe`、`ShellHost.exe`、`explorer.exe` 这三个明确的进程名，其他进程排除。SearchHost 保持原生。

若已装 Windhawk 且使用其他进程的模块，安装器会停止并保留现有设置。不要为此直接覆盖自己的 Windhawk 配置；请先备份，在专用于本套件的测试电脑/虚拟机验证，或等待支持与其他模块共存的版本。首次安装的 Windhawk 默认不启用样式，配置器按所选功能启用模块。

卸载只禁用对应样式；已加载样式在下次登录退出。共享 Windhawk、模块文件和个人配置保留，避免影响另一个入口。可在确认不再需要后，通过 Windows 的已安装应用单独卸载 Windhawk。

## 从源码构建

需要 Windows 11 x64、系统自带 .NET Framework 4.x 编译器、Windhawk **1.7.3** 的编译器/引擎，以及 [NSIS 3.13](https://nsis.sourceforge.io/Download)。源码和构建脚本均在仓库根目录。

1. 下载官方 [Windhawk 1.7.3 离线安装程序](https://github.com/ramensoftware/windhawk/releases/download/v1.7.3/windhawk_setup_offline.exe)。SHA-256：`4D93016570F982326EEBDFC9068E924FF21F6448534EA32672BB4C1D52A8193B`。
2. 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Nsis "C:\path\to\makensis.exe" -OfflineSetup "C:\path\to\windhawk_setup_offline.exe"`。Windhawk 路径可通过 `-Windhawk` 指定。
3. 安装包生成在源码目录的上一级。仅编译配置器可加 `-AppOnly`。

任务栏 DLL 编译保留 `--export-all-symbols`，必须导出 Windhawk 生命周期入口。旧覆盖层/外部观察器代码保留供回溯，当前发布不包含该旧观察器可执行文件。

## 验证与许可证

本次发布会检查 C# 和三个 C++ 模块从公开源码编译、DLL 入口导出、安装器编译和包内文件，并扫描个人路径与密钥。未在开发电脑执行安装/卸载，以免打断正在运行的 Windows 桌面；这些检查不等于新电脑的安装实测。欢迎通过 Issues 提供 Windows 版本及问题，上传日志前请去除个人信息。

见 [LICENSE](LICENSE) 与 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。源自 Windhawk 模块的代码保留原作者署名。Windhawk 官方程序有其自身许可证和第三方依赖声明。
