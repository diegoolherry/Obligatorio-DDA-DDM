param([string]$Launcher, [string]$Capture, [string]$Profile = 'https',
    [int]$ChildExit = 0, [switch]$AbsentPrevious, [switch]$ThrowChild)
$ErrorActionPreference = 'Stop'
$env:ConnectionStrings__Berruti = 'PREVIOUS_SYNTHETIC'
$env:Jwt__Key = 'PREVIOUS_JWT_SYNTHETIC'
if ($AbsentPrevious) { $env:ConnectionStrings__Berruti = $null; $env:Jwt__Key = $null }
$mpKeys = @('MercadoPago__AccessToken', 'MercadoPago__ExpectedSellerId',
    'MercadoPago__ExpectedSiteId', 'MercadoPago__TestCredentialOriginConfirmed')
foreach ($k in $mpKeys) {
    if ($AbsentPrevious) { [Environment]::SetEnvironmentVariable($k, $null, 'Process') }
    else { [Environment]::SetEnvironmentVariable($k, 'PREVIOUS_MP_' + $k, 'Process') }
}
$env:API_ENV_TEST_OTHER = 'UNCHANGED'
$global:probe = @{ Called = $false; Value = $null; JwtValue = $null; Arguments = @(); Mp = @{}; MpRestored = @{} }
function global:dotnet {
    $global:probe.Called = $true
    $global:probe.Value = $env:ConnectionStrings__Berruti
    $global:probe.JwtValue = $env:Jwt__Key
    $global:probe.Arguments = @($args)
    foreach ($k in $mpKeys) { $global:probe.Mp[$k] = [Environment]::GetEnvironmentVariable($k, 'Process') }
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
$global:probe.JwtRestored = $env:Jwt__Key
foreach ($k in $mpKeys) { $global:probe.MpRestored[$k] = [Environment]::GetEnvironmentVariable($k, 'Process') }
$global:probe.Other = $env:API_ENV_TEST_OTHER
$global:probe |
    ConvertTo-Json -Compress | Set-Content -LiteralPath $Capture -Encoding UTF8
exit $code
