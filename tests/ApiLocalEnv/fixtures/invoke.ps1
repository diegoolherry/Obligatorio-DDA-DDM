param([string]$Launcher, [string]$Capture, [string]$Profile = 'https',
    [int]$ChildExit = 0, [switch]$AbsentPrevious, [switch]$ThrowChild)
$ErrorActionPreference = 'Stop'
$env:ConnectionStrings__Berruti = 'PREVIOUS_SYNTHETIC'
if ($AbsentPrevious) { $env:ConnectionStrings__Berruti = $null }
$env:API_ENV_TEST_OTHER = 'UNCHANGED'
$global:probe = @{ Called = $false; Value = $null; Arguments = @() }
function global:dotnet {
    $global:probe.Called = $true
    $global:probe.Value = $env:ConnectionStrings__Berruti
    $global:probe.Arguments = @($args)
    if ($ThrowChild) { throw 'SYNTHETIC_CHILD_EXCEPTION' }
    [Console]::Out.WriteLine('FAKE_DOTNET')
    $global:LASTEXITCODE = $ChildExit
}
if (Test-Path -LiteralPath $Launcher) {
    if ($Profile -eq 'default') { & $Launcher }
    else { & $Launcher -Profile $Profile }
    $code = $LASTEXITCODE
} else {
    [Console]::Error.WriteLine('LAUNCHER_NOT_IMPLEMENTED')
    $code = 99
}
$global:probe.Restored = $env:ConnectionStrings__Berruti
$global:probe.Other = $env:API_ENV_TEST_OTHER
$global:probe |
    ConvertTo-Json -Compress | Set-Content -LiteralPath $Capture -Encoding UTF8
exit $code
