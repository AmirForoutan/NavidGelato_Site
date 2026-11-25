param(
    [string]$Destination = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$downloadUrl = "https://download.geofabrik.de/asia/iran-shortbread-1.0.mbtiles"

if ([string]::IsNullOrWhiteSpace($Destination)) {
    $Destination = Join-Path $PSScriptRoot "App_Data\Maps\iran-shortbread-1.0.mbtiles"
}

$Destination = [System.IO.Path]::GetFullPath($Destination)
$directory = Split-Path -Parent $Destination
$tempFile = "$Destination.partial"

if ((Test-Path $Destination) -and -not $Force) {
    Write-Host "Offline Iran map already exists:"
    Write-Host $Destination
    Write-Host "Use -Force to download it again."
    exit 0
}

New-Item -ItemType Directory -Path $directory -Force | Out-Null

if (Test-Path $tempFile) {
    Remove-Item $tempFile -Force
}

Write-Host "Downloading offline Iran vector map..."
Write-Host "Source: $downloadUrl"
Write-Host "Target: $Destination"
Write-Host "The archive is large (roughly 650 MB)."

$bits = Get-Command Start-BitsTransfer -ErrorAction SilentlyContinue

try {
    if ($null -ne $bits) {
        Start-BitsTransfer -Source $downloadUrl -Destination $tempFile -DisplayName "NavidPro Iran Offline Map"
    }
    else {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $tempFile -UseBasicParsing
    }

    $downloaded = Get-Item $tempFile
    if ($downloaded.Length -lt 100MB) {
        throw "Downloaded map archive is unexpectedly small: $($downloaded.Length) bytes."
    }

    Move-Item $tempFile $Destination -Force

    Write-Host ""
    Write-Host "Offline Iran map installed successfully."
    Write-Host "File: $Destination"
    Write-Host "Size: $([Math]::Round((Get-Item $Destination).Length / 1MB, 1)) MB"
    Write-Host ""
    Write-Host "No app restart is required. Refresh the home page and the vector map will activate automatically."
}
catch {
    if (Test-Path $tempFile) {
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    }

    throw
}
