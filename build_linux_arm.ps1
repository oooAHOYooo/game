# Build script for Linux ARM64 (Raspberry Pi / Garuda)
$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
$projectPath = Get-Location
$buildDir = "Builds\RPi"

if (!(Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir
}

Write-Host "🏗️  Starting Unity Linux ARM64 Build (via BuildRPi.BuildForRPi)..." -ForegroundColor Cyan

& $unityPath -batchmode -projectPath "$projectPath" -executeMethod BuildRPi.BuildForRPi -logFile "$projectPath\unity_linux_build.log" -quit

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build complete! Locate it in: $buildDir" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed. Check unity_linux_build.log for details." -ForegroundColor Red
    exit $LASTEXITCODE
}
