<#
.SYNOPSIS
Recrea la base local descartable (borra tablas, aplica migraciones y siembra cuentas demo).
.DESCRIPTION
Lee la misma ConnectionStrings__Berruti privada que start-api-local.ps1 desde
src/Api/API-Berruti/API-Berruti/.env. En Development pide la contrasena ficticia de las
cuentas demo (pasajero, cobrador y admin) sin mostrarla ni guardarla. Solo funciona con
las bases locales que acepta el guard de la API. Borra TODOS los datos de esa base.
.PARAMETER Environment
Development (berrutilocal, con cuentas demo) o AuthenticationTests (berrutiauthtests, solo esquema).
.EXAMPLE
.\scripts\reset-local-db.ps1
#>
param([ValidateSet('Development', 'AuthenticationTests')][string]$Environment = 'Development')
$ErrorActionPreference = 'Stop'
$key = 'ConnectionStrings__Berruti'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src/Api/API-Berruti/API-Berruti/API-Berruti.csproj'
$file = Join-Path (Split-Path $project -Parent) '.env'

$value = $null
try {
    foreach ($line in [IO.File]::ReadAllLines($file)) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) { continue }
        if ($line.StartsWith($key + '=', [StringComparison]::Ordinal)) { $value = $line.Substring($key.Length + 1) }
    }
    if ([string]::IsNullOrWhiteSpace($value)) { throw 'Invalid configuration' }
} catch {
    [Console]::Error.WriteLine('Reinicio local: configuracion .env invalida o no disponible.')
    exit 2
}

$names = @($key, 'DemoSeed__Contrasena', 'ASPNETCORE_ENVIRONMENT')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$code = 2
try {
    [Environment]::SetEnvironmentVariable($key, $value, 'Process')
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', $Environment, 'Process')
    if ($Environment -eq 'Development') {
        $secure = Read-Host 'Contrasena ficticia para las cuentas demo' -AsSecureString
        $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try {
            [Environment]::SetEnvironmentVariable('DemoSeed__Contrasena',
                [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr), 'Process')
        } finally {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
    & dotnet run --project $project --no-launch-profile -- --reset-local
    $code = $LASTEXITCODE
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
exit $code
