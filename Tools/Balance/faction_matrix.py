#!/usr/bin/env python3
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
"""Random-faction skirmish matrix with Russia in every match.

Each match draws its non-Russian seats at random from the twelve lobby templates
(America, China, GLA and their nine generals) and puts Russia on a random seat.
1v1 is Russia against one draw; 2v2 is Russia and one ally against two enemies,
teams fixed by seat (0-1 against 2-3).

    python faction_matrix.py --players 2 --matches 24 --cells 96 128
    python faction_matrix.py --players 4 --matches 24 --cells 128 160

The per-match rows go to a CSV; the summary is printed.  A frame-limited match
counts as a draw for every seat.  Decided matches are read from the game's
HEADLESS PLAYER lines: a seat on the winning team is a win, the other team's
seats are losses.

The draw is reproducible from --rng-seed and the choices are written to the CSV
next to the results.
"""

import argparse
import csv
import os
import random
import re
import subprocess
import sys
import tempfile

RUSSIA = "FactionRussia"

OPPONENTS = [
    "FactionAmerica", "FactionChina", "FactionGLA",
    "FactionAmericaAirForceGeneral", "FactionAmericaLaserGeneral", "FactionAmericaSuperWeaponGeneral",
    "FactionChinaTankGeneral", "FactionChinaInfantryGeneral", "FactionChinaNukeGeneral",
    "FactionGLAToxinGeneral", "FactionGLADemolitionGeneral", "FactionGLAStealthGeneral",
]

PLAYER_RE = re.compile(
    r"HEADLESS PLAYER (\d+) '([^']*)': (\w+).*?slot (\d+) team (-?\d+)")
RESULT_RE = re.compile(
    r"HEADLESS RESULT: (decided|frame limit reached|player left|idle) on frame (\d+)")


def parse_log(path):
    """Return (why, frames, {slot: (status, name)})."""
    why, frames, players = "no result", 0, {}
    try:
        text = open(path, encoding="latin-1", errors="replace").read()
    except OSError:
        return "no log", 0, players
    m = RESULT_RE.search(text)
    if m:
        why, frames = m.group(1), int(m.group(2))
    for m in PLAYER_RE.finditer(text):
        name, status, slot, team = m.group(2), m.group(3), int(m.group(4)), int(m.group(5))
        players[slot] = (status, name, team)
    return why, frames, players


def winner_team(why, players):
    """Return the winning team number, or None for a draw."""
    if why != "decided":
        return None
    winners = [team for status, _, team in players.values() if status == "WON"]
    return winners[0] if winners else None


def winner_slot(why, players):
    """Return the winning seat for a duel, or None for a draw."""
    if why != "decided":
        return None
    winners = [slot for slot, (status, _, _) in players.items() if status == "WON"]
    return winners[0] if winners else None


def run_match(args, i, rng, deck):
    players = args.players
    russia_slot = rng.randrange(players)
    sides = {russia_slot: RUSSIA}
    teams = {}

    if players == 2:
        other = 1 - russia_slot
        sides[other] = deck.pop()
    else:
        # teams by seat: 0-1 against 2-3
        for slot in range(players):
            teams[slot] = 0 if slot < 2 else 1
        russia_team = teams[russia_slot]
        mates = [s for s in range(players) if teams[s] == russia_team and s != russia_slot]
        foes = [s for s in range(players) if teams[s] != russia_team]
        draws = rng.sample(OPPONENTS, 3)
        sides[mates[0]] = draws[0]
        sides[foes[0]] = draws[1]
        sides[foes[1]] = draws[2]

    cells = args.cells[i % len(args.cells)]
    seed = args.seed_start + i
    prefix = f"{args.prefix}_{i:03d}"
    log = os.path.join(os.path.dirname(os.path.abspath(args.exe)), prefix + "DebugLogFile.txt")
    try:
        os.unlink(log)
    except OSError:
        pass

    cmd = [
        args.exe, "-headless", "-quickstart", "-noshellmap", "-observer",
        "-multiInstance", "-noFPSLimit",
        "-overlay", args.overlay, "-root", args.root,
        "-maxframes", str(args.maxframes),
        "-randommap", str(seed), str(players), str(cells),
        "-autoskirmish", str(players), "-aidiff", "brutal",
        "-seed", str(seed), "-logPrefix", prefix,
    ]
    for slot in range(players):
        cmd += ["-side", str(slot), sides[slot]]
    for slot, team in teams.items():
        cmd += ["-team", str(slot), str(team)]

    env = dict(os.environ)
    env["ZH_UNATTENDED"] = "1"
    env["ZH_USER_DATA_DIR"] = tempfile.mkdtemp(prefix="matrix-userdata-")
    try:
        proc = subprocess.run(cmd, cwd=args.root, env=env, capture_output=True,
                              text=True, timeout=args.timeout)
    except subprocess.TimeoutExpired:
        return why_rows(i, cells, seed, sides, teams, "timeout", 0, {}, args)
    why, frames, parsed = parse_log(log)
    if proc.returncode != 0 and why == "no result":
        why = f"exit {proc.returncode}"
    return why_rows(i, cells, seed, sides, teams, why, frames, parsed, args)


def why_rows(i, cells, seed, sides, teams, why, frames, parsed, args):
    won_slot = winner_slot(why, parsed)
    won_team = winner_team(why, parsed)
    rows = []
    for slot, faction in sides.items():
        status, name, team = parsed.get(slot, ("?", "?", teams.get(slot, -1)))
        if why != "decided":
            result = "draw"
        elif args.players == 2:
            result = "win" if won_slot == slot else "loss"
        elif won_team is not None and team == won_team:
            result = "win"
        else:
            result = "loss"
        rows.append({
            "match": i, "seed": seed, "cells": cells, "players": args.players,
            "slot": slot, "faction": faction, "team": team,
            "status": status, "result": result, "frames": frames, "why": why,
        })
    return rows


def summarize(rows, players):
    per = {}
    for row in rows:
        s = per.setdefault(row["faction"], {"played": 0, "wins": 0, "losses": 0, "draws": 0})
        s["played"] += 1
        s["wins" if row["result"] == "win" else "losses" if row["result"] == "loss" else "draws"] += 1
    print(f"\n{players // 2}v{players // 2} summary ({len(rows)} seat-results)")
    print(f"{'faction':34} {'played':>7} {'decided':>8} {'wins':>6} {'losses':>7} {'draws':>6} {'win%':>7}")
    for faction in sorted(per, key=lambda f: (-(per[f]["wins"] / max(1, per[f]["wins"] + per[f]["losses"])),
                                              f)):
        s = per[faction]
        decided = s["wins"] + s["losses"]
        rate = 100.0 * s["wins"] / decided if decided else 0.0
        print(f"{faction:34} {s['played']:7} {decided:8} {s['wins']:6} {s['losses']:7} "
              f"{s['draws']:6} {rate:6.1f}%")
    return per


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--exe", default="build-mac/generals")
    ap.add_argument("--overlay", default="build-mac/overlay")
    ap.add_argument("--data", default=os.path.expanduser("~/zh-data"))
    ap.add_argument("--root", default=None)
    ap.add_argument("--players", type=int, choices=(2, 4), default=2)
    ap.add_argument("--matches", type=int, default=24)
    ap.add_argument("--cells", type=int, nargs="+", default=[96, 128])
    ap.add_argument("--maxframes", type=int, default=60000)
    ap.add_argument("--seed-start", type=int, default=0)
    ap.add_argument("--rng-seed", type=int, default=1)
    ap.add_argument("--prefix", default="matrix")
    ap.add_argument("--out", default="build-mac/faction_matrix.csv")
    ap.add_argument("--timeout", type=int, default=240)
    args = ap.parse_args()

    args.exe = os.path.abspath(args.exe)
    args.overlay = os.path.abspath(args.overlay)
    args.root = os.path.abspath(args.root or os.path.join(args.data, "zerohour"))
    if not os.path.exists(args.exe):
        sys.exit(f"no executable at {args.exe}")

    rng = random.Random(args.rng_seed)
    # 1v1: a shuffled deck of the twelve opponents, so each appears before any repeats.
    deck = OPPONENTS * ((args.matches + len(OPPONENTS) - 1) // len(OPPONENTS))
    rng.shuffle(deck)

    rows = []
    for i in range(args.matches):
        rows += run_match(args, i, rng, deck)
        print(f"[{i + 1}/{args.matches}] done", flush=True)

    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    with open(args.out, "w", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)
    summarize(rows, args.players)
    print(f"\nrows: {args.out}")


if __name__ == "__main__":
    main()
