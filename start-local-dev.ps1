# PowerShell script to start all local development emulators
# Usage: .\start-local-dev.ps1 [-Remove] [-Pull]

param(
    [switch]$Remove,
    [switch]$Pull
)

$scriptDir = $PSScriptRoot

Write-Host "=== Starting local development environment ==="
Write-Host

Write-Host "--- Cosmos DB Emulator ---"
& "$scriptDir/start-cosmos-db-emulator.ps1" -Remove:$Remove -Pull:$Pull

Write-Host
Write-Host "--- Azurite (Azure Storage Emulator) ---"
& "$scriptDir/start-azurite.ps1" -Remove:$Remove -Pull:$Pull

Write-Host
Write-Host "=== Local development environment is ready ==="
