# PowerShell script to start Azure Cosmos DB Emulator in Docker
# Usage: .\start-cosmos-db-emulator.ps1 [-Remove] [-Pull] [-Refresh]
#                                       [-GatewayEndpoint <address>] [-ConfigureForGuestVm]

param(
    [switch]$Remove,
    [switch]$Pull,
    [switch]$Refresh,
    # Address the emulator advertises to clients. Set this to the address a
    # remote machine (e.g. a Parallels guest VM) uses to reach this host,
    # otherwise the emulator advertises "localhost" and remote clients fail.
    [string]$GatewayEndpoint,
    # Convenience switch for accessing the emulator from a Parallels guest VM:
    # advertises the Parallels shared-networking host IP and uses https-insecure
    # so the guest SDK doesn't fail cert validation against an IP endpoint.
    [switch]$ConfigureForGuestVm
)

# Parallels shared-networking host IP (the address a guest uses to reach the host)
$parallelsHostIp = "10.211.55.2"

if ($Refresh) {
    $Remove = $true
    $Pull = $true
}

$imageName = "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-latest"
$containerName = "cosmosdb"
$defaultProtocol = "https" # http, https, https-insecure
$explorerProtocol = $defaultProtocol
$endpointProtocol = $defaultProtocol

if ($ConfigureForGuestVm) {
    # Advertise the host address the guest uses, and skip cert validation since
    # the self-signed cert won't match the IP endpoint.
    if (-not $GatewayEndpoint) {
        $GatewayEndpoint = $parallelsHostIp
    }
    $endpointProtocol = "https-insecure"
    $explorerProtocol = "https-insecure"
    Write-Host "Configuring for Parallels guest VM access: gateway endpoint '$GatewayEndpoint', protocol 'https-insecure'."
}

if ($Remove) {
    Write-Host "Stopping, killing, and removing any existing 'cosmosdb' container..."
    docker stop $containerName 2>$null
    docker kill $containerName 2>$null
    docker rm $containerName 2>$null
}



if ($Pull) {
    Write-Host "Pulling the latest Azure Cosmos DB Emulator Docker image..."
    docker pull $imageName
}

# Start the Cosmos DB Emulator container
Write-Host "Starting Azure Cosmos DB Emulator container..."
# docker run --name cosmosdb --detach --publish 8081:8081 --publish 1234:1234 -e PROTOCOL=https $imageName --protocol https


# docker options (go BEFORE the image name)
$dockerOptions = @(
    "--name", $containerName
    "--detach"
    "--publish", "8081:8081"
    "--publish", "8080:8080"
    "--publish", "1234:1234"
)

# emulator arguments (go AFTER the image name; passed to the container entrypoint)
$emulatorArgs = @(
    "--protocol", $endpointProtocol
    "--explorer-protocol", $explorerProtocol
)

if ($GatewayEndpoint) {
    $emulatorArgs += "--gateway-endpoint", $GatewayEndpoint
}

Write-Host "Running: docker run $($dockerOptions -join ' ') $imageName $($emulatorArgs -join ' ')"
docker run @dockerOptions $imageName @emulatorArgs


# Show exposed ports
Write-Host "Exposed ports for 'cosmosdb' container:"
docker port cosmosdb

Write-Host
Write-Host "The admin site is available at http://localhost:1234 "
