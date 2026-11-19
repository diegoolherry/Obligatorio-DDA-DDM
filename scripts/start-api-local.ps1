<#
.SYNOPSIS
Inicia solo la API con configuracion privada local opcional al arranque nativo.
.DESCRIPTION
Lee src/Api/API-Berruti/API-Berruti/.env relativo al repositorio del script.
Acepta lineas vacias, comentarios # y exactamente una vez cada una de estas claves:
ConnectionStrings__Berruti=valor y Jwt__Key=valor (clave HS256 de al menos 32 bytes,
fuera de Git). No admite export, comillas exteriores, interpolacion ni otras claves.
Los valores se transmiten literalmente por entorno Process, nunca como argumento de
dotnet, y se restauran al terminar. El archivo es texto plano, no una boveda.
No cambia cuentas ni evita el guard API.
.PARAMETER Profile
Perfil existente https (predeterminado) o http (solo scaffold, no credenciales).
.EXAMPLE
.\scripts\start-api-local.ps1
#>
param([string]$Profile = 'https')
$ErrorActionPreference = 'Stop'
$keys = @('ConnectionStrings__Berruti', 'Jwt__Key')
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src/Api/API-Berruti/API-Berruti/API-Berruti.csproj'
$file = Join-Path (Split-Path $project -Parent) '.env'
$values = @{}
try {
    if ($Profile -cnotin @('https', 'http')) { throw 'Invalid configuration' }
    foreach ($line in [IO.File]::ReadAllLines($file)) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) { continue }
        $matched = $null
        foreach ($key in $keys) {
            if ($line.StartsWith($key + '=', [StringComparison]::Ordinal)) { $matched = $key }
        }
        if ($null -eq $matched -or $values.ContainsKey($matched)) { throw 'Invalid configuration' }
        $value = $line.Substring($matched.Length + 1)
        $trimmed = $value.Trim()
        if ([string]::IsNullOrWhiteSpace($value) -or $value.Contains([string][char]0) -or
            $trimmed.StartsWith('"') -or $trimmed.EndsWith('"') -or
            $trimmed.StartsWith("'") -or $trimmed.EndsWith("'")) { throw 'Invalid configuration' }
        $values[$matched] = $value
    }
    foreach ($key in $keys) { if (!$values.ContainsKey($key)) { throw 'Invalid configuration' } }
} catch {
    [Console]::Error.WriteLine('API local: configuracion .env invalida o no disponible.')
    exit 2
}
$previous = @{}
foreach ($key in $keys) { $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }
$code = 2
try {
    foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $values[$key], 'Process') }
    & dotnet run --project $project --launch-profile $Profile
    $code = $LASTEXITCODE
} catch {
    [Console]::Error.WriteLine('API local: no se pudo iniciar dotnet.')
} finally {
    foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process') }
}
exit $code
