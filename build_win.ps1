# Build script for Windows 11
$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
$projectPath = Get-Location
$buildDir = "Builds\Windows"
$exePath = "$buildDir\NinjaStrike.exe"

if (!(Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir
}

Write-Host "🏗️  Starting Unity Windows Build..." -ForegroundColor Cyan

& $unityPath -batchmode -projectPath "$projectPath" -buildWindows64Player "$exePath" -logFile "$projectPath\unity_build.log" -quit

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build complete! Locate it at: $exePath" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed. Check unity_build.log for details." -ForegroundColor Red
    exit $LASTEXITCODE
}
