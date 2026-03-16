# Build script for Windows (x64)
$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
$projectPath = Get-Location
$buildDir = "Builds\Windows"

if (!(Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir
}

Write-Host "🏗️  Starting Unity Windows Build (via BuildWindows.BuildForWindows)..." -ForegroundColor Cyan

& "$unityPath" -batchmode -projectPath "$projectPath" -executeMethod BuildWindows.BuildForWindows -logFile "$projectPath\unity_windows_build.log" -quit

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Windows Build complete! Locate it in: $buildDir" -ForegroundColor Green
} else {
    Write-Host "❌ Windows Build failed. Check unity_windows_build.log for details." -ForegroundColor Red
    exit $LASTEXITCODE
}
