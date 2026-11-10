<#
.SYNOPSIS
Verifies the operational demo through the real local HTTPS API without destructive calls.
.DESCRIPTION
Requires an already-running API. Snapshots all four operational resources before any
write, inserts only compatible missing demo data through the idempotent endpoint, then
checks repeated, conflicting, and invalid requests. Never calls DELETE or logout.
The prompted credential is held only in process memory; its token sessions remain active.
#>
param([string]$BaseUrl = 'https://localhost:7053')
$ErrorActionPreference = 'Stop'
$routes = [ordered]@{
    paradas='/api/administracion/paradas'
    tramos='/api/administracion/tramos'
    servicios='/api/administracion/servicios'
    tarifas='/api/administracion/tarifas'
}
$demoRoute='/api/administracion/escenario-demo/preparar-demo'
$secure=$null; $bstr=[IntPtr]::Zero; $password=$null; $client=$null; $tokens=@{}; $failures=0

function Check([string]$name,[bool]$ok) {
    if($ok){Write-Host "PASS $name"} else {Write-Host "FAIL $name"; $script:failures++}
}
function CallApi([string]$method,[string]$path,[string]$token,[string]$json) {
    $request=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($method),[Uri]::new($script:baseUri,$path))
    if($token){$request.Headers.Authorization=[Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$token)}
    if($null -ne $json){$request.Content=[Net.Http.StringContent]::new($json,[Text.Encoding]::UTF8,'application/json')}
    $response=$null
    try {$response=$script:client.SendAsync($request).GetAwaiter().GetResult(); return @{Status=[int]$response.StatusCode;Body=$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}}
    catch {return @{Status=0;Body=$null}}
    finally {if($response){$response.Dispose()};$request.Dispose()}
}
function ReadSnapshot([string]$adminToken) {
    $snapshot=[ordered]@{}
    foreach($key in $script:routes.Keys){
        $r=CallApi GET $script:routes[$key] $adminToken $null
        if($r.Status -ne 200 -or $null -eq $r.Body){throw "Unable to read complete snapshot for $key (HTTP $($r.Status))."}
        $rows=@(ConvertFrom-Json -InputObject $r.Body)
        $normalized=@(foreach($row in $rows){$o=[ordered]@{};foreach($p in ($row.PSObject.Properties|Sort-Object Name)){$o[$p.Name]=$p.Value};[pscustomobject]$o})
        $canonical=@($normalized|Sort-Object { $_.id })
        $snapshot[$key]=[pscustomobject]@{Rows=$rows;Canonical=(ConvertTo-Json -InputObject $canonical -Depth 12 -Compress)}
        $r.Body=$null
    }
    return $snapshot
}
function SameSnapshot($left,$right) {
    foreach($key in $script:routes.Keys){if($left[$key].Canonical -cne $right[$key].Canonical){return $false}}
    return $true
}
function ReadResult($response) {
    if(!$response.Body){return $null}
    try{return ConvertFrom-Json -InputObject $response.Body} catch{return $null}
}
function AssertUnchanged([string]$label,$expected,[string]$adminToken) {
    $actual=ReadSnapshot $adminToken
    Check $label (SameSnapshot $expected $actual)
    return $actual
}
function VerifyDemo($snapshot) {
    $stops=@($snapshot.paradas.Rows)
    $omb=@($stops|Where-Object {$_.nombre -ceq 'Ombúes'})
    $rad=@($stops|Where-Object {$_.nombre -ceq 'Radial de Conchillas'})
    $col=@($stops|Where-Object {$_.nombre -ceq 'Colonia'})
    if($omb.Count -ne 1 -or $rad.Count -ne 1 -or $col.Count -ne 1){return $false}
    $segments=@($snapshot.tramos.Rows)
    $seg1=@($segments|Where-Object {$_.origenId -ceq $omb[0].id -and $_.destinoId -ceq $rad[0].id})
    $seg2=@($segments|Where-Object {$_.origenId -ceq $rad[0].id -and $_.destinoId -ceq $col[0].id})
    if($seg1.Count -ne 1 -or $seg2.Count -ne 1){return $false}
    $services=@($snapshot.servicios.Rows|Where-Object {$_.fecha -like '2026-11-11*'})
    $s1=@($services|Where-Object {$_.tramoId -ceq $seg1[0].id -and $_.horaSalida -like '08:00:00*' -and $_.horaLlegada -like '08:30:00*' -and $_.capacidad -eq 30 -and $_.politicaAsiento -ceq 'sinAsiento'})
    $s2=@($services|Where-Object {$_.tramoId -ceq $seg2[0].id -and $_.horaSalida -like '08:45:00*' -and $_.horaLlegada -like '10:00:00*' -and $_.capacidad -eq 40 -and $_.politicaAsiento -ceq 'asignacionAutomatica'})
    $fa=@($snapshot.tarifas.Rows|Where-Object {$_.tramoId -ceq $seg1[0].id -and [decimal]$_.importe -eq 100 -and $_.vigenteDesde -like '2026-11-01*' -and $_.vigenteHasta -like '2026-11-30*'})
    $fb=@($snapshot.tarifas.Rows|Where-Object {$_.tramoId -ceq $seg2[0].id -and [decimal]$_.importe -eq 250 -and $_.vigenteDesde -like '2026-11-01*' -and $_.vigenteHasta -like '2026-11-30*'})
    return ($s1.Count -eq 1 -and $s2.Count -eq 1 -and $fa.Count -eq 1 -and $fb.Count -eq 1)
}

try {
    $uri=[Uri]$BaseUrl
    if(!$uri.IsAbsoluteUri -or $uri.Scheme -cne 'https' -or $uri.Host -notin @('localhost','127.0.0.1','::1','[::1]') -or $uri.UserInfo -or $uri.Query -or $uri.Fragment -or $uri.AbsolutePath -notin @('/','')){throw 'BaseUrl must be HTTPS loopback without user info, path, query, or fragment.'}
    $script:baseUri=[Uri]::new($uri.GetLeftPart([UriPartial]::Authority)+'/')
    $handler=[Net.Http.HttpClientHandler]::new();$handler.AllowAutoRedirect=$false;$handler.UseProxy=$false;$handler.UseCookies=$false
    $script:client=[Net.Http.HttpClient]::new($handler);$script:client.Timeout=[TimeSpan]::FromSeconds(15)

    foreach($route in $routes.Values){$r=CallApi GET $route $null $null;Check "anonymous GET $route => 401" ($r.Status -eq 401)}
    $anon=CallApi POST $demoRoute $null '{}';Check 'anonymous demo POST => 401' ($anon.Status -eq 401)
    $secure=Read-Host 'Existing fictitious demo-account password (hidden input)' -AsSecureString
    $bstr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    $password=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $accounts=@(@{Role='Pasajero';Email='pasajero.demo@example.test'},@{Role='Cobrador';Email='cobrador.demo@example.test'},@{Role='Administrador';Email='admin.demo@example.test'})
    foreach($a in $accounts){
        $payload=@{correo=$a.Email;contrasena=$password}|ConvertTo-Json -Compress
        $r=CallApi POST '/api/autenticacion/ingreso' $null $payload;$payload=$null
        Check "$($a.Role) login => 200" ($r.Status -eq 200)
        if($r.Status -eq 200){$obj=ReadResult $r;if($obj -and $obj.token){$tokens[$a.Role]=[string]$obj.token}else{Check "$($a.Role) token received in memory" $false}}
        $r.Body=$null
    }
    if($failures -gt 0 -or !$tokens['Administrador'] -or !$tokens['Pasajero'] -or !$tokens['Cobrador']){throw 'Authentication precondition failed; no operational write attempted.'}
    foreach($route in $routes.Values){
        foreach($role in @('Pasajero','Cobrador')){$r=CallApi GET $route $tokens[$role] $null;Check "$role GET $route => 403" ($r.Status -eq 403)}
        $r=CallApi GET $route $tokens['Administrador'] $null;Check "admin GET $route => 200" ($r.Status -eq 200)
    }
    $baseline=ReadSnapshot $tokens['Administrador']
    foreach($role in @('Pasajero','Cobrador')){$r=CallApi POST $demoRoute $tokens[$role] '{}';Check "$role demo POST => 403" ($r.Status -eq 403)}
    $r=CallApi POST $demoRoute $tokens['Administrador'] '{}';Check 'admin empty demo POST => 400' ($r.Status -eq 400)
    $current=AssertUnchanged 'unauthorized and invalid requests preserved all baseline rows' $baseline $tokens['Administrador']
    if($failures -gt 0){throw 'Pre-write checks failed; no scenario write attempted.'}

    $scenario=@{fecha='2026-11-11';horaSalidaOmbuesRadial='08:00:00';horaLlegadaOmbuesRadial='08:30:00';capacidadOmbuesRadial=30;importeOmbuesRadial=100.00;vigenteDesdeOmbuesRadial='2026-11-01';vigenteHastaOmbuesRadial='2026-11-30';horaSalidaRadialColonia='08:45:00';horaLlegadaRadialColonia='10:00:00';capacidadRadialColonia=40;importeRadialColonia=250.00;vigenteDesdeRadialColonia='2026-11-01';vigenteHastaRadialColonia='2026-11-30'}|ConvertTo-Json -Compress
    $created=CallApi POST $demoRoute $tokens['Administrador'] $scenario
    if($created.Status -eq 409){Check 'preexisting incompatible scenario returns 409' $true;$null=AssertUnchanged 'initial conflict preserved every row' $current $tokens['Administrador'];throw 'Scenario conflict; stopped without retry or replacement.'}
    Check 'scenario preparation => 200' ($created.Status -eq 200)
    if($created.Status -ne 200){$null=AssertUnchanged 'unexpected create response did not change existing rows' $current $tokens['Administrador'];throw 'Unexpected scenario response; no retries or compensating mutation attempted.'}
    $createdResult=ReadResult $created
    $afterCreate=ReadSnapshot $tokens['Administrador']
    Check 'scenario has exactly the authorized content' (VerifyDemo $afterCreate)
    # Every prior row must still exist byte-for-value by ID and all fields.
    foreach($key in $routes.Keys){
        $beforeById=@{};foreach($row in $current[$key].Rows){$beforeById[[string]$row.id]=$row}
        $afterById=@{};foreach($row in $afterCreate[$key].Rows){$afterById[[string]$row.id]=$row}
        $preserved=$true
        foreach($id in $beforeById.Keys){if(!$afterById.ContainsKey($id) -or (ConvertTo-Json $beforeById[$id] -Depth 12 -Compress) -cne (ConvertTo-Json $afterById[$id] -Depth 12 -Compress)){$preserved=$false}}
        Check "all prior $key rows preserved by ID and content" $preserved
    }
    if($failures -gt 0){throw 'The scenario response or preservation check failed; stopping without more writes.'}
    $repeat=CallApi POST $demoRoute $tokens['Administrador'] $scenario
    $repeatResult=ReadResult $repeat
    Check 'repeat => 200 with no changes' ($repeat.Status -eq 200 -and $repeatResult -and $repeatResult.sinCambios -eq $true -and $repeatResult.paradasCreadas -eq 0 -and $repeatResult.tramosCreados -eq 0 -and $repeatResult.serviciosCreados -eq 0 -and $repeatResult.tarifasCreadas -eq 0)
    $afterRepeat=ReadSnapshot $tokens['Administrador'];Check 'repeat preserved IDs and all content' (SameSnapshot $afterCreate $afterRepeat)
    $changed=$scenario|ConvertFrom-Json;$changed.capacidadOmbuesRadial=31;$changedJson=ConvertTo-Json -InputObject $changed -Compress
    $conflict=CallApi POST $demoRoute $tokens['Administrador'] $changedJson
    Check 'changed request => 409 conflict' ($conflict.Status -eq 409)
    $afterConflict=AssertUnchanged 'changed request conflict made no changes' $afterRepeat $tokens['Administrador']
    $invalid=$scenario|ConvertFrom-Json;$invalid.capacidadOmbuesRadial=0;$invalidJson=ConvertTo-Json -InputObject $invalid -Compress
    $bad=CallApi POST $demoRoute $tokens['Administrador'] $invalidJson
    Check 'invalid request => 400' ($bad.Status -eq 400)
    $null=AssertUnchanged 'invalid request made no changes' $afterConflict $tokens['Administrador']
    foreach($key in $routes.Keys){Write-Host "READBACK $key rows=$($afterRepeat[$key].Rows.Count)"}
    Write-Host 'NOTE Three new authentication sessions are intentionally left active; no logout was requested or sent.'
} catch {
    Write-Host "BLOCKED $($_.Exception.GetType().Name); details suppressed to avoid exposing sensitive request or connection data."
    if($failures -eq 0){$failures++}
} finally {
    if($client){$client.Dispose()}
    $tokens=$null;$password=$null
    if($secure){$secure.Dispose()}
    if($bstr -ne [IntPtr]::Zero){[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)}
}
if($failures -gt 0){exit 1};exit 0
