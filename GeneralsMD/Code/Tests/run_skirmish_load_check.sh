#!/usr/bin/env bash
#	Copyright 2026 KuiseRapper for Generals Vanguard.
#	Additional terms under GNU GPL section 7 apply: see LICENSE.md.
#
#	This program is free software: you can redistribute it and/or modify
#	it under the terms of the GNU General Public License as published by
#	the Free Software Foundation, either version 3 of the License, or
#	(at your option) any later version.
#
#	This program is distributed in the hope that it will be useful,
#	but WITHOUT ANY WARRANTY; without even the implied warranty of
#	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#	GNU General Public License for more details.
#
#	You should have received a copy of the GNU General Public License
#	along with this program.  If not, see <http://www.gnu.org/licenses/>.
#
# The skirmish load screen's faction plate.  PlayerTemplate.ini has carried LoadScreenImage since EA
# wrote it, but the only two places that would have read it were comments, so the five Vanguard plates
# the fork maps over SAFactionLogoPage_* / ReforgedLoadRussia were data nothing drew.  A headless
# skirmish still builds the load screen and its log names the plate, which makes the wiring checkable
# without a window: one short run on the stock USA template (no mod needed) must log
#
#   LoadScreen: the skirmish plate is SAFactionLogoPage_US (FactionAmerica)
#
# and must not log "no image named".  The screenshot that proves how it looks is a separate check
# (ZH_LOADSCREEN_SHOT=1, the screenshot in build-mac/tmp_preview): this one guards the wiring from being commented out
# again.
#
# Needs ZH_DATA_DIR (a folder holding zerohour/); without it, exit 77, which ctest reports as Skipped.
# The -root/-overlay switches are POSIX's, so the run skips on Windows rather than touch a real install.
# Usage: run_skirmish_load_check.sh <generals>
set -u
if [ -z "${ZH_DATA_DIR:-}" ] || [ ! -d "$ZH_DATA_DIR/zerohour" ]; then
	echo "skip: no game data (ZH_DATA_DIR, a folder holding zerohour/)"
	exit 77
fi
case "$(uname -s)" in
	MINGW*|MSYS*|CYGWIN*)
		echo "skip: -root and -overlay are POSIX switches; a Windows run would use the real install"
		exit 77
		;;
esac

GENERALS="$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
EXEDIR="$(dirname "$GENERALS")"
ROOT="$(cd "$ZH_DATA_DIR/zerohour" && pwd)"
WORK="$(mktemp -d "${TMPDIR:-/tmp}/skirmish-load-check.XXXXXX")" || WORK=""
if [ -z "$WORK" ] || [ ! -d "$WORK" ]; then
	echo "run_skirmish_load_check: cannot make a work folder under ${TMPDIR:-/tmp}" >&2
	exit 2
fi
trap 'rm -rf "$WORK"; rm -f "$EXEDIR/loadcheckDebugLogFile.txt"' EXIT
export HOME="$WORK/home"
mkdir -p "$HOME"
export ZH_USER_DATA_DIR="$WORK/userdata"
mkdir -p "$ZH_USER_DATA_DIR"

# A raw POSIX build writes its debug log beside the executable; an app bundle writes it in the user
# data folder.  ctest runs the raw one, whose directory is the build tree and writable.
LOG="$EXEDIR/loadcheckDebugLogFile.txt"
rm -f "$LOG"
( cd "$WORK" && "$GENERALS" -root "$ROOT" -overlay "$EXEDIR/overlay" \
	-headless -quickstart -noshellmap -map "Maps\Alpine Assault\Alpine Assault.map" \
	-autoskirmish 2 -side 0 FactionAmerica -side 1 FactionChina -seed 1 -maxframes 10 \
	-noaudio -logprefix loadcheck > "$WORK/run.log" 2>&1 )
status=$?
failed=0
check() { if eval "$1"; then echo "ok: $2"; else echo "FAIL: $2"; failed=1; fi; }

check '[ $status -eq 0 ]' "a headless skirmish ran (exit $status)"
check '[ -f "$LOG" ]' "its log is where the run says it is"
check 'grep -q "LoadScreen: the skirmish plate is SAFactionLogoPage_US" "$LOG"' \
	"the load screen read PlayerTemplate.LoadScreenImage and found the USA plate"
check '! grep -q "LoadScreen: no image named" "$LOG"' \
	"the plate resolved instead of falling back to the layout's background"

exit $failed
