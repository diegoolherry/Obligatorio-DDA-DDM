<#
.SYNOPSIS
Publica posiciones ficticias como el cobrador demo, simulando su celular embarcado (MVP-009).
.DESCRIPTION
Pide la contrasena ficticia del cobrador demo de forma segura, ingresa por HTTPS de loopback sin
desactivar la validacion TLS, lista sus servicios asignados y publica -Cantidad posiciones hacia el
destino del servicio elegido, separadas por la periodicidad indicada. Imprime solo codigos de estado
y datos de la publicacion; nunca credenciales ni tokens. Revoca la sesion en finally.
Requiere la base local reiniciada con la asignacion cobrador_servicio sembrada.
.EXAMPLE
.\scripts\publish-demo-position.ps1 -Destino Colonia -Latitud -34.1928 -Longitud -57.8400 -Cantidad 3 -Segundos 30
#>
param(
    [string]$BaseUrl = 'https://localhost:7053',
    [string]$Correo = 'cobrador.demo@example.test',
    [string]$Destino = 'Colonia',
    [double]$Latitud = -34.1928,
    [double]$Longitud = -57.8400,
    [ValidateRange(1, 20)][int]$Cantidad = 1,
    [ValidateRange(1, 600)][int]$Segundos = 30,
    # Avance hacia el sur por publicacion, en grados (ficticio).
    [double]$Paso = 0.01
)
$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1 no carga System.Net.Http por defecto.
Add-Type -AssemblyName System.Net.Http
$client = $null
$token = $null
$inv = [Globalization.CultureInfo]::InvariantCulture

function Send([string]$Method, [string]$Path, [string]$Body) {
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($Method), [Uri]::new($script:baseUri, $Path))
    try {
        if ($script:token) { $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $script:token) }
        if ($Body) { $request.Content = [System.Net.Http.StringContent]::new($Body, [Text.Encoding]::UTF8, 'application/json') }
        $response = $script:client.SendAsync($request).GetAwaiter().GetResult()
        try { return @{ Status = [int]$response.StatusCode; Content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult() } }
        finally { $response.Dispose() }
    }
    finally { $request.Dispose() }
}

try {
    $target = [Uri]$BaseUrl
    if ($target.Scheme -ne 'https' -or $target.Host -notin @('localhost', '127.0.0.1', '::1', '[::1]') -or $target.AbsolutePath -notin @('/', '')) {
        throw 'BaseUrl debe ser HTTPS en localhost y sin ruta.'
    }
    $script:baseUri = [Uri]::new($target.GetLeftPart([UriPartial]::Authority) + '/')

    $secure = Read-Host "Contrasena ficticia de $Correo" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }

    # Validacion TLS predeterminada; sin callback permisivo, redirecciones ni cookies.
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $handler.UseProxy = $false
    $handler.UseCookies = $false
    $script:client = [System.Net.Http.HttpClient]::new($handler)
    $script:client.Timeout = [TimeSpan]::FromSeconds(15)

    $login = Send 'POST' '/api/autenticacion/ingreso' (@{ correo = $Correo; contrasena = $password } | ConvertTo-Json -Compress)
    $password = $null
    if ($login.Status -ne 200) { throw "Ingreso rechazado ($($login.Status))." }
    $script:token = [string](ConvertFrom-Json $login.Content).token
    Write-Host "Ingreso del cobrador: 200"

    $lista = Send 'GET' '/api/ubicacion/cobrador/servicios' $null
    if ($lista.Status -ne 200) { throw "Servicios asignados: $($lista.Status)." }
    # En PowerShell 5.1 ConvertFrom-Json emite el array como un solo objeto: se desenrolla con el pipeline.
    $servicios = @((ConvertFrom-Json $lista.Content) | ForEach-Object { $_ })
    Write-Host "Servicios asignados: $($servicios.Count)"
    $servicio = $servicios | Where-Object { $_.destino -eq $Destino } | Select-Object -First 1
    if (!$servicio) { throw "No hay un servicio asignado con destino '$Destino'." }
    Write-Host "Servicio: $($servicio.origen) -> $($servicio.destino), llegada programada $($servicio.llegada)"

    for ($i = 0; $i -lt $Cantidad; $i++) {
        if ($i -gt 0) { Start-Sleep -Seconds $Segundos }
        $lat = [Math]::Round($Latitud - $Paso * $i, 6)
        $body = '{"latitud":' + $lat.ToString($inv) + ',"longitud":' + ([Math]::Round($Longitud, 6)).ToString($inv) + '}'
        $r = Send 'POST' "/api/ubicacion/servicios/$($servicio.id)/posiciones" $body
        if ($r.Status -eq 201) {
            $p = ConvertFrom-Json $r.Content
            Write-Host "Publicacion $($i + 1): 201 lat=$($p.latitud) lon=$($p.longitud) recibida=$($p.recibidaUtc) intervalo=$($p.intervaloSegundos) cumplePeriodicidad=$($p.cumplePeriodicidad)"
        }
        else { Write-Host "Publicacion $($i + 1): $($r.Status)"; exit 1 }
    }
    exit 0
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)"
    exit 1
}
finally {
    if ($script:token -and $script:client) {
        try { $s = Send 'POST' '/api/autenticacion/salida' $null; Write-Host "Salida del cobrador: $($s.Status)" } catch { Write-Host 'Salida del cobrador: no confirmada' }
    }
    $script:token = $null
    if ($script:client) { $script:client.Dispose() }
}
