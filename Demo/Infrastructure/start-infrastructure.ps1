<#
.SYNOPSIS
    Starts the data stores (SQL Server, MySQL, MariaDB, PostgreSQL, KurrentDB) and messaging services (Kafka, RabbitMQ, Azure Service Bus emulator)
    the demos use, in Docker, on their default ports and demo credentials. Anything already running locally is left alone.

.DESCRIPTION
    Each service is checked for a listening port (and, for SQL Server, a running local instance). Only the missing ones are started,
    grouped under the zerra-demo compose project. Rancher Desktop or Docker Desktop is started first if no container engine is running,
    and only when something is missing. Run remove-infrastructure.ps1 to remove the containers, volumes, and optionally the images.

.PARAMETER Runtime
    Which container runtime to use. Auto uses whichever is already running, otherwise starts Rancher Desktop if installed, otherwise Docker Desktop.
    Rancher or Docker uses that one, stopping the other first since they can't run at the same time.

.PARAMETER Only
    Limit to these services: mssql, mysql, mariadb, postgresql, kurrentdb, kafka, rabbitmq, servicebus.

.PARAMETER Exclude
    Skip these services.

.EXAMPLE
    .\start-infrastructure.ps1
    .\start-infrastructure.ps1 -Runtime Docker -Exclude servicebus
#>
param(
    [ValidateSet('Auto', 'Rancher', 'Docker')]
    [string]$Runtime = 'Auto',
    [ValidateSet('mssql', 'mysql', 'mariadb', 'postgresql', 'kurrentdb', 'kafka', 'rabbitmq', 'servicebus')]
    [string[]]$Only,
    [ValidateSet('mssql', 'mysql', 'mariadb', 'postgresql', 'kurrentdb', 'kafka', 'rabbitmq', 'servicebus')]
    [string[]]$Exclude
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'infrastructure-common.ps1')

#Ports and credentials match the demo defaults in Demo/Store/Store.Common/StoreSettings.cs and the docs
$services = @(
    [pscustomobject]@{ Name = 'mssql';      Display = 'SQL Server';                 Port = 1433; MemoryMB = 800;  LocalService = '^MSSQLSERVER$'; Connection = 'Data Source=localhost,1433;User ID=sa;Password=Password123;TrustServerCertificate=True' }
    [pscustomobject]@{ Name = 'mysql';      Display = 'MySQL';                      Port = 3306; MemoryMB = 200;  LocalService = '^MySQL';        Connection = 'Server=localhost;Port=3306;Uid=root;Pwd=password123' }
    [pscustomobject]@{ Name = 'mariadb';    Display = 'MariaDB';                    Port = 3307; MemoryMB = 120;  LocalService = '^MariaDB';      Connection = 'Server=localhost;Port=3307;Uid=root;Pwd=password123' }
    [pscustomobject]@{ Name = 'postgresql'; Display = 'PostgreSQL';                 Port = 5432; MemoryMB = 60;   LocalService = '^postgresql';   Connection = 'Host=localhost;Port=5432;User ID=postgres;Password=password123' }
    [pscustomobject]@{ Name = 'kurrentdb';  Display = 'KurrentDB';                  Port = 2113; MemoryMB = 420;  LocalService = '^(KurrentDB|EventStore)'; Connection = 'kurrentdb://localhost:2113?tls=false (UI http://localhost:2113)' }
    [pscustomobject]@{ Name = 'kafka';      Display = 'Kafka';                      Port = 9092; MemoryMB = 320;  LocalService = '^Kafka';        Connection = 'localhost:9092' }
    [pscustomobject]@{ Name = 'rabbitmq';   Display = 'RabbitMQ';                   Port = 5672; MemoryMB = 130;  LocalService = '^RabbitMQ';     Connection = 'localhost (guest/guest, UI http://localhost:15672)' }
    [pscustomobject]@{ Name = 'servicebus'; Display = 'Azure Service Bus emulator'; Port = 5300; MemoryMB = 1100; LocalService = $null;           Connection = 'Endpoint=sb://localhost:5673;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;' }
)
if ($Only) { $services = @($services | Where-Object { $Only -contains $_.Name }) }
if ($Exclude) { $services = @($services | Where-Object { $Exclude -notcontains $_.Name }) }

#Docker's port forwarding accepts connections even when the container's process isn't listening, so the emulator is checked by its health endpoint
function Test-ServiceBusEmulator {
    try { return (Invoke-WebRequest -Uri 'http://127.0.0.1:5300/health' -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200 } catch { return $false }
}

function Test-LocalPort([int]$port) {
    foreach ($address in @('127.0.0.1', '::1')) {
        $client = New-Object System.Net.Sockets.TcpClient($(if ($address -eq '::1') { 'InterNetworkV6' } else { 'InterNetwork' }))
        try {
            if ($client.ConnectAsync($address, $port).Wait(1000) -and $client.Connected) { return $true }
        }
        catch { }
        finally { $client.Dispose() }
    }
    return $false
}

Write-Host 'Checking for services already running locally' -ForegroundColor Cyan
$windowsServices = @(Get-Service -ErrorAction SilentlyContinue)
$missing = @()
foreach ($service in $services) {
    $local = if ($service.LocalService) { @($windowsServices | Where-Object { $_.Name -match $service.LocalService }) } else { @() }
    $localRunning = @($local | Where-Object { $_.Status -eq 'Running' })

    if ($service.Name -eq 'servicebus') {
        if (Test-ServiceBusEmulator) {
            Write-Host "  $($service.Display): running, health endpoint on port 5300 answered" -ForegroundColor Green
        }
        else {
            Write-Host "  $($service.Display): missing" -ForegroundColor Yellow
            $missing += $service
        }
    }
    elseif (Test-LocalPort $service.Port) {
        Write-Host "  $($service.Display): running, port $($service.Port) is listening" -ForegroundColor Green
    }
    #the demo's SQL Server connection is Data Source=., which a local default instance answers over shared memory even with TCP disabled
    elseif ($service.Name -eq 'mssql' -and $localRunning.Count -gt 0) {
        Write-Host "  $($service.Display): running, local service $($localRunning[0].Name)" -ForegroundColor Green
    }
    else {
        $note = if ($local.Count -gt 0) { " (local service $($local[0].Name) is installed but $($local[0].Status), starting it later will conflict with the container's port)" } else { '' }
        Write-Host "  $($service.Display): missing$note" -ForegroundColor Yellow
        $missing += $service
    }
}

if ($missing.Count -eq 0) {
    Write-Host 'Everything is already running' -ForegroundColor Green
    return
}

#Rancher Desktop and Docker Desktop both run in the WSL2 VM, if .wslconfig caps its memory below what the containers need the VM stops answering
$wslConfig = Join-Path $env:USERPROFILE '.wslconfig'
if (Test-Path $wslConfig) {
    $memoryLine = Get-Content $wslConfig | Where-Object { $_ -match '^\s*memory\s*=\s*(\d+)\s*(GB|MB)\s*$' } | Select-Object -First 1
    if ($memoryLine -and $memoryLine -match '^\s*memory\s*=\s*(\d+)\s*(GB|MB)\s*$') {
        $limitMB = if ($Matches[2] -eq 'GB') { [int]$Matches[1] * 1024 } else { [int]$Matches[1] }
        #plus the VM and runtime, measured at about 0.6 GB for Rancher Desktop, 1.7 GB with its Kubernetes enabled
        $neededMB = ($missing | Measure-Object -Property MemoryMB -Sum).Sum + 700
        if ($limitMB -lt $neededMB) {
            Write-Warning "$wslConfig limits WSL to $($limitMB / 1024) GB but these containers need about $([math]::Ceiling($neededMB / 1024)) GB, the container engine may stop responding. Raise memory= in that file and run: wsl --shutdown"
        }
    }
}

$previousContext = $env:DOCKER_CONTEXT
try {
    Start-ContainerRuntime -Runtime $Runtime

    $names = @($missing | ForEach-Object { $_.Name })
    Write-Host "Starting $($names -join ', ') in Docker (the first run pulls the images, which can take a while)" -ForegroundColor Cyan
    #compose writes its progress to stderr
    $ErrorActionPreference = 'Continue'
    #the engine's connection can drop briefly, up is safe to repeat so it's retried before giving up
    function Invoke-ComposeUp([string[]]$arguments) {
        for ($attempt = 1; $attempt -le 3; $attempt++) {
            & docker compose --file $composeFile --project-name $composeProject up --detach --wait --wait-timeout 900 @arguments
            if ($LASTEXITCODE -eq 0) { return }
            if ($attempt -lt 3) {
                Write-Host "docker compose failed, retrying ($attempt of 2)" -ForegroundColor Yellow
                Start-Sleep -Seconds 10
            }
        }
        throw "docker compose failed, see the output above or run: docker compose --file `"$composeFile`" --project-name $composeProject logs"
    }

    $others = @($names | Where-Object { $_ -ne 'servicebus' })
    if ($others.Count -gt 0) {
        Invoke-ComposeUp $others
    }
    if ($names -contains 'servicebus') {
        #the emulator fails on database files left from a previous run, so it and its SQL Server always start from new containers
        Invoke-ComposeUp @('--force-recreate', 'servicebus-sql', 'servicebus')
    }

    #the emulator has no healthcheck in its image, wait for its health endpoint so it's ready when this returns
    if ($names -contains 'servicebus') {
        $timer = [System.Diagnostics.Stopwatch]::StartNew()
        $ready = $false
        while (-not ($ready = Test-ServiceBusEmulator) -and $timer.Elapsed.TotalSeconds -lt 180) {
            Start-Sleep -Seconds 3
        }
        if (-not $ready) { Write-Warning 'Azure Service Bus emulator did not report healthy, check: docker logs zerra-servicebus' }
    }
}
finally {
    $env:DOCKER_CONTEXT = $previousContext
}

Write-Host ''
Write-Host 'Started in Docker:' -ForegroundColor Green
foreach ($service in $missing) {
    Write-Host "  $($service.Display): $($service.Connection)"
}
