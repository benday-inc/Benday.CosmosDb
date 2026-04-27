# PowerShell script to start Azure Cosmos DB Emulator in Docker
# Usage: .\start-cosmos-db-emulator.ps1 [-Remove]

param(
    [switch]$Remove,
    [switch]$Pull
)

if ($Remove) {
    Write-Host "Stopping, killing, and removing any existing 'cosmosdb' container..."
    docker stop cosmosdb 2>$null
    docker kill cosmosdb 2>$null
    docker rm cosmosdb 2>$null
}

if ($Pull) {
    Write-Host "Pulling the latest Azure Cosmos DB Emulator Docker image..."
    docker pull mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview
}

# Start the Cosmos DB Emulator container
Write-Host "Starting Azure Cosmos DB Emulator container..."
docker run --name cosmosdb --detach --publish 8081:8081 --publish 1234:1234 -e PROTOCOL=https mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview --protocol https

# Show exposed ports
Write-Host "Exposed ports for 'cosmosdb' container:"
docker port cosmosdb