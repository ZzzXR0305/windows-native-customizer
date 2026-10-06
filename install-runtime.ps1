param([Parameter(Mandatory=$true)][string]$Bundle, [switch]$Preflight, [ValidateSet('Taskbar','Menu')][string]$Remove)
$ErrorActionPreference = 'Stop'
$log = Join-Path $env:TEMP 'WindowsNativeCustomizer-install.log'
try {
    if (-not [Environment]::Is64BitOperatingSystem) { throw 'Requires Windows 11 x64.' }
    $build = [int](Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion').CurrentBuild
    if ($build -lt 22000) { throw 'Requires Windows 11.' }
    $enginePath = 'SOFTWARE\Windhawk\Engine\Settings'
    $service = Get-Service Windhawk -ErrorAction SilentlyContinue
    if ($Remove) {
        $ids = if ($Remove -eq 'Taskbar') { @('native-taskbar-adaptive-contrast') } else { @('windows-11-start-menu-styler','windows-11-notification-center-styler') }
        foreach ($id in $ids) {
            $key = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey("SOFTWARE\Windhawk\Engine\Mods\$id", $true)
            if ($key) { try { $key.SetValue('Disabled',1,[Microsoft.Win32.RegistryValueKind]::DWord) } finally { $key.Dispose() } }
        }
        exit 0
    }
    if ($service) {
        $engine = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($enginePath)
        if (-not $engine) { throw 'Existing Windhawk is not isolated. See README; settings were not changed.' }
        try {
            $allowed = @('StartMenuExperienceHost.exe','ShellHost.exe','explorer.exe')
            $entries = ([string]$engine.GetValue('Include','')).Split('|') | Where-Object { $_.Trim() }
            if ([string]$engine.GetValue('Exclude','') -ne '*' -or ($entries | Where-Object { $_.Trim() -notin $allowed }) -or [int]$engine.GetValue('InjectIntoCriticalProcesses',0) -ne 0 -or [int]$engine.GetValue('InjectIntoIncompatiblePrograms',0) -ne 0 -or [int]$engine.GetValue('InjectIntoGames',0) -ne 0) {
                throw 'Existing Windhawk has a different injection scope. Installer will not replace your settings. See README.'
            }
        } finally { $engine.Dispose() }
    }
    if ($Preflight) { exit 0 }
    if (-not $service) {
        $setup = Join-Path $Bundle 'windhawk_setup_offline.exe'
        if ((Get-FileHash -LiteralPath $setup).Hash -ne '4D93016570F982326EEBDFC9068E924FF21F6448534EA32672BB4C1D52A8193B') { throw 'Official Windhawk installer hash mismatch.' }
        $engine = [Microsoft.Win32.Registry]::LocalMachine.CreateSubKey($enginePath)
        try {
            $engine.SetValue('Include','',[Microsoft.Win32.RegistryValueKind]::String)
            $engine.SetValue('Exclude','*',[Microsoft.Win32.RegistryValueKind]::String)
            foreach ($name in @('InjectIntoCriticalProcesses','InjectIntoIncompatiblePrograms','InjectIntoGames')) { $engine.SetValue($name,0,[Microsoft.Win32.RegistryValueKind]::DWord) }
        } finally { $engine.Dispose() }
        $result = Start-Process -FilePath $setup -ArgumentList "/S /D=$env:ProgramFiles\Windhawk" -WindowStyle Hidden -Wait -PassThru
        if ($result.ExitCode -ne 0) { throw "Windhawk installation failed: $($result.ExitCode)" }
        if (-not (Get-Service Windhawk -ErrorAction SilentlyContinue)) { throw 'Windhawk service was not installed.' }
        $engine = [Microsoft.Win32.Registry]::LocalMachine.CreateSubKey($enginePath)
        try {
            $engine.SetValue('Include','',[Microsoft.Win32.RegistryValueKind]::String)
            $engine.SetValue('Exclude','*',[Microsoft.Win32.RegistryValueKind]::String)
            foreach ($name in @('InjectIntoCriticalProcesses','InjectIntoIncompatiblePrograms','InjectIntoGames')) { $engine.SetValue($name,0,[Microsoft.Win32.RegistryValueKind]::DWord) }
        } finally { $engine.Dispose() }
    }
    $target = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'Windhawk\Engine\Mods\64'
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    $specs = @(@('native-taskbar-adaptive-contrast','1.2','explorer.exe'),@('windows-11-start-menu-styler','1.6','StartMenuExperienceHost.exe'),@('windows-11-notification-center-styler','1.6','ShellHost.exe'))
    foreach ($spec in $specs) {
        $id = $spec[0]; $version = $spec[1]; $file = "${id}_${version}_64.dll"
        $source = Join-Path $Bundle "运行组件\Mod\$file"; $destination = Join-Path $target $file
        if (-not (Test-Path -LiteralPath $destination) -or (Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) { Copy-Item -LiteralPath $source -Destination $destination -Force }
        $path = "SOFTWARE\Windhawk\Engine\Mods\$id"
        $existing = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($path)
        if ($existing) { $existing.Dispose(); continue }
        $key = [Microsoft.Win32.Registry]::LocalMachine.CreateSubKey($path)
        try {
            $key.SetValue('LibraryFileName',$file,[Microsoft.Win32.RegistryValueKind]::String)
            $key.SetValue('Disabled',1,[Microsoft.Win32.RegistryValueKind]::DWord)
            $key.SetValue('Include',$spec[2],[Microsoft.Win32.RegistryValueKind]::String)
            $key.SetValue('Architecture','x86-64',[Microsoft.Win32.RegistryValueKind]::String)
            $key.SetValue('Version',$version,[Microsoft.Win32.RegistryValueKind]::String)
        } finally { $key.Dispose() }
    }
    exit 0
} catch {
    $_.Exception.Message | Set-Content -LiteralPath $log -Encoding UTF8
    exit 1
}
