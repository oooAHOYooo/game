#!/usr/bin/env bash
# =============================================================
#  sync.sh — Unity project sync & pre-compile
#  Usage (from project root):  ./sync.sh
#
#  What it does:
#    1. Pulls the latest code from git (with rebase)
#    2. Runs Unity in headless batch mode to force a full
#       script compile + asset import
#    3. Exits so Unity is warm and ready when you open it
# =============================================================

set -euo pipefail

# ── Colours ──────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
RESET='\033[0m'

# ── Config ───────────────────────────────────────────────────
UNITY_EXE="/home/alexg/Unity/Hub/Editor/6000.3.9f1/Editor/Unity"
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LOG_DIR="$PROJECT_DIR/.sync_logs"
LOG_FILE="$LOG_DIR/unity_compile_$(date +%Y%m%d_%H%M%S).log"
LATEST_LOG="$LOG_DIR/latest.log"

mkdir -p "$LOG_DIR"

# ── Helpers ──────────────────────────────────────────────────
step()    { echo -e "\n${CYAN}${BOLD}▶ $1${RESET}"; }
ok()      { echo -e "${GREEN}✔  $1${RESET}"; }
warn()    { echo -e "${YELLOW}⚠  $1${RESET}"; }
fail()    { echo -e "${RED}✖  $1${RESET}"; }
hr()      { echo -e "${CYAN}────────────────────────────────────────${RESET}"; }

# ── Banner ───────────────────────────────────────────────────
clear
hr
echo -e "${BOLD}  🎮  Unity Project Sync${RESET}"
echo -e "  Project : ${CYAN}$PROJECT_DIR${RESET}"
echo -e "  Unity   : ${CYAN}$UNITY_EXE${RESET}"
echo -e "  Time    : $(date '+%Y-%m-%d %H:%M:%S')"
hr

# ── 1. Sanity checks ─────────────────────────────────────────
step "Checking environment"

if [ ! -f "$UNITY_EXE" ]; then
  fail "Unity not found at: $UNITY_EXE"
  echo "    Edit UNITY_EXE in this script to point to your Unity installation."
  exit 1
fi
ok "Unity found"

if ! git -C "$PROJECT_DIR" rev-parse --git-dir &>/dev/null; then
  fail "Not a git repository: $PROJECT_DIR"
  exit 1
fi
ok "Git repo detected"

# ── 2. Check for uncommitted local changes ───────────────────
step "Checking local working tree"

if ! git -C "$PROJECT_DIR" diff --quiet || ! git -C "$PROJECT_DIR" diff --cached --quiet; then
  warn "You have uncommitted local changes:"
  git -C "$PROJECT_DIR" status --short
  echo ""
  read -rp "  Continue anyway? They won't be lost. [y/N] " confirm
  if [[ ! "$confirm" =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 0
  fi
else
  ok "Working tree is clean"
fi

# ── 3. Git pull ──────────────────────────────────────────────
step "Pulling latest from remote (rebase)"

BEFORE=$(git -C "$PROJECT_DIR" rev-parse HEAD)

if git -C "$PROJECT_DIR" pull --rebase --autostash 2>&1; then
  AFTER=$(git -C "$PROJECT_DIR" rev-parse HEAD)
  if [ "$BEFORE" = "$AFTER" ]; then
    ok "Already up to date — no new commits"
  else
    COUNT=$(git -C "$PROJECT_DIR" rev-list --count "$BEFORE".."$AFTER")
    ok "Pulled $COUNT new commit(s)"
    echo ""
    git -C "$PROJECT_DIR" log --oneline "$BEFORE".."$AFTER" | sed 's/^/    /'
  fi
else
  fail "Git pull failed. Fix merge conflicts then re-run."
  exit 1
fi

# ── 4. Unity batch-mode compile & import ────────────────────
step "Running Unity in batch mode (compile + import)"
echo -e "  This may take 30–90 seconds on first run..."
echo -e "  Log: ${CYAN}$LOG_FILE${RESET}"
echo ""

UNITY_START=$(date +%s)

# -quit          : exit after done
# -batchmode     : no GUI
# -nographics    : skip GPU init
# -projectPath   : our project
# -logFile       : write output here
# We use || true so we can inspect the log ourselves for real errors

"$UNITY_EXE" \
  -quit \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_DIR" \
  -logFile "$LOG_FILE" \
  2>/dev/null || true

UNITY_END=$(date +%s)
UNITY_DURATION=$(( UNITY_END - UNITY_START ))

# Copy to latest.log for easy access
cp "$LOG_FILE" "$LATEST_LOG"

# ── 5. Check compile result ──────────────────────────────────
step "Checking compile result"

if grep -q "Scripts have compiler errors" "$LOG_FILE" 2>/dev/null; then
  fail "Compiler errors detected!"
  echo ""
  echo -e "${YELLOW}  Errors from Unity log:${RESET}"
  grep -E "(error CS|Scripts have compiler errors)" "$LOG_FILE" | head -20 | sed 's/^/    /'
  echo ""
  echo -e "  Full log: ${CYAN}$LOG_FILE${RESET}"
  exit 1
elif grep -q "Compilation failed" "$LOG_FILE" 2>/dev/null; then
  fail "Compilation failed — check the log:"
  echo -e "  ${CYAN}$LOG_FILE${RESET}"
  exit 1
else
  ok "Compilation successful (${UNITY_DURATION}s)"
fi

# Check for any warnings worth surfacing
WARN_COUNT=$(grep -c "warning CS" "$LOG_FILE" 2>/dev/null || true)
if [ "$WARN_COUNT" -gt 0 ]; then
  warn "$WARN_COUNT compiler warning(s) — see $LATEST_LOG for details"
fi

# ── 6. Done ──────────────────────────────────────────────────
echo ""
hr
echo -e "${GREEN}${BOLD}  ✅  All done! Open Unity and hit Play.${RESET}"
hr
echo ""
