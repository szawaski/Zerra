<#
.SYNOPSIS
    Removes everything start-infrastructure.ps1 created in Docker: the zerra-demo containers, their network, and their data volumes.
    Services running locally outside Docker are never touched.

.PARAMETER Runtime
    Which container runtime holds the containers, it's started if needed: Auto (whichever is running, otherwise Rancher Desktop if installed), Rancher, or Docker.
    The containers live inside that runtime, so remove them with the same one that started them.

.PARAMETER RemoveImages
    Also delete the downloaded images.

.EXAMPLE
    .\remove-infrastructure.ps1 -RemoveImages
#>
param(
    [ValidateSet('Auto', 'Rancher', 'Docker')]
    [string]$Runtime = 'Auto',
    [switch]$RemoveImages
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'infrastructure-common.ps1')

$previousContext = $env:DOCKER_CONTEXT
try {
    Start-ContainerRuntime -Runtime $Runtime

    $arguments = @('compose', '--file', $composeFile, '--project-name', $composeProject, 'down', '--volumes', '--remove-orphans')
    if ($RemoveImages) { $arguments += @('--rmi', 'all') }

    Write-Host "Removing the $composeProject containers and volumes$(if ($RemoveImages) { ' and images' })" -ForegroundColor Cyan
    #compose writes its progress to stderr
    $ErrorActionPreference = 'Continue'
    & docker @arguments
    if ($LASTEXITCODE -ne 0) { throw 'docker compose down failed, see the output above' }
}
finally {
    $env:DOCKER_CONTEXT = $previousContext
}

Write-Host 'Removed' -ForegroundColor Green
