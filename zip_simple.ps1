# Simple zip
Remove-Item -Path "NinjaStrike_Windows.zip" -ErrorAction SilentlyContinue
Compress-Archive -Path "Builds/Windows/*" -DestinationPath "NinjaStrike_Windows.zip"
Write-Host "Done"
