# Zip the Windows build for distribution
$sourceDir = Join-Path $PSScriptRoot "Builds/Windows"
$destinationFile = Join-Path $PSScriptRoot "NinjaStrike_Windows.zip"

if (Test-Path $destinationFile) {
    Remove-Item $destinationFile -Force
}

Write-Host "📦 Zipping build for students..." -ForegroundColor Cyan

# Create the zip
Compress-Archive -Path "$sourceDir/*" -DestinationPath $destinationFile

if (Test-Path $destinationFile) {
    Write-Host "✅ Zip created: NinjaStrike_Windows.zip" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to create zip." -ForegroundColor Red
    exit 1
}
