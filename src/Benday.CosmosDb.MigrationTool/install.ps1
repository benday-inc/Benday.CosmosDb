[CmdletBinding()]

param([Parameter(HelpMessage='Uninstall before installing')]
    [ValidateNotNullOrEmpty()]
    [switch]
    $reinstall)

if ($reinstall -eq $true)
{
    &.\uninstall.ps1
}

dotnet build

$pathToDebugFolder = Join-Path $PSScriptRoot 'bin\Debug'

Write-Host "Installing cosmosmigrator from $pathToDebugFolder"

dotnet tool install --global --add-source "$pathToDebugFolder" cosmosmigrator
