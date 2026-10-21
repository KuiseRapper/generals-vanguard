"""Give the fork's Russia faction its own skirmish AI script in SkirmishScripts.scb.

The engine attaches a skirmish script list to a map side by *name*: SidesList::prepareForMP_or_Skirmish
matches the side's playerName against the file's ScriptsPlayers entries.  A side with no entry of its
own (Russia) fell back on the script of the faction it is built on (China), so its AI built and fought
with Chinese objects.  This copies the China player's script list and its teams under SkirmishRussia,
owned by a Russia player, and rewrites every object type to Russia's roster.  Where Russia has no
counterpart for a China object the action is dropped rather than left pointing at China: a China
building in a Russia build order is the defect this exists to remove.

usage: russia.py <in.scb> <out.scb>
"""
import copy
import sys

import scbtool

PLAYER_NAME = "SkirmishRussia"
SOURCE_PLAYER = "SkirmishChina"
OBJECT_TYPE = 15
TEAM = 3

# The skeleton roster in RussiaReforged.ini.  Everything else China names has no Russia counterpart
# yet and is dropped (build actions) or emptied (team slots).
UNITS = {
    "ChinaInfantryRedguard": "RussiaMotostrelki",
    "ChinaVehicleDozer": "RussiaSapery",
    "ChinaVehicleTroopCrawler": "RussiaBMP3",
    "ChinaTankBattleMaster": "RussiaT72B",
    "ChinaTankOverlord": "RussiaT90",
    "ChinaTankGattling": "RussiaTunguska",
    "ChinaVehicleNukeLauncher": "Russia2S19",
    "ChinaCommandCenter": "RussiaCommandBunker",
    "ChinaPowerPlant": "RussiaThermalPowerPlant",
    "ChinaSupplyCenter": "RussiaSupplyCenter",
    "ChinaBarracks": "RussiaRegimentalDepot",
    "ChinaWarFactory": "RussiaMobilizationPlant",
    "ChinaPropagandaCenter": "RussiaFireDirectionCenter",
}

# Actions that name an upgrade or a building Russia does not have yet; dropped whole, because a
# build request for a China object is exactly the failure this patch removes.
DROP_ACTIONS = {"AI_PLAYER_BUILD_UPGRADE"}


def team_name(name):
    if name.startswith("China "):
        return "Russia " + name[len("China "):]
    if name.startswith("China"):
        return "Russia" + name[len("China"):]
    if name == "teamSkirmishChina":
        return "teamSkirmishRussia"
    return "Russia " + name


def clone_team(team, renamed):
    entries = []
    unit_slot = 0
    for key, kind, value in team:
        value = copy.deepcopy(value)
        if key == "teamName":
            value = renamed[value]
        elif key == "teamOwner" and value == SOURCE_PLAYER:
            value = PLAYER_NAME
        elif key.startswith("teamUnitType") and value and value != "<none>":
            slot = key[len("teamUnitType"):]
            mapped = UNITS.get(value)
            if mapped is None:
                # no Russia counterpart: empty the slot and zero its counts
                unit_slot = int(slot)
                value = "<none>"
            else:
                value = mapped
        entries.append((key, kind, value))
    if unit_slot:
        entries = [(key, kind, 0) if key in (f"teamUnitMinCount{unit_slot}", f"teamUnitMaxCount{unit_slot}") else (key, kind, value)
                   for key, kind, value in entries]
    return entries


def rename_group(name):
    """The scripts carry their side in their names; a Russia group that says China in every line is
    the kind of thing that makes the next reader distrust the file."""
    if name.startswith("China "):
        return "Russia " + name[len("China "):]
    if name.startswith("China"):
        return "Russia" + name[len("China"):]
    return name


def condition_names_china_object(script):
    """A condition that tests for an object Russia has no counterpart for can never be true; the
    script that needs it is dead weight and is dropped rather than left gated on a China object."""
    for child in script.children or []:
        if child.name not in ("Condition", "OrCondition"):
            continue
        candidates = [child] + list(child.children or [])
        for condition in candidates:
            if condition.name != "Condition":
                continue
            for parameter in condition.fields[2]:
                if parameter[0] == OBJECT_TYPE and parameter[3] not in UNITS and parameter[3].startswith("China"):
                    return True
    return False


def drop_dead_scripts(script_list):
    dropped = 0
    for group in list(script_list.children or []):
        for script in list(group.children or []):
            if script.name != "Script":
                continue
            if condition_names_china_object(script):
                group.children.remove(script)
                dropped += 1
            else:
                for sub in list(script.children or []):
                    if sub.name == "OrCondition":
                        for condition in list(sub.children or []):
                            for parameter in condition.fields[2]:
                                if parameter[0] == OBJECT_TYPE and parameter[3] not in UNITS and parameter[3].startswith("China"):
                                    group.children.remove(script)
                                    dropped += 1
                                    break
                            else:
                                continue
                            break
    return dropped


def rewrite_scripts(script_list, renamed):
    dropped = 0
    rewritten = 0
    stack = [script_list]
    while stack:
        chunk = stack.pop()
        if chunk.name == "ScriptGroup":
            chunk.fields[0] = rename_group(chunk.fields[0])
        elif chunk.name == "Script":
            chunk.fields[0] = rename_group(chunk.fields[0])
        for child in list(chunk.children or []):
            if chunk.name in ("Script", "OrCondition") and child.name in ("Condition", "ScriptAction", "ScriptActionFalse"):
                # conditions test object types too: "has a China dozer" is false for a Russian player
                for parameter in child.fields[2]:
                    if parameter[0] == OBJECT_TYPE and parameter[3] in UNITS:
                        parameter[3] = UNITS[parameter[3]]
                        rewritten += 1
            if child.name in ("ScriptAction", "ScriptActionFalse"):
                action = child.fields[1]
                params = child.fields[2]
                doomed = action in DROP_ACTIONS
                for parameter in params:
                    if parameter[0] == OBJECT_TYPE and parameter[3] in UNITS:
                        parameter[3] = UNITS[parameter[3]]
                        rewritten += 1
                    elif parameter[0] == OBJECT_TYPE and parameter[3].startswith("China"):
                        doomed = True
                    elif parameter[0] == TEAM and parameter[3] in renamed:
                        parameter[3] = renamed[parameter[3]]
                if doomed:
                    chunk.children.remove(child)
                    dropped += 1
                continue
            if child.children:
                stack.append(child)
    return rewritten, dropped


def main():
    source, target = sys.argv[1], sys.argv[2]
    table, chunks = scbtool.load(source)

    players = [chunk for chunk in chunks if chunk.name == "ScriptsPlayers"][0]
    names = [name for name, _ in players.fields[1]]
    if PLAYER_NAME in names:
        raise SystemExit(f"{PLAYER_NAME} is already in {source}")
    source_index = names.index(SOURCE_PLAYER)

    lists = [chunk for chunk in chunks if chunk.name == "PlayerScriptsList"][0]
    source_list = lists.children[source_index]
    russia_list = copy.deepcopy(source_list)

    teams = [chunk for chunk in chunks if chunk.name == "ScriptTeams"][0]
    source_teams = [team for team in teams.fields
                    if dict((key, value) for key, _, value in team).get("teamOwner") == SOURCE_PLAYER]
    renamed = {}
    for team in source_teams:
        old = dict((key, value) for key, _, value in team)["teamName"]
        renamed[old] = team_name(old)
    if len(set(renamed.values())) != len(renamed):
        raise SystemExit("two China teams map to one Russia name")

    dead = drop_dead_scripts(russia_list)
    rewritten, dropped = rewrite_scripts(russia_list, renamed)
    dropped += dead
    for team in source_teams:
        teams.fields.append(clone_team(team, renamed))

    players.fields[1].append((PLAYER_NAME, None))
    lists.children.append(russia_list)

    scbtool.save(target, table, chunks)
    print(f"{PLAYER_NAME}: {len(source_teams)} teams, {rewritten} object types rewritten, "
          f"{dropped} actions dropped")


if __name__ == "__main__":
    main()
