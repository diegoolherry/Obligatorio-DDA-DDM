# Public launcher probes only; no database, server, real API .env or dependencies.
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source = Join-Path $root 'scripts/start-api-local.ps1'
$scratch = Join-Path $PSScriptRoot ('fixtures/template-' + [Guid]::NewGuid().ToString('N'))
$run = Join-Path $scratch 'probes'
$configured = Join-Path $run 'configured'
$missing = Join-Path $run 'missing'
$apiRelative = 'src/Api/API-Berruti/API-Berruti'
foreach ($tree in @($configured, $missing)) {
    New-Item -ItemType Directory -Path (Join-Path $tree 'scripts') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $tree $apiRelative) -Force | Out-Null
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $tree 'scripts/start-api-local.ps1')
    }
}
$script:failures = 0
$script:count = 0
function Check([string]$Name, [bool]$Condition) {
    $script:count++
    if ($Condition) { Write-Output "PASS $Name" }
    else { Write-Output "FAIL $Name"; $script:failures++ }
}
function Invoke-Probe([string]$Name, [string]$Text, [bool]$Valid = $false,
    [string]$Profile = 'default', [int]$ChildExit = 0, [switch]$MissingFile,
    [switch]$AbsentPrevious, [switch]$ThrowChild) {
    $tree = $configured
    if ($MissingFile) { $tree = $missing }
    else {
        [IO.File]::WriteAllText((Join-Path $tree "$apiRelative/.env"), $Text)
    }
    $capture = Join-Path $run "$Name.json"
    $driver = Join-Path $PSScriptRoot 'fixtures/invoke.ps1'
    $launcher = Join-Path $tree 'scripts/start-api-local.ps1'
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = (Get-Process -Id $PID).Path
    $info.Arguments = '-NoProfile -File "{0}" -Launcher "{1}" -Capture "{2}" -Profile "{3}" -ChildExit {4}' -f `
        $driver, $launcher, $capture, $Profile, $ChildExit
    if ($AbsentPrevious) { $info.Arguments += ' -AbsentPrevious' }
    if ($ThrowChild) { $info.Arguments += ' -ThrowChild' }
    $info.WorkingDirectory = $run # Deliberately not the fake repository root.
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($info)
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    @{ Exit = $process.ExitCode; Stdout = $stdout; Stderr = $stderr } |
        ConvertTo-Json -Compress | Set-Content -LiteralPath (Join-Path $run "$Name.transport.json") -Encoding UTF8
    $result = Get-Content -LiteralPath $capture -Raw | ConvertFrom-Json
    $expectedPrevious = 'PREVIOUS_SYNTHETIC'
    if ($AbsentPrevious) { $expectedPrevious = $null }
    $safe = $result.Restored -ceq $expectedPrevious -and $result.Other -ceq 'UNCHANGED'
    if ($Valid) {
        $expectedValue = $Text.Split("`n") | Where-Object { $_.StartsWith('ConnectionStrings__Berruti=') }
        $expectedValue = $expectedValue.Substring('ConnectionStrings__Berruti='.Length)
        $expectedProfile = $Profile
        if ($Profile -eq 'default') { $expectedProfile = 'https' }
        $expectedArgs = @('run', '--project', (Join-Path $tree "$apiRelative/API-Berruti.csproj"), '--launch-profile', $expectedProfile)
        $safe = $safe -and $result.Called -and $result.Value -ceq $expectedValue -and `
            (($result.Arguments -join '|') -ceq ($expectedArgs -join '|'))
        if ($ThrowChild) {
            $safe = $safe -and $process.ExitCode -eq 2 -and $stdout -ceq '' -and `
                $stderr -ceq "API local: no se pudo iniciar dotnet.`r`n"
        } else {
            $safe = $safe -and $process.ExitCode -eq $ChildExit -and `
                $stdout -ceq "FAKE_DOTNET`r`n" -and $stderr -ceq ''
        }
    } else {
        $safe = $safe -and !$result.Called -and $process.ExitCode -eq 2 -and `
            $stdout -ceq '' -and $stderr -ceq "API local: configuracion .env invalida o no disponible.`r`n"
    }
    # Failures show only rule names, never file content or captured values.
    Check $Name $safe
}
$value = 'Server=synthetic;Database=fixture;Password=marker==;Literal=$(not-executed);Other=#data'
$entry = 'ConnectionStrings__Berruti=' + $value
Invoke-Probe 'missing-file' '' -MissingFile
Invoke-Probe 'missing-key' "# comment`n`n"
Invoke-Probe 'empty-value' 'ConnectionStrings__Berruti='
Invoke-Probe 'whitespace-value' 'ConnectionStrings__Berruti=   '
Invoke-Probe 'malformed-line' 'SYNTHETIC_MALFORMED'
Invoke-Probe 'unknown-key' ($entry + "`nOTHER=synthetic")
Invoke-Probe 'duplicate-key' ($entry + "`n" + $entry)
Invoke-Probe 'wrong-case' 'connectionstrings__berruti=synthetic'
Invoke-Probe 'export-not-supported' ('export ' + $entry)
Invoke-Probe 'outer-quotes-not-supported' 'ConnectionStrings__Berruti="synthetic"'
Invoke-Probe 'unbalanced-quote' 'ConnectionStrings__Berruti="synthetic'
Invoke-Probe 'invalid-profile' $entry -Profile 'invalid'
Invoke-Probe 'https-forward-literal' ("# comment`n`n" + $entry) -Valid $true
Invoke-Probe 'http-profile' $entry -Valid $true -Profile 'http'
Invoke-Probe 'restore-child-nonzero' $entry -Valid $true -ChildExit 17
Invoke-Probe 'restore-absent-env' $entry -Valid $true -AbsentPrevious
Invoke-Probe 'restore-child-exception' $entry -Valid $true -ThrowChild
$privatePaths = @('src/Api/API-Berruti/API-Berruti/.env',
    'src/Api/API-Berruti/API-Berruti/.env.local', 'src/Api/API-Berruti/API-Berruti/.env.production',
    '.local/mysql-tests.json', 'src/Api/API-Berruti/API-Berruti/bin/probe.dll')
$ignored = @(& git -C $root check-ignore --no-index -- $privatePaths 2>$null)
Check 'ignore-private-and-existing' (@($privatePaths | Where-Object { $_ -notin $ignored }).Count -eq 0)
$example = 'src/Api/API-Berruti/API-Berruti/api-local-config.example.txt'
& git -C $root check-ignore --no-index --quiet -- $example
Check 'example-trackable' ($LASTEXITCODE -eq 1)
$examplePath = Join-Path $root $example
$blankExample = (Test-Path -LiteralPath $examplePath)
if ($blankExample) {
    $lines = @(Get-Content -LiteralPath $examplePath | Where-Object { $_.Trim() -ne '' -and !$_.Trim().StartsWith('#') })
    $blankExample = $lines.Count -eq 1 -and $lines[0] -ceq 'ConnectionStrings__Berruti='
}
Check 'example-only-empty-key' $blankExample
Write-Output ("RESULT {0}/{1} PASS" -f ($script:count - $script:failures), $script:count)
if ($script:failures -gt 0) { exit 1 }
exit 0
