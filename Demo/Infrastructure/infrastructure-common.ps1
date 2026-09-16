<#
.SYNOPSIS
    Shared helpers for start-infrastructure.ps1 and remove-infrastructure.ps1, dot-source this file.
#>

$composeFile = Join-Path $PSScriptRoot 'docker-compose.yml'
$composeProject = 'zerra-demo'

$dockerDesktopExe = Join-Path $env:ProgramFiles 'Docker\Docker\Docker Desktop.exe'
$rancherDesktopExe = @(
    (Join-Path $env:ProgramFiles 'Rancher Desktop\Rancher Desktop.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Rancher Desktop\Rancher Desktop.exe')
) | Where-Object { Test-Path $_ } | Select-Object -First 1

#Windows PowerShell turns a native command's stderr into an error record, which Stop makes terminating, so docker calls run with Continue and check the exit code
function Test-DockerContext([string]$context) {
    #a half started engine can accept the connection and never answer, so don't wait on it forever
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = (Get-Command docker).Source
    $startInfo.Arguments = "--context `"$context`" info --format {{.ServerVersion}}"
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        #the output is a single line, so it can't fill the pipe buffers while waiting
        if (-not $process.WaitForExit(15000)) {
            try { $process.Kill() } catch { }
            return $false
        }
        return $process.ExitCode -eq 0
    }
    finally {
        $process.Dispose()
    }
}

#Docker Desktop and Rancher Desktop (moby) expose different pipes, so find the context whose engine is answering
function Find-DockerContext {
    $ErrorActionPreference = 'Continue'
    $existing = @(& docker context ls --format '{{.Name}}' 2>$null)
    $current = (& docker context show 2>$null)
    $candidates = @($current, 'desktop-linux', 'default', 'rancher-desktop') | Where-Object { $_ -and $existing -contains $_ } | Select-Object -Unique
    foreach ($context in $candidates) {
        if (Test-DockerContext $context) { return $context }
    }
    return $null
}

#Detected by the app's process, not the WSL distros: Docker Desktop's WSL integration boots other distros, including rancher-desktop,
#and terminating a distro Docker Desktop has integrated breaks its engine
function Test-RancherRunning {
    return [bool](Get-Process -Name 'Rancher Desktop' -ErrorAction SilentlyContinue)
}

function Test-DockerDesktopRunning {
    return [bool](Get-Process -Name 'Docker Desktop' -ErrorAction SilentlyContinue)
}

#Rancher Desktop and Docker Desktop can't run at the same time, so the other one is stopped before one is started
function Stop-RancherDesktop {
    $ErrorActionPreference = 'Continue'
    Write-Host 'Stopping Rancher Desktop, it cannot run alongside Docker Desktop' -ForegroundColor Cyan
    $rdctl = Get-Command rdctl -ErrorAction SilentlyContinue
    if ($rdctl) { & $rdctl.Source shutdown 2>&1 | Out-Null }
    Get-Process -Name 'Rancher Desktop' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

function Stop-DockerDesktop {
    $ErrorActionPreference = 'Continue'
    Write-Host 'Stopping Docker Desktop, it cannot run alongside Rancher Desktop' -ForegroundColor Cyan
    $dockerDesktopCli = Join-Path $env:ProgramFiles 'Docker\Docker\resources\bin\docker.exe'
    if (Test-Path $dockerDesktopCli) { & $dockerDesktopCli desktop stop 2>&1 | Out-Null }
    Get-Process -Name 'Docker Desktop' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

<#
Makes sure a container engine is running and points DOCKER_CONTEXT at it for this process so docker compose uses it too.
Auto uses whichever of Rancher Desktop or Docker Desktop is already running, otherwise starts Rancher Desktop if installed, otherwise Docker Desktop.
Rancher or Docker uses that one, stopping the other first if it's running.
#>
function Start-ContainerRuntime([ValidateSet('Auto', 'Rancher', 'Docker')][string]$Runtime = 'Auto', [int]$TimeoutSeconds = 300) {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'docker CLI not found, install Rancher Desktop or Docker Desktop'
    }

    $rancherRunning = Test-RancherRunning
    $dockerRunning = Test-DockerDesktopRunning

    $useRancher = switch ($Runtime) {
        'Rancher' { $true }
        'Docker' { $false }
        default {
            if ($rancherRunning -and $dockerRunning) { [bool](Get-Process -Name 'Rancher Desktop' -ErrorAction SilentlyContinue) }
            elseif ($rancherRunning) { $true }
            elseif ($dockerRunning) { $false }
            else { [bool]$rancherDesktopExe }
        }
    }

    if ($useRancher) {
        if (-not $rancherDesktopExe) { throw 'Rancher Desktop is not installed' }
        if ($dockerRunning) {
            Stop-DockerDesktop
            $rancherRunning = [bool](Get-Process -Name 'Rancher Desktop' -ErrorAction SilentlyContinue)
        }
    }
    else {
        if (-not (Test-Path $dockerDesktopExe)) { throw 'Docker Desktop is not installed' }
        if ($rancherRunning) {
            Stop-RancherDesktop
            $dockerRunning = [bool](Get-Process -Name 'Docker Desktop' -ErrorAction SilentlyContinue)
        }
    }
    $name = if ($useRancher) { 'Rancher Desktop' } else { 'Docker Desktop' }

    $context = Find-DockerContext
    if ($context) {
        Write-Host "$name is running (context $context)" -ForegroundColor Green
        $env:DOCKER_CONTEXT = $context
        return
    }

    if (($useRancher -and $rancherRunning) -or (-not $useRancher -and $dockerRunning)) {
        Write-Host "$name is starting" -ForegroundColor Cyan
    }
    elseif ($useRancher) {
        Write-Host 'Starting Rancher Desktop' -ForegroundColor Cyan
        $rdctl = Get-Command rdctl -ErrorAction SilentlyContinue
        if ($rdctl) {
            $ErrorActionPreference = 'Continue'
            #the docker CLI needs the moby engine rather than containerd
            & $rdctl.Source start --container-engine.name=moby 2>&1 | Out-Null
        }
        else {
            Start-Process $rancherDesktopExe
        }
    }
    else {
        Write-Host 'Starting Docker Desktop' -ForegroundColor Cyan
        Start-Process $dockerDesktopExe
    }

    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    while ($timer.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        Start-Sleep -Seconds 3
        $context = Find-DockerContext
        if ($context) {
            Write-Host "$name is running (context $context) after $([int]$timer.Elapsed.TotalSeconds)s" -ForegroundColor Green
            $env:DOCKER_CONTEXT = $context
            return
        }
    }
    throw "$name didn't answer within $TimeoutSeconds seconds, check it for errors"
}
