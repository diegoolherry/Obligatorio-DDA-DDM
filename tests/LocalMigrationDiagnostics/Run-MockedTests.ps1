$ErrorActionPreference = 'Stop'
function dotnet { throw 'Real dotnet invocation is disabled in this mocked test.' }
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $repoRoot 'scripts/update-local-db.ps1')

$script:checks = 0
$script:failures = 0
function Assert-Check([bool]$Condition, [string]$Name) {
    $script:checks++
    if ($Condition) { Write-Output "PASS $Name" }
    else { Write-Output "FAIL $Name"; $script:failures++ }
}

function New-FakeRun([string]$Failure = '', [int]$FailureCode = 17) {
    $state = [pscustomobject]@{
        Calls = [System.Collections.Generic.List[string]]::new()
        Failure = $Failure
        FailureCode = $FailureCode
        FakePassword = 'unit-only/fake password'
        CapturedConnection = ''
        Prompted = $false
    }
    $state | Add-Member -MemberType ScriptMethod -Name InvokeDotnet -Value {
        param([string[]]$Arguments)
        $call = $Arguments -join ' '
        $this.Calls.Add($call)
        $first = $Arguments[0]
        if ($first -eq 'ef') { $this.CapturedConnection = $env:ConnectionStrings__Berruti }
        if ($this.Failure -eq 'ef' -and $first -eq 'ef') {
            return [pscustomobject]@{
                ExitCode = $this.FailureCode
                Output = @("MySQL access denied; Password=$($this.FakePassword); ConnectionString=$($this.CapturedConnection)")
            }
        }
        if ($this.Failure -eq 'throw-ef' -and $first -eq 'ef') {
            throw "Mock unexpected error token=$([Uri]::EscapeDataString($this.FakePassword))"
        }
        if ($this.Failure -eq 'build' -and $first -eq 'build') {
            return [pscustomobject]@{ ExitCode = $this.FailureCode; Output = @('API build failed in mock') }
        }
        switch ($first) {
            '--version' { return [pscustomobject]@{ ExitCode = 0; Output = @('10.0.400') } }
            'tool' { return [pscustomobject]@{ ExitCode = 0; Output = @('tool restore mock') } }
            'build' { return [pscustomobject]@{ ExitCode = 0; Output = @('API build mock') } }
            'ef' { return [pscustomobject]@{ ExitCode = 0; Output = @('migration list mock') } }
            default { return [pscustomobject]@{ ExitCode = 97; Output = @('unexpected fake command') } }
        }
    }
    $state | Add-Member -MemberType ScriptMethod -Name ReadPassword -Value {
        $this.Prompted = $true
        return ConvertTo-SecureString $this.FakePassword -AsPlainText -Force
    }
    return $state
}

function Invoke-EntrypointScenario([string]$Mode, [switch]$FailEf) {
    $scriptPath = (Resolve-Path (Join-Path $repoRoot 'scripts/update-local-db.ps1')).Path.Replace("'", "''")
    $modeCall = if ($Mode -eq 'Diagnose') { "& '$scriptPath' -Diagnose" } else { "& '$scriptPath'" }
    $failureFlag = if ($FailEf) { '$true' } else { '$false' }
    $child = @"
function global:dotnet {
    if (`$args[0] -eq 'ef') {
        if (`$global:FailEf) { `$global:LASTEXITCODE=17; Write-Output 'mock-ef-failure-marker'; return }
        `$global:LASTEXITCODE=0; Write-Output ('mock-ef-route:' + (`$args -join ' ')); return
    }
    `$global:LASTEXITCODE=0
    if (`$args[0] -eq 'build') { Write-Output 'mock-build-marker' }
    elseif (`$args[0] -eq 'tool') { Write-Output 'mock-tool-marker' }
    else { Write-Output '10.0.400' }
}
function global:Read-Host { ConvertTo-SecureString 'unit-only-scriptfile-password' -AsPlainText -Force }
`$global:FailEf = $failureFlag
`$env:ConnectionStrings__Berruti = 'script-prior-connection'
`$env:ASPNETCORE_ENVIRONMENT = 'script-prior-environment'
$modeCall
"@
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($child))
    $output = @(& powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand $encoded 2>&1 | ForEach-Object { $_.ToString() })
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}

$defaultMode = Resolve-LocalDatabaseMode -Diagnose:$false -Apply:$false
Assert-Check ($defaultMode -eq 'Diagnose') 'no arguments resolve to read-only Diagnose'
Assert-Check ((Resolve-LocalDatabaseMode -Diagnose:$true -Apply:$false) -eq 'Diagnose') 'explicit Diagnose mode accepted'
Assert-Check ((Resolve-LocalDatabaseMode -Diagnose:$false -Apply:$true) -eq 'Apply') 'Apply requires its explicit switch'
$bothRejected = $false
try { Resolve-LocalDatabaseMode -Diagnose:$true -Apply:$true | Out-Null } catch { $bothRejected = $true }
Assert-Check $bothRejected 'mutually exclusive mode switches rejected'

$entryDefault = Invoke-EntrypointScenario -Mode Default
$entryDiagnose = Invoke-EntrypointScenario -Mode Diagnose
$entryFailure = Invoke-EntrypointScenario -Mode Diagnose -FailEf
foreach ($scenario in @($entryDefault, $entryDiagnose)) {
    $scenarioText = $scenario.Output -join "`n"
    Assert-Check ($scenario.ExitCode -eq 0 -and $scenarioText -match 'mock-ef-route:ef migrations list' -and $scenarioText -notmatch 'database update|Invoke-LocalNativeDotnet.*not recognized') 'script-file entrypoint lists migrations and exits successfully'
}
$failureText = $entryFailure.Output -join "`n"
Assert-Check ($entryFailure.ExitCode -eq 1 -and $failureText -match 'codigo 17' -and $failureText -match 'mock-ef-failure-marker' -and $failureText -notmatch 'unit-only-scriptfile-password|database update|Invoke-LocalNativeDotnet.*not recognized') 'script-file entrypoint reports sanitized mocked EF failure with exit 1'

$dotnetGuard = (Get-Item Function:\dotnet).ScriptBlock
function dotnet { & $env:ComSpec /d /c 'echo harmless-native-stderr 1>&2 & exit /b 17' }
$priorErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference = 'Stop'
$nativeResult = Invoke-LocalNativeDotnet -Arguments @('not-a-real-dotnet-call')
Assert-Check ($nativeResult.ExitCode -eq 17 -and ($nativeResult.Output -join "`n") -match 'harmless-native-stderr') 'native stderr helper preserves output and exit 17 under Stop'
Assert-Check ($ErrorActionPreference -eq 'Stop') 'native wrapper restores caller error preference'
$ErrorActionPreference = $priorErrorActionPreference
Set-Item Function:\dotnet $dotnetGuard

$originalLocation = Get-Location
$originalConnectionPresent = Test-Path Env:ConnectionStrings__Berruti
$originalConnection = $env:ConnectionStrings__Berruti
$originalEnvironmentPresent = Test-Path Env:ASPNETCORE_ENVIRONMENT
$originalEnvironment = $env:ASPNETCORE_ENVIRONMENT
Set-Location -LiteralPath $PSScriptRoot
$env:ConnectionStrings__Berruti = 'mock-prior-connection-marker'
$env:ASPNETCORE_ENVIRONMENT = 'mock-prior-environment'

$global:DefaultInvokerCalls = [System.Collections.Generic.List[string]]::new()
function dotnet {
    $call = $args -join ' '
    $global:DefaultInvokerCalls.Add($call)
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'ef') { Write-Output 'default-invoker-list-mock' }
    elseif ($args[0] -eq 'build') { Write-Output 'default-invoker-build-mock' }
    elseif ($args[0] -eq 'tool') { Write-Output 'default-invoker-tool-mock' }
    else { Write-Output '10.0.400' }
}
$defaultInvokerPassword = ConvertTo-SecureString 'unit-only-default-password' -AsPlainText -Force
$defaultInvokerResult = Invoke-LocalDatabaseWorkflow -Mode Diagnose -PasswordReader { $defaultInvokerPassword }
Assert-Check ($defaultInvokerResult.ExitCode -eq 0 -and @($global:DefaultInvokerCalls | Where-Object { $_ -match 'ef migrations list --no-build' }).Count -eq 1 -and @($global:DefaultInvokerCalls | Where-Object { $_ -match 'database update' }).Count -eq 0 -and ($defaultInvokerResult.Messages -join "`n") -notmatch 'not recognized|not found') 'default invoker resolves helper and lists migrations without update'
Assert-Check ($env:ConnectionStrings__Berruti -eq 'mock-prior-connection-marker' -and $env:ASPNETCORE_ENVIRONMENT -eq 'mock-prior-environment') 'default invoker restores environment after helper execution'
Remove-Item Function:\dotnet -ErrorAction SilentlyContinue
Set-Item Function:\dotnet $dotnetGuard
Remove-Variable DefaultInvokerCalls -Scope Global -ErrorAction SilentlyContinue

$diagnoseRun = New-FakeRun
$reader = { $diagnoseRun.ReadPassword() }.GetNewClosure()
$runner = { param([string[]]$Arguments) $diagnoseRun.InvokeDotnet($Arguments) }.GetNewClosure()
$diagnoseResult = Invoke-LocalDatabaseWorkflow -Mode Diagnose -DotnetInvoker $runner -PasswordReader $reader
$diagnoseCommands = @($diagnoseRun.Calls)
Assert-Check ($diagnoseResult.ExitCode -eq 0 -and ($diagnoseCommands -join '|') -match 'ef migrations list --no-build') 'diagnostic mode reaches mocked migrations list'
Assert-Check (@($diagnoseCommands | Where-Object { $_ -match 'database update' }).Count -eq 0) 'diagnostic mode never dispatches update'
Assert-Check ($diagnoseRun.Prompted -and $diagnoseRun.CapturedConnection -match 'Server=localhost' -and $diagnoseRun.CapturedConnection -match 'Database=berrutilocal' -and $diagnoseRun.CapturedConnection -match 'User ID=berruti_app') 'mock observes fixed local connection only during EF call'
Assert-Check ($env:ConnectionStrings__Berruti -eq 'mock-prior-connection-marker' -and $env:ASPNETCORE_ENVIRONMENT -eq 'mock-prior-environment' -and (Get-Location).Path -eq $PSScriptRoot) 'diagnose restores process environment and working directory'
Assert-Check (($diagnoseResult.Messages -join "`n") -notmatch [regex]::Escape($diagnoseRun.FakePassword)) 'successful mock output excludes fake password'

$applyRun = New-FakeRun
$applyReader = { $applyRun.ReadPassword() }.GetNewClosure()
$applyRunner = { param([string[]]$Arguments) $applyRun.InvokeDotnet($Arguments) }.GetNewClosure()
$applyResult = Invoke-LocalDatabaseWorkflow -Mode Apply -DotnetInvoker $applyRunner -PasswordReader $applyReader
Assert-Check ($applyResult.ExitCode -eq 0 -and @($applyRun.Calls | Where-Object { $_ -match 'ef database update' }).Count -eq 1 -and @($applyRun.Calls | Where-Object { $_ -match 'ef migrations list' }).Count -eq 0) 'explicit Apply dispatches only mocked database update'
Assert-Check (($applyResult.Messages -join "`n") -notmatch [regex]::Escape($applyRun.FakePassword)) 'mock Apply output excludes fake password'

$failed = New-FakeRun -Failure 'ef' -FailureCode 17
$failedReader = { $failed.ReadPassword() }.GetNewClosure()
$failedRunner = { param([string[]]$Arguments) $failed.InvokeDotnet($Arguments) }.GetNewClosure()
$failedResult = Invoke-LocalDatabaseWorkflow -Mode Diagnose -DotnetInvoker $failedRunner -PasswordReader $failedReader
$failedText = $failedResult.Messages -join "`n"
Assert-Check ($failedResult.ExitCode -eq 1 -and $failedText.Contains('codigo 17') -and $failedText.Contains('access denied')) 'mock native failure reports stage and exit code'
Assert-Check ($failedText -notmatch [regex]::Escape($failed.FakePassword) -and $failedText -notmatch [regex]::Escape($failed.CapturedConnection) -and $failedText -match '\[REDACTED\]') 'mock EF diagnostics redact password and full connection string'
Assert-Check (@($failed.Calls | Where-Object { $_ -match 'database update' }).Count -eq 0 -and $env:ConnectionStrings__Berruti -eq 'mock-prior-connection-marker' -and (Get-Location).Path -eq $PSScriptRoot) 'failed Diagnose never updates and still restores environment'

$unexpected = New-FakeRun -Failure 'throw-ef'
$unexpectedReader = { $unexpected.ReadPassword() }.GetNewClosure()
$unexpectedRunner = { param([string[]]$Arguments) $unexpected.InvokeDotnet($Arguments) }.GetNewClosure()
$unexpectedResult = Invoke-LocalDatabaseWorkflow -Mode Diagnose -DotnetInvoker $unexpectedRunner -PasswordReader $unexpectedReader
$unexpectedText = $unexpectedResult.Messages -join "`n"
$encodedUnexpectedPassword = [Uri]::EscapeDataString($unexpected.FakePassword)
Assert-Check ($unexpectedText -notmatch [regex]::Escape($unexpected.FakePassword) -and $unexpectedText -notmatch [regex]::Escape($encodedUnexpectedPassword) -and $unexpectedText -match '\[REDACTED\]') 'unexpected exception redacts clear and URI-encoded fake password'

$buildFailure = New-FakeRun -Failure 'build' -FailureCode 9
$buildReader = { $buildFailure.ReadPassword() }.GetNewClosure()
$buildRunner = { param([string[]]$Arguments) $buildFailure.InvokeDotnet($Arguments) }.GetNewClosure()
$buildResult = Invoke-LocalDatabaseWorkflow -Mode Diagnose -DotnetInvoker $buildRunner -PasswordReader $buildReader
Assert-Check ($buildResult.ExitCode -eq 1 -and !$buildFailure.Prompted -and @($buildFailure.Calls | Where-Object { $_ -match '^ef ' }).Count -eq 0) 'preflight build failure stops before password and EF calls'

Set-Location -LiteralPath $originalLocation
if ($originalConnectionPresent) { $env:ConnectionStrings__Berruti = $originalConnection }
else { Remove-Item Env:ConnectionStrings__Berruti -ErrorAction SilentlyContinue }
if ($originalEnvironmentPresent) { $env:ASPNETCORE_ENVIRONMENT = $originalEnvironment }
else { Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue }

Write-Output "$($script:checks - $script:failures)/$($script:checks) PASS"
if ($script:failures -gt 0) { exit 1 }
exit 0
