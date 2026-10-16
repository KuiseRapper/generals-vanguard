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
# ai-batch.sh: ai-batch.ps1's POSIX twin.  The rule in ROADMAP.md and AGENTS.md: no AI change can be
# claimed without a number behind it, and one match proves nothing - the same seed on the same map is
# repeatable, but a different seed is a different game.  This plays the same configuration over a
# spread of seeds and map sizes, one headless process at a time, bounded by --maxframes so a match the
# AI cannot close out still ends, and names each run's log with -logPrefix so runs never clobber each
# other's answers.  It then prints the win rate per seat and the match length, so a change can be
# reported as "40% -> 65%" instead of "it felt better".
#
# The map comes from -randommap unless --map names a shipped one, so the seed is the map and nothing
# has to be installed to compare two builds.  That is the pair of switches an AI change is measured
# with: --exe <new build> against --exe <old build>, same seeds both ways.
#
# It reads the game's own log lines, the same ones ai-batch.ps1 reads, and --parse-only prints the
# same table from logs already written (no game data, no game): the seam a fixture can verify.
#   HEADLESS RESULT: <why> on frame <n> (<n> frames in <s>s wall, ...)
#   HEADLESS CRC:    0x<hex> at frame <n>
#   HEADLESS PLAYER <n> '<name>': <status> | score <n> | money <n> earned <n> spent | units <n> built
#                                    <n> lost <n> killed peak <n> | buildings <n> built <n> lost | slot/team
#   HEADLESS PATHFIND: <slot> <n>x/<ms> ... | nopath <n> outofcells <n> blocked <n> stuck <n> ...
#
# WHAT THIS IS NOT.  Unlike the other POSIX harnesses this one is not a ctest that proves the engine
# works: it roots the game directly at --data's zerohour (rule 9's farm is not built) and it does not
# stage the fork's overlay, so pass --overlay, or let the default find <exe dir>/overlay in a build
# tree, or the fork's data is simply missing.  It also runs one match at a time: ai-batch.ps1's
# -Parallel is not reproduced, so a batch takes as long as its matches and the wall times do not
# depend on what else is running.
#
# A run that never reaches its match is killed after --timeout minutes rather than waited on forever.
#
# Exit status: 0 the batch ran (a match that produced no result is reported as failed, not returned),
# 2 a usage or setup error, 77 no game data, which ctest and callers read as skipped.

set -u

REPO="$(cd "$(dirname "$0")" && pwd)"
LOGIC_FPS=60		# LOGICFRAMES_PER_SECOND: every frame number the log writes counts these
TMP="${TMPDIR:-/tmp}"; TMP="${TMP%/}"

RUNS=20
PLAYERS=2
DIFFICULTY=brutal
MAXFRAMES=60000		# 60 a second: 16.7 minutes of game time
MAP=""
MAPCELLS="96 128 160"
SEEDSTART=0
LOGPREFIX=ai
EXE=""
DATA="${ZH_DATA_DIR:-}"
OVERLAY=""
TIMEOUT=20
EXTRA=""
DRYRUN=0
PARSEONLY=0
LOGFILES=()

usage() {
	cat <<'EOF'
ai-batch.sh: play a batch of headless skirmishes and print the win rate per seat (ai-batch.ps1's POSIX twin).

Usage: ai-batch.sh [options]
  --runs <n>             matches to play (default 20)
  --players <n>          players a match, every side AI under -autoskirmish (default 2)
  --difficulty <name>    easy|medium|brutal, what the game reads -aidiff as (default brutal)
  --maxframes <n>        logic frames a match may run; 60 a second (default 60000, 16.7 min)
  --map <name>           a shipped map, e.g. "Twilight Flame", instead of a generated one
  --mapcells <list>      generated map sizes to spread over (default "96 128 160")
  --seedstart <n>        first seed of the batch; the seed is the match (default 0)
  --logprefix <tag>      names this batch's logs, <tag>_<seed> (default ai)
  --exe <path>           the executable to run (default <repo>/build-mac/generals)
  --data <dir>           a folder holding zerohour/ (default $ZH_DATA_DIR)
  --overlay <dir>        the fork's data, passed as -overlay (default <exe dir>/overlay if it exists)
  --timeout <min>        kill a run wedged past this (default 20)
  --extra "<args>"       extra engine switches, appended verbatim to every run's command line
  --dryrun               print the commands a run would issue, run nothing, need no game data
  --parse-only <log>...  print the table from logs already written, need no game data
  --help

With the flag absent nothing changes for the game: this is a driver, not an engine switch.

Exit status: 0 the batch ran, 2 a usage or setup error, 77 no game data.
EOF
}

die() { echo "ai-batch: $*" >&2; exit 2; }
is_num() { case "$1" in ''|*[!0-9]*) return 1;; *) return 0;; esac; }
map_label() { if [ -n "$MAP" ]; then printf 'on %s' "$MAP"; else printf 'generated maps'; fi; }

while [ $# -gt 0 ]; do
	arg="$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')"
	case "$arg" in
		-runs|--runs) [ $# -ge 2 ] || die "$1 needs a value"; RUNS="$2"; shift 2;;
		-players|--players) [ $# -ge 2 ] || die "$1 needs a value"; PLAYERS="$2"; shift 2;;
		-difficulty|--difficulty) [ $# -ge 2 ] || die "$1 needs a value"; DIFFICULTY="$2"; shift 2;;
		-maxframes|--maxframes) [ $# -ge 2 ] || die "$1 needs a value"; MAXFRAMES="$2"; shift 2;;
		-map|--map) [ $# -ge 2 ] || die "$1 needs a value"; MAP="$2"; shift 2;;
		-mapcells|--mapcells) [ $# -ge 2 ] || die "$1 needs a value"; MAPCELLS="$2"; shift 2;;
		-seedstart|--seedstart) [ $# -ge 2 ] || die "$1 needs a value"; SEEDSTART="$2"; shift 2;;
		-logprefix|--logprefix) [ $# -ge 2 ] || die "$1 needs a value"; LOGPREFIX="$2"; shift 2;;
		-exe|--exe) [ $# -ge 2 ] || die "$1 needs a value"; EXE="$2"; shift 2;;
		-data|--data) [ $# -ge 2 ] || die "$1 needs a value"; DATA="$2"; shift 2;;
		-overlay|--overlay) [ $# -ge 2 ] || die "$1 needs a value"; OVERLAY="$2"; shift 2;;
		-timeout|--timeout) [ $# -ge 2 ] || die "$1 needs a value"; TIMEOUT="$2"; shift 2;;
		-extra|--extra) [ $# -ge 2 ] || die "$1 needs a value"; EXTRA="$2"; shift 2;;
		-dryrun|--dryrun) DRYRUN=1; shift;;
		-parse-only|--parse-only) PARSEONLY=1; shift; LOGFILES=("$@"); break;;
		-help|--help|-h) usage; exit 0;;
		*) die "unknown argument $1 (try --help)";;
	esac
done

is_num "$RUNS" || die "--runs wants a number, not '$RUNS'"
is_num "$PLAYERS" || die "--players wants a number, not '$PLAYERS'"
is_num "$MAXFRAMES" || die "--maxframes wants a number, not '$MAXFRAMES'"
is_num "$SEEDSTART" || die "--seedstart wants a number, not '$SEEDSTART'"
is_num "$TIMEOUT" || die "--timeout wants a number, not '$TIMEOUT'"
[ "$RUNS" -ge 1 ] || die "--runs must be at least 1"
[ "$PLAYERS" -ge 2 ] || PLAYERS=2		# the engine clamps a smaller -autoskirmish the same way
[ "$TIMEOUT" -ge 1 ] || TIMEOUT=1

# ---------------------------------------------------------------------------------------------------
# The log parser.  parse_log fills the P_* globals from one run's log; note_match/note_plain fold a
# run into the totals; print_summary prints the table both modes share.  Nothing here reads the game,
# which is what lets --parse-only be checked against a fixture with no data.
# ---------------------------------------------------------------------------------------------------

reset_parse() {
	P_WHY=""; P_FRAMES=0; P_WALL=""; P_CRC=""; P_CRC_FRAME=""; P_IDS=""; P_PF_HAS=0
	P_PF_FINDS=0; P_PF_MS=0; P_PF_EXPANDS=0; P_PF_NOPATH=0; P_PF_OUT=0; P_PF_BLOCKED=0; P_PF_STUCK=0
	unset P_NAME P_STATUS P_SCORE P_EARNED P_SPENT P_BUILT P_LOST P_KILLED P_PEAK P_BBUILT P_BLOST 2>/dev/null
}

parse_log() {
	local file="$1" line body fields id rest
	reset_parse
	if [ ! -f "$file" ]; then
		P_WHY="no log"
		return 1
	fi

	line="$(grep -a 'HEADLESS RESULT: ' "$file" 2>/dev/null | tail -1 | tr -d '\r')"
	if [ -n "$line" ]; then
		P_WHY="$(printf '%s\n' "$line" | sed -n 's/.*HEADLESS RESULT: \(.*\) on frame [0-9][0-9]* .*/\1/p')"
		P_FRAMES="$(printf '%s\n' "$line" | sed -n 's/.* on frame \([0-9][0-9]*\) .*/\1/p')"
		P_WALL="$(printf '%s\n' "$line" | sed -n 's/.*in \([0-9][0-9.]*\)s wall.*/\1/p')"
	fi
	[ -n "$P_WHY" ] || P_WHY="no result"
	[ -n "$P_FRAMES" ] || P_FRAMES=0

	line="$(grep -a 'HEADLESS CRC: 0x' "$file" 2>/dev/null | tail -1 | tr -d '\r')"
	if [ -n "$line" ]; then
		P_CRC="$(printf '%s\n' "$line" | sed -n 's/.*HEADLESS CRC: \(0x[0-9A-Fa-f]*\).*/\1/p')"
		P_CRC_FRAME="$(printf '%s\n' "$line" | sed -n 's/.* at frame \([0-9][0-9]*\).*/\1/p')"
	fi

	line="$(grep -a 'HEADLESS PATHFIND: ' "$file" 2>/dev/null | tail -1 | tr -d '\r')"
	if [ -n "$line" ]; then
		body="${line#*HEADLESS PATHFIND: }"
		P_PF_HAS=1
		P_PF_MS="$(printf '%s\n' "$body" | grep -o -E '[0-9][0-9]*x/[0-9.]+' | sed 's|.*/||' | awk '{s += $1} END {printf "%.0f", s + 0}')"
		P_PF_EXPANDS="$(printf '%s\n' "$body" | sed -n 's/.* expand \([0-9][0-9]*\)x.*/\1/p')"
		P_PF_NOPATH="$(printf '%s\n' "$body" | sed -n 's/.* nopath \([0-9][0-9]*\).*/\1/p')"
		P_PF_OUT="$(printf '%s\n' "$body" | sed -n 's/.* outofcells \([0-9][0-9]*\).*/\1/p')"
		P_PF_BLOCKED="$(printf '%s\n' "$body" | sed -n 's/.* blocked \([0-9][0-9]*\).*/\1/p')"
		P_PF_STUCK="$(printf '%s\n' "$body" | sed -n 's/.* stuck \([0-9][0-9]*\).*/\1/p')"
		local slot n
		for slot in find closest patch attack; do
			n="$(printf '%s\n' "$body" | grep -o -E "(^| )$slot [0-9][0-9]*x" | grep -o -E '[0-9][0-9]*' | awk '{s += $1} END {printf "%.0f", s + 0}')"
			P_PF_FINDS=$(( P_PF_FINDS + ${n:-0} ))
		done
		[ -n "$P_PF_EXPANDS" ] || P_PF_EXPANDS=0
		[ -n "$P_PF_NOPATH" ] || P_PF_NOPATH=0
		[ -n "$P_PF_OUT" ] || P_PF_OUT=0
		[ -n "$P_PF_BLOCKED" ] || P_PF_BLOCKED=0
		[ -n "$P_PF_STUCK" ] || P_PF_STUCK=0
	fi

	# one line per playable player; a heredoc, not a pipe, so the arrays set here survive
	while IFS= read -r line; do
		line="${line%$'\r'}"
		id="$(printf '%s\n' "$line" | sed -n 's/^HEADLESS PLAYER \([0-9][0-9]*\) .*/\1/p')"
		[ -n "$id" ] || continue
		P_NAME[$id]="$(printf '%s\n' "$line" | sed -n "s/^HEADLESS PLAYER [0-9][0-9]* '\(.*\)': .*/\1/p")"
		rest="$(printf '%s\n' "$line" | sed -n "s/^HEADLESS PLAYER [0-9][0-9]* '.*': //p")"
		P_STATUS[$id]="${rest%% | *}"
		P_SCORE[$id]="$(printf '%s\n' "$rest" | sed -n 's/.* | score \(-\{0,1\}[0-9][0-9]*\) |.*/\1/p')"
		fields="$(printf '%s\n' "$rest" | sed -n 's/.* money \([0-9][0-9]*\) earned \([0-9][0-9]*\) spent .*/\1 \2/p')"
		# the words trail their numbers: "money <earned> earned <spent> spent | units <built> built ..."
		set -- $fields
		P_EARNED[$id]="${1:-0}"; P_SPENT[$id]="${2:-0}"
		fields="$(printf '%s\n' "$rest" | sed -n 's/.* units \([0-9][0-9]*\) built \([0-9][0-9]*\) lost \([0-9][0-9]*\) killed peak \([0-9][0-9]*\).*/\1 \2 \3 \4/p')"
		set -- $fields
		P_BUILT[$id]="${1:-0}"; P_LOST[$id]="${2:-0}"; P_KILLED[$id]="${3:-0}"; P_PEAK[$id]="${4:-0}"
		fields="$(printf '%s\n' "$rest" | sed -n 's/.* buildings \([0-9][0-9]*\) built \([0-9][0-9]*\) lost.*/\1 \2/p')"
		set -- $fields
		P_BBUILT[$id]="${1:-0}"; P_BLOST[$id]="${2:-0}"
		P_IDS="$P_IDS $id"
	done <<EOF
$(grep -a 'HEADLESS PLAYER ' "$file" 2>/dev/null)
EOF
	return 0
}

first_winner() {
	local id
	for id in $(printf '%s\n' $P_IDS | tr ' ' '\n' | LC_ALL=C sort -n -u); do
		[ -n "$id" ] || continue
		if [ "${P_STATUS[$id]}" = "WON" ]; then printf 'player %s' "$id"; return 0; fi
	done
	printf 'no winner'
}

note_match() {
	local label="$1" winner id frames="${P_FRAMES:-0}" wall="${P_WALL:-$M_WALL}"
	winner="$(first_winner)"
	R_TOTAL=$(( R_TOTAL + 1 ))
	case "$P_WHY" in
		decided) R_DECIDED=$(( R_DECIDED + 1 )); D_FRAMES=$(( D_FRAMES + frames ));;
		"frame limit reached") R_LIMITED=$(( R_LIMITED + 1 ));;
		*) R_FAILED=$(( R_FAILED + 1 ));;
	esac
	printf '[%3d/%d] %s ... %s, %s, frame %s, %s, %ss\n' "$R_TOTAL" "$TOTAL_ROWS" "$label" "$P_WHY" "$winner" "$frames" "${P_CRC:--}" "$wall"
	for id in $(printf '%s\n' $P_IDS | tr ' ' '\n' | LC_ALL=C sort -n -u); do
		[ -n "$id" ] || continue
		A_COUNT[$id]=$(( ${A_COUNT[$id]:-0} + 1 ))
		A_NAME[$id]="${P_NAME[$id]:-}"
		if [ "${P_STATUS[$id]}" = "WON" ]; then A_WIN[$id]=$(( ${A_WIN[$id]:-0} + 1 )); fi
		A_PEAK[$id]=$(( ${A_PEAK[$id]:-0} + ${P_PEAK[$id]:-0} ))
		A_KILLS[$id]=$(( ${A_KILLS[$id]:-0} + ${P_KILLED[$id]:-0} ))
		A_LOST[$id]=$(( ${A_LOST[$id]:-0} + ${P_LOST[$id]:-0} ))
		A_SPENT[$id]=$(( ${A_SPENT[$id]:-0} + ${P_SPENT[$id]:-0} ))
		A_IDS="$A_IDS $id"
	done
	T_FINDS=$(( T_FINDS + P_PF_FINDS )); T_MS=$(( T_MS + P_PF_MS )); T_EXPANDS=$(( T_EXPANDS + P_PF_EXPANDS ))
	T_NOPATH=$(( T_NOPATH + P_PF_NOPATH )); T_OUT=$(( T_OUT + P_PF_OUT ))
	T_BLOCKED=$(( T_BLOCKED + P_PF_BLOCKED )); T_STUCK=$(( T_STUCK + P_PF_STUCK ))
	T_FRAMES=$(( T_FRAMES + frames ))
	PF_ROWS=$(( PF_ROWS + P_PF_HAS ))
	if [ -n "$P_CRC" ]; then CRC_ROWS=$(( CRC_ROWS + 1 )); fi
	CSV="${CSV}${M_SEED},${M_CELLS},${P_WHY},${frames},${wall},${winner},${P_PF_FINDS},${P_PF_MS},${P_PF_EXPANDS},${P_PF_NOPATH},${P_PF_OUT},${P_PF_BLOCKED},${P_PF_STUCK}"$'\n'
}

note_plain() {
	local label="$1" why="$2"
	R_TOTAL=$(( R_TOTAL + 1 )); R_FAILED=$(( R_FAILED + 1 ))
	printf '[%3d/%d] %s ... %s\n' "$R_TOTAL" "$TOTAL_ROWS" "$label" "$why"
	CSV="${CSV}${M_SEED},${M_CELLS},${why},0,,no winner,0,0,0,0,0,0,0"$'\n'
}

print_summary() {
	local id
	echo ""
	if [ "$PARSEONLY" -eq 1 ]; then
		echo "=== parse-only : ${R_TOTAL} logs ==="
	else
		echo "=== ${LOGPREFIX} : ${R_TOTAL} matches, ${PLAYERS} players, ${DIFFICULTY}, $(map_label), ${EXE}, cap ${MAXFRAMES} frames ==="
	fi
	echo ""
	printf 'decided %d/%d   frame-limited %d   failed %d\n' "$R_DECIDED" "$R_TOTAL" "$R_LIMITED" "$R_FAILED"
	if [ "$R_DECIDED" -gt 0 ]; then
		awk -v f="$D_FRAMES" -v n="$R_DECIDED" -v fps="$LOGIC_FPS" \
			'BEGIN { printf "average length %.0f frames (%.1f min of game time)\n", f/n, f/n/fps/60 }'
	fi
	echo ""
	printf '%-6s %-20s %6s %7s %8s %8s %8s %9s\n' "Slot" "Name" "Wins" "Win%" "AvgPeak" "AvgKills" "AvgLost" "AvgSpent"
	printf '%-6s %-20s %6s %7s %8s %8s %8s %9s\n' "----" "----" "----" "----" "-------" "--------" "-------" "--------"
	for id in $(printf '%s\n' $A_IDS | tr ' ' '\n' | LC_ALL=C sort -n -u); do
		[ -n "$id" ] || continue
		awk -v slot="$id" -v name="${A_NAME[$id]:-}" -v w="${A_WIN[$id]:-0}" -v n="${A_COUNT[$id]:-0}" -v total="$R_TOTAL" \
			-v peak="${A_PEAK[$id]:-0}" -v kills="${A_KILLS[$id]:-0}" -v lost="${A_LOST[$id]:-0}" -v spent="${A_SPENT[$id]:-0}" \
			'BEGIN {
				if (n < 1) n = 1;
				printf "%-6s %-20s %6d %7.1f %8.1f %8.1f %8.1f %9.0f\n", slot, name, w, 100.0*w/total, peak/n, kills/n, lost/n, spent/n
			}'
	done
	if [ "$PF_ROWS" -gt 0 ]; then
		echo ""
		awk -v r="$PF_ROWS" -v finds="$T_FINDS" -v ms="$T_MS" -v expands="$T_EXPANDS" -v np="$T_NOPATH" \
			-v oc="$T_OUT" -v bl="$T_BLOCKED" -v st="$T_STUCK" \
			'BEGIN { printf "pathfind per match: %.0f searches / %.0fms, %.0f cell expansions | nopath %.0f outofcells %.0f | blocked unit-frames %.0f (stuck %.0f)\n", finds/r, ms/r, expands/r, np/r, oc/r, bl/r, st/r }'
		awk -v fr="$T_FRAMES" -v bl="$T_BLOCKED" 'BEGIN { if (fr > 0) printf "blocked unit-frames per 1000 logic frames: %.1f\n", 1000.0*bl/fr }'
	fi
}

write_csv() {
	local path="$1"
	{
		echo "Seed,Cells,Why,Frames,Wall,Winner,Searches,SearchMs,Expands,NoPath,OutOfCells,Blocked,Stuck"
		printf '%s' "$CSV"
	} > "$path" || return 1
	echo "per-match rows: $path"
}

# The totals both modes fold into.
R_TOTAL=0; R_DECIDED=0; R_LIMITED=0; R_FAILED=0; D_FRAMES=0
T_FINDS=0; T_MS=0; T_EXPANDS=0; T_NOPATH=0; T_OUT=0; T_BLOCKED=0; T_STUCK=0; T_FRAMES=0; PF_ROWS=0; CRC_ROWS=0
A_IDS=""
unset A_WIN A_COUNT A_NAME A_PEAK A_KILLS A_LOST A_SPENT 2>/dev/null
CSV=""
M_SEED=""; M_CELLS=""; M_WALL=0

# ---------------------------------------------------------------------------------------------------
# --parse-only: the table from logs already written, with no game data and no game.
# ---------------------------------------------------------------------------------------------------
if [ "$PARSEONLY" -eq 1 ]; then
	[ "${#LOGFILES[@]}" -gt 0 ] || die "--parse-only wants at least one log file"
	TOTAL_ROWS="${#LOGFILES[@]}"
	for f in "${LOGFILES[@]}"; do
		[ -f "$f" ] || die "no log at $f"
		M_SEED=""; M_CELLS=""
		if parse_log "$f"; then
			[ -n "$P_WALL" ] || P_WALL=0
			note_match "$(basename "$f")"
		else
			note_plain "$(basename "$f")" "$P_WHY"
		fi
	done
	print_summary
	exit 0
fi

# ---------------------------------------------------------------------------------------------------
# --dryrun: the commands a run would issue.  Needs no game data and no executable, so the shape of a
# batch can be checked wherever it is written.
# ---------------------------------------------------------------------------------------------------
build_args() {
	local prefix="$1"
	ARGS=(-headless -quickstart -noshellmap -observer -multiInstance -noFPSLimit)
	if [ -n "$OVERLAY" ]; then ARGS+=(-overlay "$OVERLAY"); fi
	ARGS+=(-root "$ROOT_ARG" -maxframes "$MAXFRAMES")
	if [ -n "$MAP" ]; then
		ARGS+=(-map "Maps\\$MAP\\$MAP.map")
	else
		ARGS+=(-randommap "$SEED_NOW" "$PLAYERS" "$CELLS_NOW")
	fi
	ARGS+=(-autoskirmish "$PLAYERS" -aidiff "$DIFFICULTY" -seed "$SEED_NOW" -logPrefix "$prefix")
	# --extra: one string of switches, split here so a caller can pass several at once.  Appended
	# last, so the game's own parser sees them after everything this driver set.
	if [ -n "$EXTRA" ]; then ARGS+=($EXTRA); fi
}

nth_cell() {		# nth_cell <index>: MAPCELLS cycled, as ai-batch.ps1 cycles $MapCells
	local n=0 cell count=0
	for cell in $MAPCELLS; do count=$(( count + 1 )); done
	[ "$count" -ge 1 ] || { printf '96'; return 0; }
	n=$(( $1 % count ))
	for cell in $MAPCELLS; do
		if [ "$n" -eq 0 ]; then printf '%s' "$cell"; return 0; fi
		n=$(( n - 1 ))
	done
}

if [ "$DRYRUN" -eq 1 ]; then
	if [ -z "$EXE" ]; then EXE="$REPO/build-mac/generals"; fi
	if [ -z "$DATA" ]; then
		ROOT_ARG="<data>/zerohour"		# a placeholder, not a path: no data was given
		ROOT_Q="$ROOT_ARG"
		echo "# no --data or ZH_DATA_DIR given: a real run would skip with 77" >&2
	else
		ROOT_ARG="$DATA/zerohour"
		ROOT_Q="$(printf '%q' "$ROOT_ARG")"
	fi
	if [ -z "$OVERLAY" ] && [ -d "$(dirname "$EXE")/overlay" ]; then OVERLAY="$(dirname "$EXE")/overlay"; fi
	USERDATA_ARG="${ZH_USER_DATA_DIR:-$TMP/ai-batch-userdata}"
	i=0
	while [ "$i" -lt "$RUNS" ]; do
		SEED_NOW=$(( SEEDSTART + i ))
		CELLS_NOW="$(nth_cell "$i")"
		prefix="${LOGPREFIX}_$(printf '%03d' "$SEED_NOW")"
		build_args "$prefix"
		cmd=""
		for a in "${ARGS[@]}"; do cmd="$cmd $(printf '%q' "$a")"; done
		printf '[%3d/%d] cd %s && ZH_UNATTENDED=1 ZH_USER_DATA_DIR=%q %q%s\n' \
			"$(( i + 1 ))" "$RUNS" "$ROOT_Q" "$USERDATA_ARG" "$EXE" "$cmd"
		i=$(( i + 1 ))
	done
	exit 0
fi

# ---------------------------------------------------------------------------------------------------
# The real batch.
# ---------------------------------------------------------------------------------------------------
if [ -z "$DATA" ] || [ ! -d "$DATA/zerohour" ]; then
	echo "skip: no game data (--data or ZH_DATA_DIR, a folder holding zerohour/)"
	exit 77
fi
[ -n "$EXE" ] || EXE="$REPO/build-mac/generals"
[ -x "$EXE" ] || die "no executable at $EXE (build first, or --exe)"
EXE="$(cd "$(dirname "$EXE")" && pwd)/$(basename "$EXE")"
LOGDIR="$(dirname "$EXE")"
ROOT_ARG="$(cd "$DATA/zerohour" && pwd)"
if [ -n "$OVERLAY" ]; then
	[ -d "$OVERLAY" ] || die "no overlay folder at $OVERLAY"
	OVERLAY="$(cd "$OVERLAY" && pwd)"
elif [ -d "$LOGDIR/overlay" ]; then
	OVERLAY="$LOGDIR/overlay"		# a build tree: the fork's data is staged beside the executable
fi
USERDATA="${ZH_USER_DATA_DIR:-$TMP/ai-batch-userdata}"
mkdir -p "$USERDATA" || die "cannot make the user data folder $USERDATA"
echo "ai-batch: $RUNS matches from seed $SEEDSTART, $PLAYERS players, $DIFFICULTY, $(map_label), $EXE"
echo "ai-batch: root $ROOT_ARG${OVERLAY:+, overlay $OVERLAY}, user data $USERDATA, logs in $LOGDIR"

TOTAL_ROWS="$RUNS"
i=0
while [ "$i" -lt "$RUNS" ]; do
	SEED_NOW=$(( SEEDSTART + i ))
	CELLS_NOW="$(nth_cell "$i")"
	prefix="${LOGPREFIX}_$(printf '%03d' "$SEED_NOW")"
	M_SEED="$SEED_NOW"; M_CELLS="$CELLS_NOW"
	build_args "$prefix"
	rm -f -- "$LOGDIR/${prefix}DebugLogFile.txt" "$LOGDIR/${prefix}.out" "$LOGDIR/${prefix}.err"
	start=$SECONDS
	( cd "$ROOT_ARG" && exec env ZH_UNATTENDED=1 ZH_USER_DATA_DIR="$USERDATA" "$EXE" "${ARGS[@]}" \
		>"$LOGDIR/${prefix}.out" 2>"$LOGDIR/${prefix}.err" ) &
	pid=$!
	deadline=$(( SECONDS + TIMEOUT * 60 ))
	killed=0
	while kill -0 "$pid" 2>/dev/null; do
		if [ "$SECONDS" -ge "$deadline" ]; then
			kill "$pid" 2>/dev/null
			killed=1
			break
		fi
		sleep 1
	done
	wait "$pid" 2>/dev/null
	status=$?
	M_WALL=$(( SECONDS - start ))
	log="$LOGDIR/${prefix}DebugLogFile.txt"
	if [ ! -f "$log" ]; then
		log="$USERDATA/Logs/${prefix}DebugLogFile.txt"		# an app bundle logs into the user data
	fi
	label="seed $SEED_NOW $CELLS_NOW cells"
	if [ "$killed" -eq 1 ]; then
		note_plain "$label" "wedged past $TIMEOUT min"
	elif [ ! -f "$log" ]; then
		note_plain "$label" "no log (exit $status)"
	elif parse_log "$log"; then
		[ -n "$P_WALL" ] || P_WALL="$M_WALL"
		note_match "$label"
	else
		note_plain "$label" "$P_WHY"
	fi
	i=$(( i + 1 ))
done

print_summary
echo ""
write_csv "$LOGDIR/${LOGPREFIX}-batch.csv" || echo "ai-batch: could not write the per-match rows" >&2
exit 0
