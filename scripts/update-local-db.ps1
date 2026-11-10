<#
.SYNOPSIS
Diagnostica migraciones locales; solo -Apply solicita aplicar cambios.
.DESCRIPTION
Sin argumentos equivale a -Diagnose. Diagnose consulta la lista de migraciones
con la identidad local y no actualiza el esquema. Solo -Apply ejecuta database update.
Usa localhost:3306/berrutilocal/berruti_app y una clave SecureString. La salida
de procesos se sanitiza antes de mostrarse y nunca se guarda en archivos.
#>
[CmdletBinding()]
param([switch]$Diagnose, [switch]$Apply)

$script:LocalMigrationRepoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$script:LocalMigrationProject = 'src/Api/API-Berruti/API-Berruti/API-Berruti.csproj'

function Resolve-LocalDatabaseMode {
    param([bool]$Diagnose, [bool]$Apply)
    if ($Diagnose -and $Apply) { throw 'Usa -Diagnose o -Apply, no ambos.' }
    if ($Apply) { return 'Apply' }
    return 'Diagnose'
}

function Protect-LocalMigrationOutput {
    param([AllowNull()][object[]]$Lines, [AllowNull()][string[]]$Secrets)
    $text = (@($Lines | ForEach-Object { [string]$_ }) -join [Environment]::NewLine)
    foreach ($secret in @($Secrets | Where-Object { -not [string]::IsNullOrEmpty($_) } | Sort-Object Length -Descending)) {
        $text = [regex]::Replace($text, [regex]::Escape($secret), '[REDACTED]', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    }
    $assignment = '(?i)(password|pwd)\s*=\s*(?:"[^"]*"|''[^'']*''|[^;,\s]+)'
    $text = [regex]::Replace($text, $assignment, '$1=[REDACTED]')
    return @($text -split "`r?`n")
}

function Invoke-LocalNativeDotnet {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        if ($null -eq (Get-Command dotnet -ErrorAction SilentlyContinue)) {
            return [pscustomobject]@{ ExitCode = 127; Output = @('No se encontro el ejecutable dotnet.') }
        }
        # Windows PowerShell can promote native stderr records to terminating errors
        # under Stop; capture them without losing the process exit code.
        $ErrorActionPreference = 'Continue'
        $output = @(& dotnet @Arguments 2>&1 | ForEach-Object { $_.ToString() })
        $nativeCode = $LASTEXITCODE
        return [pscustomobject]@{ ExitCode = [int]$nativeCode; Output = $output }
    }
    catch {
        return [pscustomobject]@{ ExitCode = 127; Output = @($_.Exception.Message) }
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Invoke-LocalDatabaseWorkflow {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidateSet('Diagnose', 'Apply')][string]$Mode,
        [scriptblock]$DotnetInvoker,
        [scriptblock]$PasswordReader
    )

    if ($null -eq $DotnetInvoker) {
        $DotnetInvoker = ${function:Invoke-LocalNativeDotnet}
    }
    if ($null -eq $PasswordReader) {
        $PasswordReader = { Read-Host 'Contrasena MySQL local de berruti_app' -AsSecureString }.GetNewClosure()
    }

    $messages = [System.Collections.Generic.List[string]]::new()
    $previousLocation = Get-Location
    $hadConnection = Test-Path Env:ConnectionStrings__Berruti
    $previousConnection = $env:ConnectionStrings__Berruti
    $hadEnvironment = Test-Path Env:ASPNETCORE_ENVIRONMENT
    $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $builder = $null
    $securePassword = $null
    $passwordBstr = [IntPtr]::Zero
    $password = $null
    $connectionString = $null
    $secrets = [System.Collections.Generic.List[string]]::new()
    $resultCode = 1
    $stage = 'Inicializacion'

    try {
        Set-Location -LiteralPath $script:LocalMigrationRepoRoot
        Remove-Item Env:ConnectionStrings__Berruti -ErrorAction SilentlyContinue

        $stage = 'SDK .NET'
        $versionResult = & $DotnetInvoker -Arguments @('--version')
        $versionLines = Protect-LocalMigrationOutput $versionResult.Output @()
        $versionText = $versionLines -join [Environment]::NewLine
        if ($versionResult.ExitCode -ne 0 -or $versionText -notmatch '(?m)^\s*10\.') {
            $messages.Add("Fallo en $stage (codigo $($versionResult.ExitCode)); se requiere SDK .NET 10.")
            foreach ($line in $versionLines) { if ($line) { $messages.Add($line) } }
            return [pscustomobject]@{ ExitCode = 1; Messages = @($messages) }
        }
        $messages.Add("${stage}: $($versionText.Trim())")

        $manifest = Join-Path $script:LocalMigrationRepoRoot 'dotnet-tools.json'
        if (!(Test-Path -LiteralPath $manifest)) {
            $messages.Add('Fallo en restauracion de herramientas: falta dotnet-tools.json.')
            return [pscustomobject]@{ ExitCode = 1; Messages = @($messages) }
        }
        $project = $script:LocalMigrationProject

        $stage = 'Restauracion de herramientas EF'
        $toolResult = & $DotnetInvoker -Arguments @('tool', 'restore', '--tool-manifest', $manifest)
        $toolLines = Protect-LocalMigrationOutput $toolResult.Output @()
        if ($toolResult.ExitCode -ne 0) {
            $messages.Add("Fallo en $stage (codigo $($toolResult.ExitCode)); revisa el manifiesto y los feeds disponibles.")
            foreach ($line in $toolLines) { if ($line) { $messages.Add($line) } }
            return [pscustomobject]@{ ExitCode = 1; Messages = @($messages) }
        }
        $messages.Add("${stage}: correcto.")
        foreach ($line in $toolLines) { if ($line) { $messages.Add($line) } }

        $stage = 'Compilacion del proyecto API'
        $buildResult = & $DotnetInvoker -Arguments @('build', $project, '--no-restore', '--nologo')
        $buildLines = Protect-LocalMigrationOutput $buildResult.Output @()
        if ($buildResult.ExitCode -ne 0) {
            $messages.Add("Fallo en $stage (codigo $($buildResult.ExitCode)); revisa los errores de compilacion mostrados.")
            foreach ($line in $buildLines) { if ($line) { $messages.Add($line) } }
            return [pscustomobject]@{ ExitCode = 1; Messages = @($messages) }
        }
        $messages.Add("${stage}: correcto.")
        foreach ($line in $buildLines) { if ($line) { $messages.Add($line) } }

        $securePassword = & $PasswordReader
        if ($null -eq $securePassword -or $securePassword.Length -eq 0) {
            $messages.Add('Operacion cancelada: la contrasena local esta vacia.')
            return [pscustomobject]@{ ExitCode = 1; Messages = @($messages) }
        }
        $passwordBstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
        $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordBstr)
        $secrets.Add($password)
        $secrets.Add([Uri]::EscapeDataString($password))

        $builder = [System.Data.Common.DbConnectionStringBuilder]::new()
        $builder['Server'] = 'localhost'
        $builder['Port'] = 3306
        $builder['Database'] = 'berrutilocal'
        $builder['User ID'] = 'berruti_app'
        $builder['Password'] = $password
        $connectionString = $builder.ConnectionString
        $secrets.Add($connectionString)

        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:ConnectionStrings__Berruti = $connectionString

        $commonArgs = @('--no-build', '--project', $project, '--startup-project', $project, '--context', 'API_Berruti.Data.BerrutiDbContext')
        if ($Mode -eq 'Diagnose') {
            $stage = 'Lectura de historial de migraciones (solo diagnostico)'
            $efArgs = @('ef', 'migrations', 'list') + $commonArgs
        }
        else {
            $stage = 'Aplicacion de migraciones EF (-Apply explicito)'
            $efArgs = @('ef', 'database', 'update') + $commonArgs
        }

        $efResult = & $DotnetInvoker -Arguments $efArgs
        $efLines = Protect-LocalMigrationOutput $efResult.Output $secrets
        if ($efResult.ExitCode -ne 0) {
            $messages.Add("Fallo en $stage (codigo $($efResult.ExitCode)); causa reportada por EF/MySQL, ya sanitizada:")
            foreach ($line in $efLines) { if ($line) { $messages.Add($line) } }
            return [pscustomobject]@{ ExitCode = 1; Messages = @($messages) }
        }

        if ($Mode -eq 'Diagnose') {
            $messages.Add("EF devolvio codigo 0 en $stage. La lista puede mostrarse aun si falla la lectura de historial; esto no prueba permisos DDL ni compatibilidad del esquema.")
        }
        else {
            $messages.Add('EF devolvio codigo 0; se aplicaron las migraciones pendientes. No se crearon cuentas demo ni datos operativos.')
        }
        foreach ($line in $efLines) { if ($line) { $messages.Add($line) } }
        $resultCode = 0
    }
    catch {
        $exceptionLines = Protect-LocalMigrationOutput @($_.Exception.Message) @($secrets.ToArray())
        $messages.Add("Fallo inesperado en $stage; el mensaje fue sanitizado y no se muestran secretos.")
        foreach ($line in $exceptionLines) { if ($line) { $messages.Add($line) } }
        $resultCode = 1
    }
    finally {
        if ($passwordBstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordBstr) }
        if ($null -ne $securePassword) { $securePassword.Dispose() }
        if ($null -ne $builder) { $builder.Clear() }
        $password = $null
        $connectionString = $null
        if ($hadConnection) { $env:ConnectionStrings__Berruti = $previousConnection }
        else { Remove-Item Env:ConnectionStrings__Berruti -ErrorAction SilentlyContinue }
        if ($hadEnvironment) { $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment }
        else { Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue }
        Set-Location -LiteralPath $previousLocation
    }

    return [pscustomobject]@{ ExitCode = $resultCode; Messages = @($messages) }
}

if ($MyInvocation.InvocationName -ne '.') {
    try { $mode = Resolve-LocalDatabaseMode -Diagnose:$Diagnose -Apply:$Apply }
    catch { Write-Host 'Uso invalido: ejecuta sin argumentos o con -Diagnose para diagnosticar; -Apply requiere autorizacion explicita.'; exit 2 }
    $workflow = Invoke-LocalDatabaseWorkflow -Mode $mode
    foreach ($message in $workflow.Messages) { Write-Host $message }
    exit $workflow.ExitCode
}
