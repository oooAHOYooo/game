#!/bin/bash
# sort_mixamo.sh
# Picks up Mixamo FBX files from ~/Downloads and puts them in the right
# project folders with exactly the names AnimationLibrary.cs expects.
#
# USAGE:  bash sort_mixamo.sh
#
# Run this from the project root after downloading from mixamo.com.
# Safe to run multiple times — already-placed files are skipped.

set -e

DOWNLOADS=~/Downloads
PROJECT=$(cd "$(dirname "$0")" && pwd)

LOCO="$PROJECT/Assets/Art/Animations/Locomotion"
COMBAT="$PROJECT/Assets/Art/Animations/Combat"
CHAR="$PROJECT/Assets/Art/Models/Character"

mkdir -p "$LOCO" "$COMBAT" "$CHAR"

# ─────────────────────────────────────────────────────────────────────────────
# Mapping table: "search pattern" → "destination folder/filename.fbx"
# Pattern is case-insensitive substring match against the downloaded filename.
# The FIRST matching rule wins, so put most-specific patterns first.
# ─────────────────────────────────────────────────────────────────────────────
declare -A RULES
# ── Locomotion ────────────────────────────────────────────────────────────────
RULES["Fall A Land To Standing Idle 01"]="$LOCO/Fall A Land To Standing Idle 01.fbx"
RULES["Hard Landing"]="$LOCO/Fall A Land To Standing Idle 01.fbx"
RULES["Fall To Idle"]="$LOCO/Fall A Land To Standing Idle 01.fbx"
RULES["Landing"]="$LOCO/Fall A Land To Standing Idle 01.fbx"

RULES["Fall A Loop"]="$LOCO/Fall A Loop.fbx"
RULES["Falling Idle"]="$LOCO/Fall A Loop.fbx"
RULES["Falling"]="$LOCO/Fall A Loop.fbx"
RULES["In Air"]="$LOCO/Fall A Loop.fbx"

RULES["Standing Sprint Forward"]="$LOCO/Standing Sprint Forward.fbx"
RULES["Sprint Forward"]="$LOCO/Standing Sprint Forward.fbx"
RULES["Sprinting"]="$LOCO/Standing Sprint Forward.fbx"

RULES["Standing Run Forward"]="$LOCO/Standing Run Forward.fbx"
RULES["Run Forward"]="$LOCO/Standing Run Forward.fbx"
RULES["Running"]="$LOCO/Standing Run Forward.fbx"

RULES["Standing Jump"]="$LOCO/Standing Jump.fbx"
RULES["Jumping"]="$LOCO/Standing Jump.fbx"

RULES["Breathing Idle"]="$LOCO/Idle.fbx"
RULES["Standing Idle"]="$LOCO/Idle.fbx"
RULES["Neutral Idle"]="$LOCO/Idle.fbx"
# "Idle" alone comes last so it doesn't grab "Falling Idle" etc.
# (handled separately below via exact-name logic)

# ── Combat ────────────────────────────────────────────────────────────────────
RULES["Standing Melee Attack Horizontal"]="$COMBAT/Standing Melee Attack Horizontal.fbx"
RULES["Spinning Kick"]="$COMBAT/Standing Melee Attack Horizontal.fbx"
RULES["High Kick"]="$COMBAT/Standing Melee Attack Horizontal.fbx"
RULES["Sweep Kick"]="$COMBAT/Standing Melee Attack Horizontal.fbx"

RULES["Standing Aim Overdraw"]="$COMBAT/Standing Aim Overdraw.fbx"
RULES["Standing Aim Recoil"]="$COMBAT/Standing Aim Overdraw.fbx"
RULES["Power Up"]="$COMBAT/Standing Aim Overdraw.fbx"
RULES["Yelling"]="$COMBAT/Standing Aim Overdraw.fbx"
RULES["Charging"]="$COMBAT/Standing Aim Overdraw.fbx"

RULES["Punching"]="$COMBAT/Punching.fbx"
RULES["Jab"]="$COMBAT/Punching.fbx"
RULES["Straight Punch"]="$COMBAT/Punching.fbx"

RULES["Uppercut"]="$COMBAT/Boxing (1).fbx"
RULES["Hook"]="$COMBAT/Boxing (1).fbx"
RULES["Standing Melee Attack Downward"]="$COMBAT/Boxing (1).fbx"
RULES["Boxing"]="$COMBAT/Boxing.fbx"   # plain Boxing → LightKick slot

RULES["Roundhouse Kick"]="$COMBAT/Boxing.fbx"
RULES["Standing Kick"]="$COMBAT/Boxing.fbx"
RULES["Low Kick"]="$COMBAT/Boxing.fbx"
RULES["Jump Kick"]="$COMBAT/Boxing.fbx"
RULES["Kick"]="$COMBAT/Boxing.fbx"

# ── Character model ───────────────────────────────────────────────────────────
RULES["X Bot"]="$CHAR/X Bot.fbx"
RULES["Y Bot"]="$CHAR/Y Bot.fbx"
RULES["xbot"]="$CHAR/xbot.fbx"
RULES["ybot"]="$CHAR/ybot.fbx"
RULES["Mannequin_Base"]="$CHAR/Mannequin_Base.fbx"
RULES["Mannequin"]="$CHAR/Mannequin.fbx"
RULES["Character"]="$CHAR/Character.fbx"
RULES["T-Pose"]="$CHAR/X Bot.fbx"       # generic T-Pose download
RULES["T Pose"]="$CHAR/X Bot.fbx"

# ─────────────────────────────────────────────────────────────────────────────
PLACED=0
SKIPPED=0
UNMATCHED=()

# Collect all FBX from Downloads (non-recursive)
shopt -s nocasematch
for fbx in "$DOWNLOADS"/*.fbx "$DOWNLOADS"/*.FBX; do
    [[ -f "$fbx" ]] || continue
    filename=$(basename "$fbx")
    stem="${filename%.*}"

    dest=""

    # Special case: exact "Idle" (not "Falling Idle" etc.)
    if [[ "$stem" == "Idle" ]]; then
        dest="$LOCO/Idle.fbx"
    else
        # Try each rule in definition order
        for pattern in "${!RULES[@]}"; do
            if [[ "$stem" == *"$pattern"* ]]; then
                dest="${RULES[$pattern]}"
                break
            fi
        done
    fi

    if [[ -z "$dest" ]]; then
        UNMATCHED+=("$filename")
        continue
    fi

    dest_name=$(basename "$dest")
    dest_dir=$(dirname "$dest")

    if [[ -f "$dest" ]]; then
        echo "  skip  $filename  →  already exists as $dest_name"
        ((SKIPPED++))
    else
        mkdir -p "$dest_dir"
        cp "$fbx" "$dest"
        echo "  ✓  $filename  →  $dest_name"
        ((PLACED++))
    fi
done

echo ""
echo "────────────────────────────────────"
echo "  Placed : $PLACED"
echo "  Skipped: $SKIPPED (already present)"

if [[ ${#UNMATCHED[@]} -gt 0 ]]; then
    echo ""
    echo "  Not recognised (add a rule to this script or rename manually):"
    for f in "${UNMATCHED[@]}"; do
        echo "    - $f"
    done
fi

echo ""
echo "Done. Now in Unity: NinjaStrike ▶ Setup Animated Characters"
