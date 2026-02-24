#!/bin/bash
# run_game.sh - Launch NinjaStrike game

if [ -f build_timestamp.txt ]; then
    echo "Build timestamp: $(cat build_timestamp.txt)"
else
    echo "Build timestamp: unknown (run ./build.sh first)"
fi

if [ -f "./NinjaStrike.x86_64" ]; then
    exec ./NinjaStrike.x86_64
else
    echo "Error: NinjaStrike.x86_64 not found. Run ./build.sh to build the game."
    exit 1
fi
