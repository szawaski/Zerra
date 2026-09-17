<#
.SYNOPSIS
    Starts the Store demo's native AOT executables, each in its own window. Run .\publish-store-aot.ps1 first.

.PARAMETER PublishDir
    Where the published executables are, one subfolder per project. Defaults to ".\publish" next to this script,
    matching publish-store-aot.ps1's default output.

.PARAMETER InMemory
    Skip the databases and run every service on its in-memory store.

.PARAMETER DirectMessaging
    Skip the message brokers and send every command and event directly between services over TCP or HTTP.

.PARAMETER NoBrowser
    Don't open the site in the browser.

.EXAMPLE
    .\start-store-aot.ps1 -InMemory
#>
param(
    [string]$PublishDir = (Join-Path $PSScriptRoot 'publish'),
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
    $exePath = Join-Path $PublishDir "$project\$project.exe"
    if (-not (Test-Path $exePath)) { throw "$exePath not found, run .\publish-store-aot.ps1 first" }
}

#the service windows inherit these, they're restored afterwards so they don't stick to this shell
$previousInMemory = $env:STORE_IN_MEMORY
$previousDirectMessaging = $env:STORE_DIRECT_MESSAGING
$env:STORE_IN_MEMORY = if ($InMemory) { 'true' } else { $null }
$env:STORE_DIRECT_MESSAGING = if ($DirectMessaging) { 'true' } else { $null }
try {
    foreach ($project in $projects) {
        Write-Host "Starting $project" -ForegroundColor Green
        $exePath = Join-Path $PublishDir "$project\$project.exe"
        $directory = Split-Path $exePath -Parent

        #a published exe has no launchSettings.json, the two ASP.NET Core services need their listen address set explicitly
        $urlSetter = switch ($project) {
            'Store.Web' { "`$env:ASPNETCORE_URLS = 'http://localhost:5100'; " }
            'Store.Shipping.Service' { "`$env:ASPNETCORE_URLS = 'http://localhost:9105'; " }
            default { '' }
        }

        #-NoExit keeps the window open if the service stops, so its output can still be read
        Start-Process powershell.exe -WorkingDirectory $directory -ArgumentList @('-NoExit', '-NoProfile', '-Command', "$urlSetter& '$exePath'")
    }
}
finally {
    $env:STORE_IN_MEMORY = $previousInMemory
    $env:STORE_DIRECT_MESSAGING = $previousDirectMessaging
}

Write-Host "Store demo (native AOT) starting at http://localhost:5100, close the windows or press Ctrl+C in each to stop" -ForegroundColor Green
if (-not $NoBrowser) {
    Start-Sleep -Seconds 5
    Start-Process 'http://localhost:5100'
}
