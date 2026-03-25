# Copies the highest version nupkg for each package to the local NuGet feed folder.

$scriptDir = $PSScriptRoot

if ($IsWindows -or $env:OS -match "Windows") {
    $destDir = "C:\LocalNuGet"
} else {
    $destDir = Join-Path $HOME "LocalNuGet"
}

if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir | Out-Null
    Write-Host "Created directory: $destDir"
}

# Find all nupkg files (exclude obj folders and snupkg)
$allPackages = Get-ChildItem -Path $scriptDir -Recurse -Filter "*.nupkg" |
    Where-Object { $_.FullName -notmatch '[/\\]obj[/\\]' -and $_.Extension -eq '.nupkg' }

# Group by package name (everything before the version number)
$grouped = $allPackages | ForEach-Object {
    if ($_.BaseName -match '^(.+?)\.(\d+\..+)$') {
        [PSCustomObject]@{
            PackageName = $Matches[1]
            File        = $_
        }
    }
} | Group-Object PackageName

foreach ($group in $grouped) {
    # Pick the most recently modified file as the "highest version"
    $latest = $group.Group | Sort-Object { $_.File.LastWriteTime } -Descending | Select-Object -First 1
    $dest = Join-Path $destDir $latest.File.Name
    Copy-Item -Path $latest.File.FullName -Destination $dest -Force
    Write-Host "Copied $($latest.File.Name) -> $destDir"
}

Write-Host ""
Write-Host "Done. $($grouped.Count) package(s) copied to $destDir"
