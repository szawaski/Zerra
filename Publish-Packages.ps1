<#
.SYNOPSIS
    Pushes the Release package of every Zerra framework project to NuGet.

.DESCRIPTION
    Finds each project under Framework that builds a package, reads its <Version> from the
    .csproj, and pushes bin\Release\<PackageId>.<Version>.nupkg. The matching .snupkg next to
    it is pushed along with it. Packages already on the feed are skipped, so it is safe to rerun.

    Only the version in the .csproj is pushed, never just the newest file, so old or stray
    packages left in bin\Release are never uploaded.

.PARAMETER ApiKey
    NuGet API key. Defaults to the NUGET_API_KEY environment variable, then asks for it.

.PARAMETER Source
    Feed to push to. Defaults to nuget.org.

.PARAMETER Build
    Builds each project in Release first.

.PARAMETER DryRun
    Lists what would be pushed without pushing.

.PARAMETER Force
    Pushes without asking to confirm.

.EXAMPLE
    .\Publish-Packages.ps1 -DryRun
.EXAMPLE
    .\Publish-Packages.ps1 -Build
#>
param(
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$Source = 'https://api.nuget.org/v3/index.json',
    [switch]$Build,
    [switch]$DryRun,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$frameworkPath = Join-Path $PSScriptRoot 'Framework'

# dependencies first so a package never lands on the feed before the ones it needs
$firstProjects = @('Zerra', 'Zerra.Repository')

$packages = @()
foreach ($projectFile in Get-ChildItem -Path $frameworkPath -Filter '*.csproj' -Recurse) {
    [xml]$project = Get-Content -Path $projectFile.FullName -Raw
    $properties = $project.Project.PropertyGroup

    $generatesPackage = $properties | Where-Object { $_.GeneratePackageOnBuild -eq 'true' }
    if (-not $generatesPackage) { continue }

    $version = ($properties | Where-Object { $_.Version } | Select-Object -First 1).Version
    if (-not $version) { throw "No <Version> in $($projectFile.FullName)" }

    $packageId = ($properties | Where-Object { $_.PackageId } | Select-Object -First 1).PackageId
    if (-not $packageId) { $packageId = $projectFile.BaseName }

    $order = [array]::IndexOf($firstProjects, $packageId)
    if ($order -lt 0) { $order = $firstProjects.Length }

    $packages += [pscustomobject]@{
        Id          = $packageId
        Version     = $version
        Order       = $order
        ProjectFile = $projectFile.FullName
        Package     = Join-Path $projectFile.DirectoryName "bin\Release\$packageId.$version.nupkg"
    }
}

$packages = $packages | Sort-Object Order, Id

if ($packages.Count -eq 0) { throw "No packable projects found under $frameworkPath" }

if ($Build) {
    foreach ($package in $packages) {
        Write-Host "Building $($package.Id) $($package.Version)" -ForegroundColor Cyan
        & dotnet build $package.ProjectFile -c Release --nologo -v minimal
        if ($LASTEXITCODE -ne 0) { throw "Build failed for $($package.Id)" }
    }
}

$missing = $packages | Where-Object { -not (Test-Path $_.Package) }
if ($missing) {
    $missing | ForEach-Object { Write-Host "Missing: $($_.Package)" -ForegroundColor Red }
    throw 'Some packages are not built. Build them in Release or run with -Build.'
}

$versions = $packages | Select-Object -ExpandProperty Version -Unique
if (@($versions).Count -gt 1) {
    Write-Host "Warning: projects have different versions: $($versions -join ', ')" -ForegroundColor Yellow
}

Write-Host ''
Write-Host "Packages for $Source" -ForegroundColor Cyan
foreach ($package in $packages) {
    $file = Get-Item $package.Package
    $symbols = if (Test-Path ([IO.Path]::ChangeExtension($package.Package, '.snupkg'))) { '+ symbols' } else { '' }
    Write-Host ("  {0,-30} {1,-14} built {2:yyyy-MM-dd HH:mm} {3}" -f $package.Id, $package.Version, $file.LastWriteTime, $symbols)
}
Write-Host ''

if ($DryRun) {
    Write-Host 'Dry run, nothing pushed.' -ForegroundColor Yellow
    return
}

if (-not $ApiKey) {
    $secureKey = Read-Host -Prompt 'NuGet API key' -AsSecureString
    $ApiKey = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey))
    if (-not $ApiKey) { throw 'No API key given.' }
}

if (-not $Force) {
    $answer = Read-Host "Push $($packages.Count) packages? Published versions can only be unlisted, not deleted (y/n)"
    if ($answer -ne 'y') {
        Write-Host 'Cancelled.' -ForegroundColor Yellow
        return
    }
}

$failed = @()
foreach ($package in $packages) {
    Write-Host "Pushing $($package.Id) $($package.Version)" -ForegroundColor Cyan
    & dotnet nuget push $package.Package --api-key $ApiKey --source $Source --skip-duplicate
    if ($LASTEXITCODE -ne 0) { $failed += $package.Id }
}

Write-Host ''
if ($failed.Count -gt 0) {
    Write-Host "Failed: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
Write-Host "Pushed $($packages.Count) packages." -ForegroundColor Green
