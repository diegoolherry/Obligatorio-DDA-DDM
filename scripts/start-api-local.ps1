<#
.SYNOPSIS
Inicia solo la API con configuracion privada local opcional al arranque nativo.
.DESCRIPTION
Lee src/Api/API-Berruti/API-Berruti/.env relativo al repositorio del script.
Acepta lineas vacias, comentarios # y una unica ConnectionStrings__Berruti=valor.
No admite export, comillas exteriores, interpolacion ni otras claves. El valor
se transmite literalmente por entorno Process, nunca como argumento de dotnet.
El archivo es texto plano, no una boveda. No cambia cuentas ni evita el guard API.
.PARAMETER Profile
Perfil existente https (predeterminado) o http (solo scaffold, no credenciales).
.EXAMPLE
.\scripts\start-api-local.ps1
#>
param([string]$Profile = 'https')
$ErrorActionPreference = 'Stop'
$key = 'ConnectionStrings__Berruti'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src/Api/API-Berruti/API-Berruti/API-Berruti.csproj'
$file = Join-Path (Split-Path $project -Parent) '.env'
$value = $null
try {
    if ($Profile -cnotin @('https', 'http')) { throw 'Invalid configuration' }
    foreach ($line in [IO.File]::ReadAllLines($file)) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) { continue }
        $prefix = $key + '='
        if (!$line.StartsWith($prefix, [StringComparison]::Ordinal) -or $null -ne $value) {
            throw 'Invalid configuration'
        }
        $value = $line.Substring($prefix.Length)
        $trimmed = $value.Trim()
        if ([string]::IsNullOrWhiteSpace($value) -or $value.Contains([string][char]0) -or
            $trimmed.StartsWith('"') -or $trimmed.EndsWith('"') -or
            $trimmed.StartsWith("'") -or $trimmed.EndsWith("'")) { throw 'Invalid configuration' }
    }
    if ($null -eq $value) { throw 'Invalid configuration' }
} catch {
    [Console]::Error.WriteLine('API local: configuracion .env invalida o no disponible.')
    exit 2
}
$previous = [Environment]::GetEnvironmentVariable($key, 'Process')
$code = 2
try {
    [Environment]::SetEnvironmentVariable($key, $value, 'Process')
    & dotnet run --project $project --launch-profile $Profile
    $code = $LASTEXITCODE
} catch {
    [Console]::Error.WriteLine('API local: no se pudo iniciar dotnet.')
} finally {
    [Environment]::SetEnvironmentVariable($key, $previous, 'Process')
}
exit $code
