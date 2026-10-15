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
#
# ai-batch.sh on itself, with no game data and no game binary:
#   1. --parse-only on a fixture prints the win-rate table a measurement is read from: the decided and
#      frame-limited counts, the average length, both seats' win rates, the per-match winner and frame
#      (with the CRC), and the pathfinder totals.  The fixture is a hand-written pair of logs in the
#      engine's own HEADLESS format, so the seam is checked here rather than against a live match.
#   2. --dryrun prints the commands a batch would issue, and needs no data.
#   3. A real run with no game data skips in the house style and exits 77, which ctest reports as
#      Skipped rather than failed.
#
# Usage: run_ai_batch.sh <ai-batch.sh>
set -u
BATCH="$1"
TESTS="$(cd "$(dirname "$0")" && pwd)"
NOWHERE=/nonexistent/ai-batch-test-data
failed=0
check() { if eval "$1"; then echo "ok: $2"; else echo "FAIL: $2"; failed=1; fi; }

check "bash -n \"$BATCH\"" "ai-batch.sh parses (bash -n)"

# ---- 1. the parser, on the fixture --------------------------------------------------------------------
out="$(bash "$BATCH" --parse-only "$TESTS/ai_batch_sample_won.txt" "$TESTS/ai_batch_sample_limited.txt" 2>&1)"
status=$?
printf '%s\n' "$out" | sed 's/^/  /'
check '[ "$status" -eq 0 ]' "the parser reads the fixture without game data (exit $status)"
check 'printf "%s" "$out" | grep -q "^decided 1/2   frame-limited 1   failed 0$"' \
	"one decided and one frame-limited match are counted"
check 'printf "%s" "$out" | grep -q "^average length 8123 frames (2.3 min of game time)$"' \
	"the average length is over the decided match"
check 'printf "%s" "$out" | grep -q "^\[  1/2\] ai_batch_sample_won.txt ... decided, player 2, frame 8123, 0x86988377, 12.4s$"' \
	"the decided match reports its winner, frame and CRC"
check 'printf "%s" "$out" | grep -q "^\[  2/2\] ai_batch_sample_limited.txt ... frame limit reached, no winner, frame 60000, 0x1234ABCD, 91.2s$"' \
	"a match nobody won is reported without one"
check 'printf "%s" "$out" | grep -qE "^2 +Hard AI +1 +50\.0 +19\.0 +5\.0 +4\.0 +4650$"' \
	"seat 2 won one of the two, and its columns are the real ones"
check 'printf "%s" "$out" | grep -qE "^3 +Hard AI +0 +0\.0 +13\.5 +5\.5 +10\.5 +4300$"' \
	"seat 3 won none"
check 'printf "%s" "$out" | grep -q "pathfind per match: 7165 searches / 201ms, 50707 cell expansions | nopath 2 outofcells 0 | blocked unit-frames 69 (stuck 2)"' \
	"the pathfinder totals are the sum over both logs"
check 'printf "%s" "$out" | grep -q "^blocked unit-frames per 1000 logic frames: 2.0$"' \
	"and the blocked rate is per 1000 logic frames"

# a log that is not there is an error, not a silent zero
out="$(bash "$BATCH" --parse-only "$TESTS/no_such.log" 2>&1)"
status=$?
check '[ "$status" -eq 2 ]' "a missing log is a setup error (exit $status)"

# ---- 2. --dryrun, which needs no data -----------------------------------------------------------------
out="$(bash "$BATCH" --runs 2 --dryrun 2>&1)"
status=$?
printf '%s\n' "$out" | sed 's/^/  /'
check '[ "$status" -eq 0 ]' "--dryrun runs with no game data (exit $status)"
check 'printf "%s" "$out" | grep -q -- "-headless -quickstart -noshellmap -observer -multiInstance -noFPSLimit"' \
	"the headless switches are the ones the engine parses"
check 'printf "%s" "$out" | grep -q -- "-randommap 0 2 96"' "the first run is seed 0 on a 96-cell map"
check 'printf "%s" "$out" | grep -q -- "-randommap 1 2 128"' "the second is seed 1 on a 128-cell map"
check 'printf "%s" "$out" | grep -q -- "-autoskirmish 2 -aidiff brutal -seed 0 -logPrefix ai_000"' \
	"and the seat count, rung, seed and log prefix"
check 'printf "%s" "$out" | grep -q -- "-logPrefix ai_001"' "the second run's log has its own name"

# ---- 3. no game data skips cleanly --------------------------------------------------------------------
out="$(bash "$BATCH" --runs 1 --data "$NOWHERE" 2>&1)"
status=$?
printf '%s\n' "$out" | sed 's/^/  /'
check '[ "$status" -eq 77 ]' "no game data exits 77 (exit $status)"
check 'printf "%s" "$out" | grep -q "^skip: no game data (--data or ZH_DATA_DIR, a folder holding zerohour/)$"' \
	"and says so in the house style"

exit $failed
