<#
.SYNOPSIS
    Builds and starts the Store demo: the Catalog, Inventory, Orders, Shipping, and Reviews services and the web gateway, each in its own window.

.PARAMETER InMemory
    Skip the databases and run every service on its in-memory store.

.PARAMETER DirectMessaging
    Skip the message brokers and send every command and event directly between services over TCP or HTTP.

.PARAMETER NoBrowser
    Don't open the site in the browser.

.EXAMPLE
    .\start-store.ps1 -InMemory
#>
param(
    [switch]$InMemory,
    [switch]$DirectMessaging,
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'

$projects = @(
    'Store.Catalog.Service',
    'Store.Inventory.Service',
    'Store.Orders.Service',
    'Store.Shipping.Service',
    'Store.Reviews.Service',
    'Store.Web'
)

foreach ($project in $projects) {
    Write-Host "Building $project" -ForegroundColor Cyan
    dotnet build (Join-Path $PSScriptRoot "$project\$project.csproj") -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $project" }
}

#the service windows inherit these, they're restored afterwards so they don't stick to this shell
$previousInMemory = $env:STORE_IN_MEMORY
$previousDirectMessaging = $env:STORE_DIRECT_MESSAGING
$env:STORE_IN_MEMORY = if ($InMemory) { 'true' } else { $null }
$env:STORE_DIRECT_MESSAGING = if ($DirectMessaging) { 'true' } else { $null }
try {
    foreach ($project in $projects) {
        Write-Host "Starting $project" -ForegroundColor Green
        $directory = Join-Path $PSScriptRoot $project
        #-NoExit keeps the window open if the service stops, so its output can still be read
        Start-Process powershell.exe -WorkingDirectory $directory -ArgumentList @('-NoExit', '-NoProfile', '-Command', "dotnet run --no-build --project '$directory'")
    }
}
finally {
    $env:STORE_IN_MEMORY = $previousInMemory
    $env:STORE_DIRECT_MESSAGING = $previousDirectMessaging
}

Write-Host "Store demo starting at http://localhost:5100, close the windows or press Ctrl+C in each to stop" -ForegroundColor Green
if (-not $NoBrowser) {
    Start-Sleep -Seconds 5
    Start-Process 'http://localhost:5100'
}
