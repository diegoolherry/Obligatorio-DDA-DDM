<#
.SYNOPSIS
Prepara una matriz HTTP local de autorizacion para las rutas operativas.
.DESCRIPTION
Pide la contrasena ficticia de las cuentas demo de forma segura, valida HTTPS de loopback
sin desactivar la validacion TLS y nunca imprime credenciales, tokens ni cuerpos de error.
Solo crea sesiones de autenticacion y las revoca en finally. No crea ni modifica datos
operativos. No ejecutar sin autorizacion para runtime, MySQL y configuracion local.
.EXAMPLE
.\scripts\check-operational-authorization.ps1 -BaseUrl https://localhost:7053
#>
param([string]$BaseUrl = 'https://localhost:7053')
$ErrorActionPreference = 'Stop'

$script:failures = 0
$script:client = $null
$script:tokens = @()
$script:password = $null

function Assert-Status([string]$Name, [int]$Expected, [int]$Actual) {
    if ($Expected -eq $Actual) { Write-Host "PASS $Name ($Actual)" }
    else { Write-Host "FAIL $Name (esperado $Expected, obtenido $Actual)"; $script:failures++ }
}

function Invoke-LocalApi([string]$Method, [string]$Path, [string]$Token, $Body, [switch]$ReadSuccessBody) {
    $request = $null
    $response = $null
    try {
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), [Uri]::new($script:baseUri, $Path))
        if ($Token) { $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $Token) }
        if ($null -ne $Body) { $request.Content = [System.Net.Http.StringContent]::new([string]$Body, [Text.Encoding]::UTF8, 'application/json') }
        $response = $script:client.SendAsync($request).GetAwaiter().GetResult()
        $content = $null
        if ($ReadSuccessBody -and [int]$response.StatusCode -eq 200) {
            $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        }
        return @{ Status = [int]$response.StatusCode; Content = $content }
    }
    catch {
        return @{ Status = 0; Content = $null }
    }
    finally {
        if ($null -ne $response) { $response.Dispose() }
        if ($null -ne $request) { $request.Dispose() }
    }
}

$accounts = @(
    @{ Role = 'Pasajero'; Email = 'pasajero.demo@example.test' },
    @{ Role = 'Cobrador'; Email = 'cobrador.demo@example.test' },
    @{ Role = 'Administrador'; Email = 'admin.demo@example.test' }
)
$operationalRoutes = @(
    '/api/administracion/paradas',
    '/api/administracion/tramos',
    '/api/administracion/servicios',
    '/api/administracion/tarifas'
)

try {
    try { $target = [Uri]$BaseUrl } catch { throw 'BaseUrl invalido.' }
    if (!$target.IsAbsoluteUri -or $target.Scheme -ne 'https' -or
        $target.Host -notin @('localhost', '127.0.0.1', '::1', '[::1]') -or
        $target.UserInfo -or $target.Query -or $target.Fragment -or $target.AbsolutePath -notin @('/', '')) {
        throw 'BaseUrl debe ser HTTPS en localhost y sin ruta, credenciales, consulta ni fragmento.'
    }
    $script:baseUri = [Uri]::new($target.GetLeftPart([UriPartial]::Authority) + '/')

    $securePassword = Read-Host 'Contrasena ficticia de las cuentas demo' -AsSecureString
    $passwordBstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try { $script:password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordBstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordBstr) }

    # HttpClientHandler conserva su validacion TLS predeterminada; no se instala callback permisivo.
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $handler.UseProxy = $false
    $handler.UseCookies = $false
    $script:client = [System.Net.Http.HttpClient]::new($handler)
    $script:client.Timeout = [TimeSpan]::FromSeconds(15)

    $anonymousStatuses = @{}
    foreach ($route in $operationalRoutes) {
        $result = Invoke-LocalApi 'GET' $route $null $null
        $anonymousStatuses[$route] = $result.Status
        Assert-Status "Anonimo $route" 401 $result.Status
    }

    $roleTokens = @{}
    foreach ($account in $accounts) {
        $loginJson = @{ correo = $account.Email; contrasena = $script:password } | ConvertTo-Json -Compress
        $login = Invoke-LocalApi 'POST' '/api/autenticacion/ingreso' $null $loginJson -ReadSuccessBody
        $loginJson = $null
        Assert-Status "$($account.Role) ingreso demo" 200 $login.Status
        $token = $null
        if ($login.Status -eq 200 -and $login.Content) {
            try { $token = [string](ConvertFrom-Json -InputObject $login.Content).token } catch { $token = $null }
        }
        $login.Content = $null
        if ([string]::IsNullOrWhiteSpace($token)) {
            if ($login.Status -eq 200) { Write-Host "FAIL $($account.Role) respuesta de ingreso sin token"; $script:failures++ }
            continue
        }
        $roleTokens[$account.Role] = $token
        $script:tokens += $token
    }

    foreach ($route in $operationalRoutes) {
        foreach ($role in @('Pasajero', 'Cobrador')) {
            $token = $roleTokens[$role]
            $status = if ($token) { (Invoke-LocalApi 'GET' $route $token $null).Status } else { 0 }
            Assert-Status "$role denegado $route" 403 $status
        }
        $adminToken = $roleTokens['Administrador']
        $adminStatus = if ($adminToken) { (Invoke-LocalApi 'GET' $route $adminToken $null).Status } else { 0 }
        Assert-Status "Administrador permitido $route" 200 $adminStatus
    }

}
catch {
    Write-Host "FAIL configuracion o transporte ($($_.Exception.GetType().Name)); no se muestra detalle potencialmente sensible."
    $script:failures++
}
finally {
    if ($null -ne $script:client) {
        foreach ($token in $script:tokens) {
            $logout = Invoke-LocalApi 'POST' '/api/autenticacion/salida' $token $null
            if ($logout.Status -ne 204) {
                Write-Host "FAIL revocacion de sesion (esperado 204, obtenido $($logout.Status))"
                $script:failures++
            }
        }
        $script:client.Dispose()
    }
    $script:tokens = @()
    $script:password = $null
    if ($null -ne $securePassword) { $securePassword.Dispose() }
    $securePassword = $null
    $token = $null
    $adminToken = $null
    $roleTokens = $null
    $login = $null
}

if ($script:failures -gt 0) { Write-Host "$($script:failures) comprobaciones fallaron."; exit 1 }
Write-Host 'Matriz de autorizacion operativa completa; no prueba persistencia MySQL ni preparacion real de demo.'
exit 0
