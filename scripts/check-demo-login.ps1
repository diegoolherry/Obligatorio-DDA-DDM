<#
.SYNOPSIS
Prueba el ingreso real de las cuentas demo contra la API local, sin mostrar la contrasena.
.DESCRIPTION
-Save pide la contrasena ficticia de las cuentas demo y la guarda cifrada con DPAPI
(solo tu usuario de Windows puede descifrarla) en .local/demo-password.dpapi, fuera de git.
Sin -Save, la descifra en memoria y verifica por HTTP: ingreso, operacion permitida al rol,
operacion denegada a otro rol, acceso sin sesion, credenciales rechazadas, salida y token
revocado. Imprime solo codigos HTTP; nunca la contrasena ni el token.
.EXAMPLE
.\scripts\check-demo-login.ps1 -Save
.\scripts\check-demo-login.ps1
#>
param([switch]$Save, [string]$BaseUrl = 'https://localhost:7053')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$file = Join-Path $root '.local/demo-password.dpapi'

if ($Save) {
    New-Item -ItemType Directory -Force (Split-Path $file -Parent) | Out-Null
    $secure = Read-Host 'Contrasena ficticia de las cuentas demo' -AsSecureString
    ConvertFrom-SecureString $secure | Set-Content -Path $file -Encoding ascii
    Write-Host 'Contrasena demo guardada cifrada para este usuario de Windows.'
    exit 0
}
if (!(Test-Path $file)) {
    [Console]::Error.WriteLine('Falta la contrasena demo: ejecuta primero .\scripts\check-demo-login.ps1 -Save')
    exit 2
}

$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR((Get-Content $file | ConvertTo-SecureString))
try { $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }

$script:failures = 0

function Invoke-Api([string]$Method, [string]$Path, [string]$Token, $Body) {
    $request = @{ Method = $Method; Uri = "$BaseUrl/api/autenticacion/$Path"; UseBasicParsing = $true }
    if ($Token) { $request.Headers = @{ Authorization = "Bearer $Token" } }
    if ($null -ne $Body) { $request.Body = ($Body | ConvertTo-Json); $request.ContentType = 'application/json' }
    try {
        $response = Invoke-WebRequest @request
        return @{ Status = [int]$response.StatusCode; Content = $response.Content }
    } catch [System.Net.WebException] {
        if ($null -eq $_.Exception.Response) { return @{ Status = 0; Content = $null } }
        return @{ Status = [int]$_.Exception.Response.StatusCode; Content = $null }
    }
}

function Assert-Status([string]$Name, [int]$Expected, [int]$Actual) {
    if ($Expected -eq $Actual) { Write-Host "PASS $Name ($Actual)" }
    else { Write-Host "FAIL $Name (esperado $Expected, obtenido $Actual)"; $script:failures++ }
}

$probes = @{ Pasajero = 'prueba-pasajero'; Cobrador = 'prueba-cobrador'; Administrador = 'prueba-administrador' }
$accounts = @(
    @{ Role = 'Pasajero'; Email = 'pasajero.demo@example.test'; Denied = 'Cobrador' },
    @{ Role = 'Cobrador'; Email = 'cobrador.demo@example.test'; Denied = 'Pasajero' },
    @{ Role = 'Administrador'; Email = 'admin.demo@example.test'; Denied = 'Pasajero' }
)

$anonymous = Invoke-Api 'GET' 'prueba-pasajero'
if ($anonymous.Status -eq 0) {
    [Console]::Error.WriteLine("API no disponible en $BaseUrl. Arrancala con .\scripts\start-api-local.ps1")
    exit 2
}
Assert-Status 'sin sesion: operacion protegida' 401 $anonymous.Status

foreach ($account in $accounts) {
    $role = $account.Role
    $rejected = Invoke-Api 'POST' 'ingreso' $null @{ correo = $account.Email; contrasena = 'contrasena-incorrecta' }
    Assert-Status "${role}: credenciales rechazadas" 401 $rejected.Status

    $login = Invoke-Api 'POST' 'ingreso' $null @{ correo = $account.Email; contrasena = $password }
    Assert-Status "${role}: ingreso" 200 $login.Status
    if ($login.Status -ne 200) { continue }
    $token = ($login.Content | ConvertFrom-Json).token

    Assert-Status "${role}: operacion de su rol" 200 (Invoke-Api 'GET' $probes[$role] $token).Status
    Assert-Status "${role}: operacion de $($account.Denied) denegada" 403 (Invoke-Api 'GET' $probes[$account.Denied] $token).Status
    Assert-Status "${role}: salida" 204 (Invoke-Api 'POST' 'salida' $token).Status
    Assert-Status "${role}: token revocado tras la salida" 401 (Invoke-Api 'GET' $probes[$role] $token).Status
    $token = $null
}

$password = $null
if ($script:failures -gt 0) { Write-Host "$($script:failures) comprobaciones fallaron."; exit 1 }
Write-Host 'Todas las comprobaciones pasaron.'
exit 0
