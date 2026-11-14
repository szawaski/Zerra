<#
.SYNOPSIS
    Pushes the Release package of every Zerra framework project to NuGet.

.DESCRIPTION
    Finds each project under Framework that builds a package, reads its <Version> from the
    .csproj, and pushes bin\Release\<PackageId>.<Version>.nupkg. The matching .snupkg next to
    it is pushed along with it. Packages already on the feed are skipped, so it is safe to rerun.

    Only the version in the .csproj is pushed, never just the newest file, so old or stray
    packages left in bin\Release are never uploaded.

    After a package is pushed, its listed prerelease versions older than the pushed version
    are unlisted, so only the newest RC shows on the feed. Newer versions are left alone.

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

.PARAMETER KeepPrereleases
    Leaves older prerelease versions listed.

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
    [switch]$Force,
    [switch]$KeepPrereleases
)

$ErrorActionPreference = 'Stop'

# SemVer 2 precedence, build metadata ignored
function Compare-PackageVersion([string]$a, [string]$b) {
    $a = $a.Split('+')[0]
    $b = $b.Split('+')[0]
    $aDash = $a.IndexOf('-')
    $bDash = $b.IndexOf('-')
    $aRelease = if ($aDash -ge 0) { $a.Substring(0, $aDash) } else { $a }
    $bRelease = if ($bDash -ge 0) { $b.Substring(0, $bDash) } else { $b }
    $aPre = if ($aDash -ge 0) { $a.Substring($aDash + 1) } else { '' }
    $bPre = if ($bDash -ge 0) { $b.Substring($bDash + 1) } else { '' }

    $aParts = @($aRelease.Split('.') | ForEach-Object { [long]$_ })
    $bParts = @($bRelease.Split('.') | ForEach-Object { [long]$_ })
    for ($i = 0; $i -lt [Math]::Max($aParts.Count, $bParts.Count); $i++) {
        $x = if ($i -lt $aParts.Count) { $aParts[$i] } else { 0 }
        $y = if ($i -lt $bParts.Count) { $bParts[$i] } else { 0 }
        if ($x -ne $y) { return [Math]::Sign($x - $y) }
    }

    if ($aPre -eq '' -and $bPre -eq '') { return 0 }
    if ($aPre -eq '') { return 1 }
    if ($bPre -eq '') { return -1 }

    $aIds = $aPre.Split('.')
    $bIds = $bPre.Split('.')
    for ($i = 0; $i -lt [Math]::Min($aIds.Count, $bIds.Count); $i++) {
        $aNumber = 0L
        $bNumber = 0L
        $aIsNumber = [long]::TryParse($aIds[$i], [ref]$aNumber)
        $bIsNumber = [long]::TryParse($bIds[$i], [ref]$bNumber)
        if ($aIsNumber -and $bIsNumber) {
            if ($aNumber -ne $bNumber) { return [Math]::Sign($aNumber - $bNumber) }
        }
        elseif ($aIsNumber) { return -1 }
        elseif ($bIsNumber) { return 1 }
        else {
            $compare = [string]::Compare($aIds[$i], $bIds[$i], [StringComparison]::OrdinalIgnoreCase)
            if ($compare -ne 0) { return [Math]::Sign($compare) }
        }
    }
    return [Math]::Sign($aIds.Count - $bIds.Count)
}

function Get-ListedVersions([string]$registrationBase, [string]$packageId) {
    try {
        $index = Invoke-RestMethod -Uri "$registrationBase$($packageId.ToLowerInvariant())/index.json"
    }
    catch {
        # a package that was never published has no registration
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) { return @() }
        throw
    }
    $versions = @()
    foreach ($page in $index.items) {
        $leaves = if ($page.items) { $page.items } else { (Invoke-RestMethod -Uri $page.'@id').items }
        foreach ($leaf in $leaves) {
            if ($leaf.catalogEntry.listed -ne $false) { $versions += $leaf.catalogEntry.version }
        }
    }
    return $versions
}

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

foreach ($package in $packages) {
    $package | Add-Member -NotePropertyName Unlist -NotePropertyValue @()
}

if (-not $KeepPrereleases) {
    $serviceIndex = Invoke-RestMethod -Uri $Source
    $registrationBase = ($serviceIndex.resources | Where-Object { $_.'@type' -eq 'RegistrationsBaseUrl/3.6.0' } | Select-Object -First 1).'@id'
    if (-not $registrationBase) {
        Write-Host "Warning: $Source has no SemVer 2 registration resource, older prereleases will not be unlisted." -ForegroundColor Yellow
    }
    else {
        foreach ($package in $packages) {
            $package.Unlist = @(Get-ListedVersions $registrationBase $package.Id | Where-Object {
                $_.Contains('-') -and (Compare-PackageVersion $_ $package.Version) -lt 0
            })
        }

        $unlistCount = ($packages | ForEach-Object { $_.Unlist.Count } | Measure-Object -Sum).Sum
        if ($unlistCount -gt 0) {
            Write-Host 'Prereleases to unlist after pushing' -ForegroundColor Cyan
            foreach ($package in $packages | Where-Object { $_.Unlist.Count -gt 0 }) {
                Write-Host ("  {0,-30} {1}" -f $package.Id, ($package.Unlist -join ', '))
            }
            Write-Host ''
        }
    }
}

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
    $unlistText = if ($unlistCount -gt 0) { " and unlist $unlistCount prereleases" } else { '' }
    $answer = Read-Host "Push $($packages.Count) packages$($unlistText)? Published versions can only be unlisted, not deleted (y/n)"
    if ($answer -ne 'y') {
        Write-Host 'Cancelled.' -ForegroundColor Yellow
        return
    }
}

# the first failure stops everything, so a bad API key or feed problem fails once instead of for every package
function Stop-Publish([string]$failure, [object[]]$pushed, [object[]]$remaining) {
    Write-Host ''
    Write-Host "Failed: $failure" -ForegroundColor Red
    if ($pushed.Count -gt 0) { Write-Host "Pushed before the failure: $(($pushed | ForEach-Object { $_.Id }) -join ', ')" -ForegroundColor Yellow }
    if ($remaining.Count -gt 0) { Write-Host "Not pushed: $(($remaining | ForEach-Object { $_.Id }) -join ', ')" -ForegroundColor Yellow }
    Write-Host 'Fix the problem and run again; packages already on the feed are skipped.' -ForegroundColor Yellow
    exit 1
}

for ($i = 0; $i -lt $packages.Count; $i++) {
    $package = $packages[$i]
    Write-Host "Pushing $($package.Id) $($package.Version)" -ForegroundColor Cyan
    & dotnet nuget push $package.Package --api-key $ApiKey --source $Source --skip-duplicate
    if ($LASTEXITCODE -ne 0) {
        Stop-Publish "push of $($package.Id) $($package.Version)" @($packages | Select-Object -First $i) @($packages | Select-Object -Skip $i)
    }

    # on nuget.org delete unlists, the version stays restorable
    foreach ($version in $package.Unlist) {
        Write-Host "Unlisting $($package.Id) $version" -ForegroundColor Cyan
        & dotnet nuget delete $package.Id $version --api-key $ApiKey --source $Source --non-interactive
        if ($LASTEXITCODE -ne 0) {
            Stop-Publish "unlist of $($package.Id) $version" @($packages | Select-Object -First ($i + 1)) @($packages | Select-Object -Skip ($i + 1))
        }
    }
}

Write-Host ''
Write-Host "Pushed $($packages.Count) packages." -ForegroundColor Green
