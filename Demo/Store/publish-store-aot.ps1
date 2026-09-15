<#
.SYNOPSIS
    Publishes the Store demo's six projects as native AOT executables, one folder per project.

.PARAMETER OutputDir
    Where each project's published executable goes, one subfolder per project. Defaults to ".\publish" next to this
    script, which is git-ignored.

.EXAMPLE
    .\publish-store-aot.ps1
#>
param(
    [string]$OutputDir = (Join-Path $PSScriptRoot 'publish')
)

$ErrorActionPreference = 'Stop'

#the native AOT linker needs vswhere.exe on PATH to find the VC++ toolchain, a plain shell usually doesn't have it
#(a Visual Studio Developer Command Prompt does, which is why this only bites from a script or a bare terminal)
$vswhereDir = 'C:\Program Files (x86)\Microsoft Visual Studio\Installer'
if ((Test-Path $vswhereDir) -and ($env:PATH -notlike "*$vswhereDir*")) {
    $env:PATH = "$vswhereDir;$env:PATH"
}

$projects = @(
    'Store.Catalog.Service',
    'Store.Inventory.Service',
    'Store.Orders.Service',
    'Store.Shipping.Service',
    'Store.Reviews.Service',
    'Store.Web'
)

foreach ($project in $projects) {
    Write-Host "Publishing $project" -ForegroundColor Cyan
    $projectPath = Join-Path $PSScriptRoot "$project\$project.csproj"
    $publishDir = Join-Path $OutputDir $project
    dotnet publish $projectPath -c Release -r win-x64 --self-contained -o $publishDir -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $project" }
}

Write-Host "Published to $OutputDir" -ForegroundColor Green
Write-Host "Run .\start-store-aot.ps1 to start the native executables" -ForegroundColor Green
