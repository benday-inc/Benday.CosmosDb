# PowerShell script to start Azurite (Azure Storage Emulator) in Docker
# Usage: .\start-azurite.ps1 [-Remove] [-Pull]

param(
    [switch]$Remove,
    [switch]$Pull
)

if ($Remove) {
    Write-Host "Stopping, killing, and removing any existing 'azurite' container..."
    docker stop azurite 2>$null
    docker kill azurite 2>$null
    docker rm azurite 2>$null
}

if ($Pull) {
    Write-Host "Pulling the latest Azurite Docker image..."
    docker pull mcr.microsoft.com/azure-storage/azurite
}

# Start the Azurite container
Write-Host "Starting Azurite container..."
docker run --name azurite --detach --publish 10000:10000 --publish 10001:10001 --publish 10002:10002 mcr.microsoft.com/azure-storage/azurite azurite --skipApiVersionCheck --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0

# Show exposed ports
Write-Host "Exposed ports for 'azurite' container:"
docker port azurite

Write-Host
Write-Host "Azurite is running:"
Write-Host "  Blob service:  http://localhost:10000"
Write-Host "  Queue service: http://localhost:10001"
Write-Host "  Table service: http://localhost:10002"
