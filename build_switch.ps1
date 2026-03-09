# Build script for Nintendo Switch
# Note: Requires Nintendo Switch build support to be installed in Unity Hub
$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe"
$projectPath = Get-Location
$buildDir = "Builds\Switch"
$nspPath = "$buildDir\NinjaStrike.nsp"

if (!(Test-Path $buildDir)) {
    New-Item -ItemType Directory -Path $buildDir
}

Write-Host "🏗️  Starting Unity Nintendo Switch Build..." -ForegroundColor Yellow

# Using -buildTarget Switch for Nintendo Switch
& $unityPath -batchmode -projectPath "$projectPath" -buildTarget Switch -nographics -logFile "$projectPath\unity_switch_build.log" -quit

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Switch Build complete! Check $buildDir for output." -ForegroundColor Green
} else {
    Write-Host "❌ Switch Build failed. Check unity_switch_build.log for details." -ForegroundColor Red
    Write-Host "⚠️  Note: Switch builds require the Nintendo Unity SDK to be installed." -ForegroundColor Gray
    exit $LASTEXITCODE
}
