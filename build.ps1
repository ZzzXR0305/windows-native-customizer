param(
    [string]$Windhawk = "$env:ProgramFiles\Windhawk",
    [string]$Nsis = "$PSScriptRoot\..\tools\nsis-3.13\makensis.exe",
    [string]$OfflineSetup = "$PSScriptRoot\..\tools\windhawk_setup_offline.exe",
    [switch]$AppOnly
)
$ErrorActionPreference = 'Stop'
$payload = Join-Path $PSScriptRoot '..\payload'
$mods = Join-Path $payload '运行组件\Mod'
New-Item -ItemType Directory -Path $mods -Force | Out-Null
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
& "$framework\csc.exe" /nologo /target:winexe /platform:x64 /optimize+ "/out:$payload\NativeStartCustomizer.exe" "/win32icon:$PSScriptRoot\native-start.ico" "/win32manifest:$PSScriptRoot\app.manifest" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.ServiceProcess.dll "/reference:$framework\WPF\UIAutomationClient.dll" "/reference:$framework\WPF\UIAutomationTypes.dll" "/reference:$framework\WPF\WindowsBase.dll" "$PSScriptRoot\NativeStartCustomizer.cs"
if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed.' }
if ($AppOnly) { return }
$compiler = Join-Path $Windhawk 'Compiler\bin\clang++.exe'
$modSpecs = @(
    @('native-taskbar-adaptive-contrast', '1.2', @('-lgdi32','-luuid')),
    @('windows-11-start-menu-styler', '1.6', @('-lversion')),
    @('windows-11-notification-center-styler', '1.6', @())
)
foreach ($spec in $modSpecs) {
    $id = $spec[0]; $version = $spec[1]
    $flags = @('-std=c++23','-O2','-shared','-DUNICODE','-D_UNICODE','-DWH_MOD', '-DWINVER=0x0A00','-D_WIN32_WINNT=0x0A00','-D_WIN32_IE=0x0A00','-DNTDDI_VERSION=0x0A000008','-DWH_WINDHAWK_VERSION=0x01070300', "-DWH_MOD_ID=L`"$id`"", "-DWH_MOD_VERSION=L`"$version`"", '-target','x86_64-w64-mingw32','-include','windhawk_api.h','-Wl,--export-all-symbols', "$Windhawk\Engine\1.7.3\64\windhawk.lib", "$PSScriptRoot\$id.wh.cpp", '-o', "$mods\${id}_${version}_64.dll", '-lcomctl32','-lole32','-loleaut32','-lruntimeobject') + $spec[2]
    & $compiler @flags
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $id" }
}
Copy-Item -LiteralPath "$PSScriptRoot\LICENSE","$PSScriptRoot\README.md","$PSScriptRoot\THIRD-PARTY-NOTICES.md","$PSScriptRoot\install-runtime.ps1" -Destination $payload -Force
if (-not (Test-Path -LiteralPath $OfflineSetup)) { throw 'Download the official Windhawk 1.7.3 offline installer first; see README.' }
if ((Get-FileHash -LiteralPath $OfflineSetup).Hash -ne '4D93016570F982326EEBDFC9068E924FF21F6448534EA32672BB4C1D52A8193B') { throw 'Windhawk installer SHA-256 mismatch.' }
Copy-Item -LiteralPath $OfflineSetup -Destination "$payload\windhawk_setup_offline.exe" -Force
foreach ($flavor in @('Taskbar','Menu')) {
    & $Nsis /INPUTCHARSET UTF8 "/DFLAVOR=$flavor" "$PSScriptRoot\setup.nsi"
    if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed: $flavor" }
}
