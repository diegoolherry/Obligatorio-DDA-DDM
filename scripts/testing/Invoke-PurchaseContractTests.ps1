[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('WebPurchase', 'PaymentContracts')]
    [string]$Suite,
    [switch]$VerifyFailureExit,
    [ValidateRange(1, 300)]
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$executable = Join-Path $repositoryRoot "tests\$Suite\bin\Debug\net10.0\$Suite.exe"
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    Write-Error "Expected apphost executable was not built: $executable"
    exit 2
}

$mode = if ($VerifyFailureExit) { 'controlled-failure' } else { 'normal' }
$logDirectory = Join-Path ([IO.Path]::GetTempPath()) ("purchase-contract-tests-{0}-{1}-{2}" -f $Suite, $mode, [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $logDirectory | Out-Null
$stdoutPath = Join-Path $logDirectory 'stdout.log'
$stderrPath = Join-Path $logDirectory 'stderr.log'

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $executable
$startInfo.WorkingDirectory = $repositoryRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
if ($VerifyFailureExit) {
    $startInfo.Arguments = '--verify-failure-exit'
}

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $startInfo
$started = $false
$timedOut = $false
$stdout = ''
$stderr = ''
$actualExitCode = $null
$hasExited = $false
$stdoutTask = $null
$stderrTask = $null
try {
    if (-not $process.Start()) {
        Write-Error 'The selected apphost process did not start.'
        exit 2
    }
    $started = $true
    $processId = $process.Id

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $timedOut = $true
        $process.Kill()
        $hasExited = $process.WaitForExit(5000)
    }
    else {
        $hasExited = $true
    }

    if ($hasExited) {
        $process.WaitForExit()
        $stdout = $stdoutTask.GetAwaiter().GetResult()
        $stderr = $stderrTask.GetAwaiter().GetResult()
        $actualExitCode = $process.ExitCode
    }
    else {
        $stdout = "The launched process PID $processId did not exit after its bounded wait and own-PID termination request."
        $stderr = $stdout
    }

    [IO.File]::WriteAllText($stdoutPath, $stdout, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($stderrPath, $stderr, [Text.UTF8Encoding]::new($false))

    Write-Output "Suite=$Suite Mode=$mode PID=$processId ExitCode=$actualExitCode HasExited=$hasExited TimedOut=$timedOut"
    Write-Output "StdoutLog=$stdoutPath"
    Write-Output "StderrLog=$stderrPath"
    if ($stdout.Length -gt 0) {
        Write-Output '--- captured stdout ---'
        Write-Output $stdout
    }
    if ($stderr.Length -gt 0) {
        Write-Output '--- captured stderr ---'
        Write-Output $stderr
    }

    if ($timedOut -or -not $hasExited) {
        exit 124
    }

    if ($VerifyFailureExit) {
        $expectedFailure = $actualExitCode -eq 1 -and $stderr.Contains('CONTROLLED-TEST-HARNESS-FAILURE')
        if (-not $expectedFailure) {
            Write-Error 'Controlled failure probe did not return exit 1 with the expected exception marker.'
            if ($actualExitCode -eq 0) { exit 1 }
            exit $actualExitCode
        }
        exit $actualExitCode
    }

    $summaryValid = if ($Suite -eq 'WebPurchase') {
        $summary = [regex]::Match($stdout, '(?m)^(?<passed>\d+)/(?<total>\d+) Web purchase checks passed\.\r?$')
        $summary.Success -and [int]$summary.Groups['total'].Value -gt 0 -and
            [int]$summary.Groups['passed'].Value -eq [int]$summary.Groups['total'].Value
    }
    else {
        $summary = [regex]::Match($stdout, '(?m)^Checks: (?<total>\d+); failures: (?<failures>\d+)\r?$')
        $summary.Success -and [int]$summary.Groups['total'].Value -gt 0 -and
            [int]$summary.Groups['failures'].Value -eq 0
    }
    if ($actualExitCode -eq 0 -and $stderr.Length -eq 0 -and $summaryValid) {
        exit 0
    }
    if ($actualExitCode -ne 0) {
        exit $actualExitCode
    }

    Write-Error 'Normal suite did not report a positive, fully passed summary with empty stderr.'
    exit 1
}
finally {
    if ($started -and -not $process.HasExited) {
        try {
            $process.Kill()
            [void]$process.WaitForExit(5000)
        }
        catch {
            Write-Output "Own-process cleanup failed for PID ${processId}: $($_.Exception.ToString())"
        }
    }
    $process.Dispose()
}
