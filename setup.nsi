Unicode true
!include "MUI2.nsh"
!include "x64.nsh"
!define VERSION "3.6.2"
!if "${FLAVOR}" == "Taskbar"
!define PRODUCT "任务栏自动黑白"
!define ARG "--taskbar"
!else
!define PRODUCT "Windows 原生菜单定制器"
!define ARG ""
!endif
Name "${PRODUCT} ${VERSION}"
OutFile "..\${FLAVOR}-Setup-${VERSION}-x64.exe"
InstallDir "$PROGRAMFILES64\WindowsNativeCustomizer-${FLAVOR}"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
VIProductVersion "3.6.2.0"
VIAddVersionKey /LANG=1033 "ProductName" "${PRODUCT}"
VIAddVersionKey /LANG=1033 "FileDescription" "${PRODUCT} installer"
VIAddVersionKey /LANG=1033 "FileVersion" "${VERSION}"
VIAddVersionKey /LANG=1033 "LegalCopyright" "GPL-3.0; see included notices"
!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_TEXT "安装完成。请从桌面入口打开配置器并启用功能。此版本依赖 Windhawk，缺少时已安装官方离线版。透明任务栏背景由 TranslucentTB 等工具提供。首次应用可能重新启动相关 Windows 界面。"
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"
!insertmacro MUI_LANGUAGE "English"
Section "Install"
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP "Requires Windows 11 x64."
    Abort
  ${EndIf}
  SetRegView 64
  InitPluginsDir
  SetOutPath "$PLUGINSDIR"
  File "install-runtime.ps1"
  nsExec::ExecToLog '"$WINDIR\sysnative\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$PLUGINSDIR\install-runtime.ps1" -Bundle "$INSTDIR" -Preflight'
  Pop $0
  ${If} $0 != 0
    MessageBox MB_ICONSTOP "无法安装：需要 Windows 11 x64，且已有 Windhawk 必须处于本套件支持的进程范围。详细原因：%TEMP%\WindowsNativeCustomizer-install.log。原有设置未覆盖。"
    Abort
  ${EndIf}
  SetOutPath "$INSTDIR"
  File /r "..\payload\*.*"
  nsExec::ExecToLog '"$WINDIR\sysnative\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$INSTDIR\install-runtime.ps1" -Bundle "$INSTDIR"'
  Pop $0
  ${If} $0 != 0
    MessageBox MB_ICONSTOP "运行组件安装未完成。请查看 %TEMP%\WindowsNativeCustomizer-install.log 并重新运行安装包。"
    Abort
  ${EndIf}
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  SetShellVarContext all
  CreateShortcut "$DESKTOP\${PRODUCT}.lnk" "$INSTDIR\NativeStartCustomizer.exe" "${ARG}"
  CreateDirectory "$SMPROGRAMS\Windows Native Customizer"
  CreateShortcut "$SMPROGRAMS\Windows Native Customizer\${PRODUCT}.lnk" "$INSTDIR\NativeStartCustomizer.exe" "${ARG}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsNativeCustomizer-${FLAVOR}" "DisplayName" "${PRODUCT}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsNativeCustomizer-${FLAVOR}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsNativeCustomizer-${FLAVOR}" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsNativeCustomizer-${FLAVOR}" "InstallLocation" "$INSTDIR"
SectionEnd
Section "Uninstall"
  SetRegView 64
  MessageBox MB_OKCANCEL "卸载会禁用本安装包对应的样式。已加载的样式将在下次登录后退出；现在恢复请先使用配置器里的恢复按钮。共享的 Windhawk 和个人配置会保留。" IDOK +2
  Abort
  nsExec::ExecToLog '"$WINDIR\sysnative\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$INSTDIR\install-runtime.ps1" -Bundle "$INSTDIR" -Remove ${FLAVOR}'
  Pop $0
  ${If} $0 != 0
    MessageBox MB_ICONSTOP "无法禁用模块，卸载已停止。请先使用配置器恢复。"
    Abort
  ${EndIf}
  SetShellVarContext all
  Delete "$DESKTOP\${PRODUCT}.lnk"
  Delete "$SMPROGRAMS\Windows Native Customizer\${PRODUCT}.lnk"
  RMDir "$SMPROGRAMS\Windows Native Customizer"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsNativeCustomizer-${FLAVOR}"
  Delete "$INSTDIR\NativeStartCustomizer.exe"
  Delete "$INSTDIR\LICENSE"
  Delete "$INSTDIR\README.md"
  Delete "$INSTDIR\THIRD-PARTY-NOTICES.md"
  Delete "$INSTDIR\install-runtime.ps1"
  Delete "$INSTDIR\windhawk_setup_offline.exe"
  Delete "$INSTDIR\运行组件\Mod\native-taskbar-adaptive-contrast_1.2_64.dll"
  Delete "$INSTDIR\运行组件\Mod\windows-11-start-menu-styler_1.6_64.dll"
  Delete "$INSTDIR\运行组件\Mod\windows-11-notification-center-styler_1.6_64.dll"
  RMDir "$INSTDIR\运行组件\Mod"
  RMDir "$INSTDIR\运行组件"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
SectionEnd
