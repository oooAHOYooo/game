#!/bin/bash
# build.sh - Build NinjaStrike for Linux

set -e

echo "🏗️  Building NinjaStrike for Linux x86_64..."

# Create build output directory
mkdir -p Builds/Linux

# Record build timestamp
date > build_timestamp.txt
echo "Build timestamp: $(cat build_timestamp.txt)"

# Run Unity build
/home/alexg/Unity/Hub/Editor/6000.3.9f1/Editor/Unity \
    -projectPath . \
    -executeMethod UnityEditor.Build.BuildPipeline.BuildPlayer \
    -buildTarget Linux64 \
    "Assets/OutdoorsScene.unity" \
    "Builds/Linux/NinjaStrike.x86_64" \
    -development \
    -logFile - \
    -quit \
    -nographics

# Copy executable to project root for easy access
if [ -f "Builds/Linux/NinjaStrike.x86_64" ]; then
    cp "Builds/Linux/NinjaStrike.x86_64" ./
    chmod +x ./NinjaStrike.x86_64
    echo "✅ Build complete! Run: ./run_game.sh"
else
    echo "❌ Build failed"
    exit 1
fi
