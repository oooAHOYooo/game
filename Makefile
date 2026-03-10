# =============================================================
#  Makefile — Unity project shortcuts
#  Run any of these from the project root directory.
#
#  make sync     — pull latest + Unity batch compile (main cmd)
#  make pull     — git pull only (no Unity compile)
#  make push     — git add -A, commit, and push
#  make status   — quick git status
#  make log      — last 10 commits
#  make open     — open Unity with this project
# =============================================================

UNITY_EXE  := /home/alexg/Unity/Hub/Editor/6000.3.9f1/Editor/Unity
PROJECT    := $(shell pwd)

.PHONY: sync pull push status log open

# ── Default: show available commands ────────────────────────
help:
	@echo ""
	@echo "  🎮  Unity Project — available commands"
	@echo ""
	@echo "  make sync     pull latest code + Unity batch compile"
	@echo "  make pull     git pull only"
	@echo "  make push     stage all, commit (prompts for message), push"
	@echo "  make status   git status"
	@echo "  make log      last 10 commits"
	@echo "  make open     open Unity editor with this project"
	@echo ""

# ── Main: sync + compile ────────────────────────────────────
sync:
	@bash $(PROJECT)/sync.sh

# ── Git pull only ────────────────────────────────────────────
pull:
	@echo "Pulling latest..."
	@git pull --rebase --autostash

# ── Git push (interactive commit message) ───────────────────
push:
	@git status --short
	@echo ""
	@read -rp "Commit message: " msg; \
	  git add -A && \
	  git commit -m "$$msg" && \
	  git push origin main && \
	  echo "✔  Pushed!"

# ── Status ──────────────────────────────────────────────────
status:
	@git status

# ── Log ─────────────────────────────────────────────────────
log:
	@git log --oneline -10

# ── Open Unity ──────────────────────────────────────────────
open:
	@echo "Opening Unity..."
	@nohup "$(UNITY_EXE)" -projectPath "$(PROJECT)" &>/dev/null &
	@echo "✔  Unity is launching in the background"

# ── Build Switch ────────────────────────────────────────────
switch:
	powershell.exe -ExecutionPolicy Bypass -File build_switch.ps1

# ── Build Linux ARM (RPi) ───────────────────────────────────
linux-arm:
	powershell.exe -ExecutionPolicy Bypass -File build_linux_arm.ps1
